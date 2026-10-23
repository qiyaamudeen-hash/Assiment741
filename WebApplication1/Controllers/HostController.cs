using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;


namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.Host)]
public class HostController : Controller
{
    private const int MaxPhotos = 10;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageStorage _images;
    private readonly IActivityLogger _logger;

    public HostController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IImageStorage images,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _images = images;
        _logger = logger;
    }

    =

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var properties = await _db.HostProperties
            .Include(p => p.Images)
            .Where(p => p.HostId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var ids = properties.Select(p => p.Id).ToList();

        var applications = await _db.HostApplications
            .Where(a => ids.Contains(a.PropertyId))
            .ToListAsync();

        var configuredIds = await _db.CheckInProcesses
            .Where(c => ids.Contains(c.PropertyId))
            .Select(c => c.PropertyId)
            .ToListAsync();

        var rows = properties.Select(p =>
        {
            var app = applications
                .Where(a => a.PropertyId == p.Id)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefault();

            return new HostPropertyRow
            {
                Property = p,
                ApprovalStatus = app?.Status ?? ApplicationStatus.Pending,
                AdminNotes = app?.AdminNotes,
                HasCheckIn = configuredIds.Contains(p.Id)
            };
        }).ToList();

        ViewBag.PendingBookings = await _db.Bookings
        .CountAsync(b => b.Property!.HostId == userId && b.Status == BookingStatus.Pending);

        return View(rows);
    }

=

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        if (!await IsApproved(id))
        {
            TempData["Error"] = "This listing can only be switched on after an admin approves it.";
            return RedirectToAction(nameof(Index));
        }

        property.IsActive = !property.IsActive;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), property.IsActive ? "PropertyActivated" : "PropertyDeactivated",
            $"'{property.Title}' (#{property.Id})");

        TempData["Success"] = property.IsActive
            ? $"'{property.Title}' is now visible to guests."
            : $"'{property.Title}' is now hidden from guests.";
        return RedirectToAction(nameof(Index));
    }

=

    [HttpGet]
    public IActionResult NewListing() => View(new BecomeHostViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewListing(BecomeHostViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        var files = model.Images.Where(f => f.Length > 0).ToList();
        if (files.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Images), "Please upload at least one photo of your property.");
        }
        else if (files.Count > MaxPhotos)
        {
            ModelState.AddModelError(nameof(model.Images), $"You can upload up to {MaxPhotos} photos.");
        }
        else
        {
            var error = _images.Validate(files);
            if (error != null) ModelState.AddModelError(nameof(model.Images), error);
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
            IsActive = false,
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

        await _logger.LogAsync(userId, "PropertyListingSubmitted",
            $"Application #{application.Id} for '{property.Title}'");

        TempData["Success"] = "Your new listing was submitted and is waiting for admin approval.";
        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        return View(ToEditModel(property));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPropertyViewModel model)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.Images = property.Images;
            return View(model);
        }

        property.Title = model.Title;
        property.Description = model.Description;
        property.Address = model.Address;
        property.City = model.City;
        property.PropertyType = model.PropertyType;
        property.PricePerNight = model.PricePerNight;
        property.CleaningFee = model.CleaningFee;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "PropertyUpdated", $"'{property.Title}' (#{property.Id})");

        TempData["Success"] = "Listing updated.";
        return RedirectToAction(nameof(Edit), new { id });
    }

   

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPhotos(int id, List<IFormFile> newPhotos)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        var files = newPhotos.Where(f => f.Length > 0).ToList();
        if (files.Count == 0)
        {
            TempData["Error"] = "Choose at least one photo to add.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (property.Images.Count + files.Count > MaxPhotos)
        {
            TempData["Error"] = $"A listing can have at most {MaxPhotos} photos.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var error = _images.Validate(files);
        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Edit), new { id });
        }

        var urls = await _images.SavePropertyImagesAsync(files);
        foreach (var url in urls)
            property.Images.Add(new PropertyImage { ImageUrl = url });

        await _db.SaveChangesAsync();
        await _logger.LogAsync(_userManager.GetUserId(User), "PropertyPhotosAdded",
            $"{urls.Count} photo(s) added to '{property.Title}' (#{property.Id})");

        TempData["Success"] = "Photos added.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int imageId)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        var image = property.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return NotFound();

        if (property.Images.Count <= 1)
        {
            TempData["Error"] = "A listing must keep at least one photo. Add another one before deleting this.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var url = image.ImageUrl;
        _db.PropertyImages.Remove(image);
        await _db.SaveChangesAsync();
        _images.DeletePropertyImage(url);

        await _logger.LogAsync(_userManager.GetUserId(User), "PropertyPhotoDeleted",
            $"Photo removed from '{property.Title}' (#{property.Id})");

        TempData["Success"] = "Photo deleted.";
        return RedirectToAction(nameof(Edit), new { id });
    }


    [HttpGet]
    public async Task<IActionResult> CheckIn(int id)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        var process = await _db.CheckInProcesses.FirstOrDefaultAsync(c => c.PropertyId == id);

        var model = new CheckInProcessViewModel
        {
            PropertyId = id,
            PropertyTitle = property.Title
        };

        if (process != null)
        {
            model.CheckInTime = process.CheckInTime;
            model.CheckOutTime = process.CheckOutTime;
            model.HouseRules = process.HouseRules;
            model.CheckInInstructions = process.CheckInInstructions;
            model.RequireProofOfAddress = process.RequiredDocuments.HasFlag(RequiredDocument.ProofOfAddress);
            model.RequireDriversLicense = process.RequiredDocuments.HasFlag(RequiredDocument.DriversLicense);
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id, CheckInProcessViewModel model)
    {
        var property = await OwnedProperty(id);
        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.PropertyId = id;
            model.PropertyTitle = property.Title;
            return View(model);
        }

        var process = await _db.CheckInProcesses.FirstOrDefaultAsync(c => c.PropertyId == id);
        if (process == null)
        {
            process = new CheckInProcess { PropertyId = id };
            _db.CheckInProcesses.Add(process);
        }

        process.CheckInTime = model.CheckInTime;
        process.CheckOutTime = model.CheckOutTime;
        process.HouseRules = model.HouseRules.Trim();
        process.CheckInInstructions = string.IsNullOrWhiteSpace(model.CheckInInstructions)
            ? null
            : model.CheckInInstructions.Trim();

        var documents = RequiredDocument.IdOrPassport;
        if (model.RequireProofOfAddress) documents |= RequiredDocument.ProofOfAddress;
        if (model.RequireDriversLicense) documents |= RequiredDocument.DriversLicense;
        process.RequiredDocuments = documents;

        process.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "CheckInProcessUpdated",
            $"Check-in settings saved for '{property.Title}' (#{property.Id})");

        TempData["Success"] = "Check-in settings saved.";
        return RedirectToAction(nameof(CheckIn), new { id });
    }
    
    public async Task<IActionResult> Bookings()
    {
        var userId = _userManager.GetUserId(User)!;

        var bookings = await _db.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Documents)
            .Include(b => b.Property).ThenInclude(p => p!.CheckInProcess)
            .Where(b => b.Property!.HostId == userId)
            .OrderBy(b => b.CheckInDate)
            .ToListAsync();

        return View(bookings);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBooking(int id, string? notes)
    {
        var booking = await OwnedBooking(id);
        if (booking == null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] = "This request is no longer pending (the guest may have cancelled it).";
            return RedirectToAction(nameof(Bookings));
        }

        if (booking.CheckInDate < DateOnly.FromDateTime(DateTime.Today))
        {
            TempData["Error"] = "The check-in date for this request has already passed.";
            return RedirectToAction(nameof(Bookings));
        }

        booking.Status = BookingStatus.Approved;
        booking.DecidedAt = DateTime.UtcNow;
        booking.HostNotes = CleanNote(notes);
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "BookingApproved",
            $"Booking #{booking.Id} for '{booking.Property!.Title}' approved ({booking.CheckInDate:yyyy-MM-dd} to {booking.CheckOutDate:yyyy-MM-dd})");

        TempData["Success"] = "Booking approved.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(int id, string? notes)
    {
        var booking = await OwnedBooking(id);
        if (booking == null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] = "This request is no longer pending (the guest may have cancelled it).";
            return RedirectToAction(nameof(Bookings));
        }

        var note = CleanNote(notes);
        if (note == null)
        {
            TempData["Error"] = "Please give the guest a short reason for declining.";
            return RedirectToAction(nameof(Bookings));
        }

        booking.Status = BookingStatus.Rejected;
        booking.DecidedAt = DateTime.UtcNow;
        booking.HostNotes = note;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "BookingRejected",
            $"Booking #{booking.Id} for '{booking.Property!.Title}' rejected: {note}");

        TempData["Success"] = "Booking declined.";
        return RedirectToAction(nameof(Bookings));

    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyDocument(int id)
    {
        var document = await OwnedDocument(id);
        if (document == null) return NotFound();

        var problem = DocumentReviewProblem(document);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Bookings));
        }

        document.Status = DocumentStatus.Verified;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewNote = null;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "GuestDocumentVerified",
            $"{document.DocumentType} for booking #{document.BookingId}");

        TempData["Success"] = "Document verified.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDocument(int id, string? notes)
    {
        var document = await OwnedDocument(id);
        if (document == null) return NotFound();

        var problem = DocumentReviewProblem(document);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Bookings));
        }

        var note = CleanNote(notes);
        if (note == null)
        {
            TempData["Error"] = "Please tell the guest what's wrong so they can upload a new one.";
            return RedirectToAction(nameof(Bookings));
        }

        document.Status = DocumentStatus.Rejected;
        document.ReviewedAt = DateTime.UtcNow;
        document.ReviewNote = note;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(_userManager.GetUserId(User), "GuestDocumentRejected",
            $"{document.DocumentType} for booking #{document.BookingId}: {note}");

        TempData["Success"] = "Document rejected. The guest can upload a new one.";
        return RedirectToAction(nameof(Bookings));
    }

    private async Task<HostProperty?> OwnedProperty(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == userId);
    }
  
    private async Task<GuestDocument?> OwnedDocument(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.GuestDocuments
            .Include(d => d.Booking).ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(d => d.Id == id && d.Booking!.Property!.HostId == userId);
    }
 
    private async Task<Booking?> OwnedBooking(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property!.HostId == userId);
    }

    private static string? DocumentReviewProblem(GuestDocument document)
    {
        if (document.Booking!.Status != BookingStatus.Approved)
            return "This booking is no longer active.";
        if (document.Status != DocumentStatus.Pending)
            return "This document has already been reviewed.";
        return null;
    }

    private static string? CleanNote(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var trimmed = notes.Trim();
        return trimmed.Length > 500 ? trimmed[..500] : trimmed;
    }

    private Task<bool> IsApproved(int propertyId) =>
        _db.HostApplications.AnyAsync(a => a.PropertyId == propertyId && a.Status == ApplicationStatus.Approved);

    private static EditPropertyViewModel ToEditModel(HostProperty p) => new()
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        Address = p.Address,
        City = p.City,
        PropertyType = p.PropertyType,
        PricePerNight = p.PricePerNight,
        CleaningFee = p.CleaningFee,
        Images = p.Images
    };
}