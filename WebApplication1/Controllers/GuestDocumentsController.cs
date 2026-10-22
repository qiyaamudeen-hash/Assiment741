using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.Guest + "," + Roles.Host)]
public class GuestDocumentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDocumentStorage _storage;
    private readonly IActivityLogger _logger;

    public GuestDocumentsController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IDocumentStorage storage,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Open(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        // The guest who uploaded it, or the host of that property. Nobody else, not even admins.
        var document = await _db.GuestDocuments
            .Include(d => d.Booking).ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(d => d.Id == id &&
                (d.Booking!.GuestId == userId || d.Booking.Property!.HostId == userId));

        if (document == null) return NotFound();

        var path = _storage.GetPath(document.StoredFileName);
        if (path == null) return NotFound();

        // Leave a trail whenever someone other than the owner looks at an ID document
        if (document.Booking!.GuestId != userId)
        {
            await _logger.LogAsync(userId, "GuestDocumentViewed",
                $"{document.DocumentType} for booking #{document.BookingId}");
        }

        // The content type comes from our own stored extension, never from the guest
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "image/jpeg"
        };

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";
        return PhysicalFile(path, contentType);
    }
}