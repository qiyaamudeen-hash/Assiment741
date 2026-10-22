using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;

    public AdminController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    // Dashboard: counts + pending queue
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            PendingCount = await _db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
            ApprovedCount = await _db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Approved),
            RejectedCount = await _db.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected),
            PendingApplications = await _db.HostApplications
                .Include(a => a.Applicant)
                .Include(a => a.Property).ThenInclude(p => p!.Images)
                .Where(a => a.Status == ApplicationStatus.Pending)
                .OrderBy(a => a.SubmittedAt)
                .ToListAsync()
        };

        return View(model);
    }

    // Full detail of one application
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var application = await LoadApplication(id);
        if (application == null) return NotFound();

        return View(application);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var application = await LoadApplication(id);
        if (application == null) return NotFound();

        if (application.Status != ApplicationStatus.Pending)
        {
            TempData["Error"] = "This application has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        var applicant = await _userManager.FindByIdAsync(application.ApplicantId);
        if (applicant == null) return NotFound();

        var becameHost = false;

        // 1. Add the Host role (skipped if they are already a Host adding another property)
        if (!await _userManager.IsInRoleAsync(applicant, Roles.Host))
        {
            var added = await _userManager.AddToRoleAsync(applicant, Roles.Host);
            if (!added.Succeeded)
            {
                TempData["Error"] = "Could not add the Host role: " + string.Join(", ", added.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Review), new { id });
            }
            becameHost = true;
        }

        // 2. Remove the Guest role (a user is a Guest OR a Host, never both)
        if (await _userManager.IsInRoleAsync(applicant, Roles.Guest))
        {
            var removed = await _userManager.RemoveFromRoleAsync(applicant, Roles.Guest);
            if (!removed.Succeeded)
            {
                if (becameHost) await _userManager.RemoveFromRoleAsync(applicant, Roles.Host);   // roll back step 1
                TempData["Error"] = "Could not remove the Guest role: " + string.Join(", ", removed.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Review), new { id });
            }
        }

        var adminId = _userManager.GetUserId(User)!;

        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = adminId;
        application.Property!.IsActive = true;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(adminId, "HostApplicationApproved",
            $"Application #{application.Id} ('{application.Property.Title}') approved for {applicant.Email}");

        if (becameHost)
            await _logger.LogAsync(adminId, "RoleChange", $"{applicant.Email}: Guest -> Host");

        TempData["Success"] = becameHost
            ? $"{applicant.Email} is now a Host and their property is live."
            : $"Listing approved and now live for {applicant.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? notes)
    {
        var application = await LoadApplication(id);
        if (application == null) return NotFound();

        if (application.Status != ApplicationStatus.Pending)
        {
            TempData["Error"] = "This application has already been reviewed.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            TempData["Error"] = "Please give a short reason so the applicant knows what to fix.";
            return RedirectToAction(nameof(Review), new { id });
        }

        var adminId = _userManager.GetUserId(User)!;

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = adminId;
        application.AdminNotes = notes.Trim().Length > 500 ? notes.Trim()[..500] : notes.Trim();
        await _db.SaveChangesAsync();

        await _logger.LogAsync(adminId, "HostApplicationRejected",
            $"Application #{application.Id} ('{application.Property!.Title}') rejected: {application.AdminNotes}");

        TempData["Success"] = "Application rejected.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> ActivityLog(string? filter, int page = 1)
    {
        const int pageSize = 25;
        if (page < 1) page = 1;

        var query = _db.ActivityLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter))
            query = query.Where(l => l.Action == filter);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // The log only stores user IDs, so look up the emails for this page of rows
        var userIds = logs.Where(l => l.UserId != null).Select(l => l.UserId!).Distinct().ToList();
        var emails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id);

        var model = new ActivityLogViewModel
        {
            Filter = filter,
            Page = page,
            TotalPages = totalPages,
            AvailableActions = await _db.ActivityLogs
                .Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync(),
            Rows = logs.Select(l => new ActivityLogRow
            {
                CreatedAt = l.CreatedAt,
                User = l.UserId != null && emails.TryGetValue(l.UserId, out var email) ? email : "—",
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress
            }).ToList()
        };

        return View(model);
    }
    private Task<HostApplication?> LoadApplication(int id) =>
        _db.HostApplications
            .Include(a => a.Applicant)
            .Include(a => a.Property).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(a => a.Id == id);
}
