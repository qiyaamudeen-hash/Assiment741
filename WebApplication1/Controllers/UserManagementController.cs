using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.SuperAdmin)]
public class UserManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;

    public UserManagementController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var model = new UserManagementViewModel
        {
            Search = search,
            Guests = await UsersInRole(Roles.Guest, search),
            Admins = await UsersInRole(Roles.Admin, search)
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PromoteToAdmin(string id) =>
        ChangeRole(id, fromRole: Roles.Guest, toRole: Roles.Admin);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DemoteToGuest(string id) =>
        ChangeRole(id, fromRole: Roles.Admin, toRole: Roles.Guest);

    // Swaps one role for another: add the new role first, then remove the old one,
    // and roll back if the second step fails so nobody is left with two roles.
    private async Task<IActionResult> ChangeRole(string id, string fromRole, string toRole)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
            return Fail("SuperAdmin accounts cannot be changed here.");

        if (!await _userManager.IsInRoleAsync(user, fromRole))
            return Fail($"Only users with the {fromRole} role can be changed to {toRole}.");

        var addedHere = false;
        if (!await _userManager.IsInRoleAsync(user, toRole))
        {
            var added = await _userManager.AddToRoleAsync(user, toRole);
            if (!added.Succeeded)
                return Fail($"Could not add the {toRole} role: " + string.Join(", ", added.Errors.Select(e => e.Description)));
            addedHere = true;
        }

        var removed = await _userManager.RemoveFromRoleAsync(user, fromRole);
        if (!removed.Succeeded)
        {
            if (addedHere) await _userManager.RemoveFromRoleAsync(user, toRole);   // roll back
            return Fail($"Could not remove the {fromRole} role: " + string.Join(", ", removed.Errors.Select(e => e.Description)));
        }

        var actorId = _userManager.GetUserId(User)!;
        await _logger.LogAsync(actorId, "RoleChange", $"{user.Email}: {fromRole} -> {toRole}");

        TempData["Success"] = $"{user.Email} is now {(toRole == Roles.Admin ? "an" : "a")} {toRole}.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Fail(string message)
    {
        TempData["Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<ApplicationUser>> UsersInRole(string roleName, string? search)
    {
        var query =
            from u in _db.Users
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == roleName
            select u;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(s)) ||
                u.FirstName.Contains(s) ||
                u.LastName.Contains(s));
        }

        return await query.OrderBy(u => u.Email).Take(50).ToListAsync();
    }
}