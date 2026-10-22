using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.Guest)]
public class HostApplicationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageStorage _images;
    private readonly IActivityLogger _logger;

    public HostApplicationController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IImageStorage images,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _images = images;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Apply()
    {
        var userId = _userManager.GetUserId(User)!;
        if (await HasPendingApplication(userId))
            return RedirectToAction(nameof(Status));

        return View(new BecomeHostViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(BecomeHostViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        if (await HasPendingApplication(userId))
            return RedirectToAction(nameof(Status));

        // An application without at least one image cannot be submitted
        var files = model.Images.Where(f => f.Length > 0).ToList();
        if (files.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Images), "Please upload at least one photo of your property.");
        }
        else
        {
            var error = _images.Validate(files);
            if (error != null)
                ModelState.AddModelError(nameof(model.Images), error);
        }

        if (!ModelState.IsValid) return View(model);

        var urls = await _images.SavePropertyImagesAsync(files);

        var property = new HostProperty
        {
            Title = model.Title,
            Description = model.Description,
            Address = model.Address,
            City = model.City,
            PropertyType = model.PropertyType,
            PricePerNight = model.PricePerNight,
            CleaningFee = model.CleaningFee,
            HostId = userId,
            IsActive = false,   // stays hidden until an admin approves
            Images = urls.Select(u => new PropertyImage { ImageUrl = u }).ToList()
        };

        var application = new HostApplication
        {
            ApplicantId = userId,
            Property = property,
            Status = ApplicationStatus.Pending
        };

        _db.HostApplications.Add(application);
        await _db.SaveChangesAsync();

        await _logger.LogAsync(userId, "HostApplicationSubmitted",
            $"Application #{application.Id} for '{property.Title}'");

        TempData["Success"] = "Your application has been submitted. An admin will review it soon.";
        return RedirectToAction(nameof(Status));
    }

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var userId = _userManager.GetUserId(User)!;

        var applications = await _db.HostApplications
            .Include(a => a.Property)
                .ThenInclude(p => p!.Images)
            .Where(a => a.ApplicantId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(applications);
    }

    private Task<bool> HasPendingApplication(string userId) =>
        _db.HostApplications.AnyAsync(a =>
            a.ApplicantId == userId && a.Status == ApplicationStatus.Pending);
}