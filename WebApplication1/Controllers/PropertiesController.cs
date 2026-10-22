using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

// No [Authorize] on purpose: browsing is public. Booking (Phase 4B) will be Guest-only.
public class PropertiesController : Controller
{
    private const int PageSize = 12;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PropertiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? city, PropertyType? type, int page = 1)
    {
        if (page < 1) page = 1;

        // IsActive can only become true after an admin approves the listing,
        // and a Host can switch it off at any time.
        var query = _db.HostProperties.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City == city);

        if (type.HasValue)
            query = query.Where(p => p.PropertyType == type.Value);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        if (page > totalPages) page = totalPages;

        var cards = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new PropertyCard
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                PropertyType = p.PropertyType,
                PricePerNight = p.PricePerNight,
                ImageUrl = p.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync();

        var cities = await _db.HostProperties
            .Where(p => p.IsActive)
            .Select(p => p.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return View(new PropertyListViewModel
        {
            Properties = cards,
            Cities = cities,
            City = city,
            Type = type,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total
        });
    }

    public async Task<IActionResult> Details(int id, string? checkIn, string? checkOut, int travelers = 1)
    {
        var property = await _db.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Host)
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) return NotFound();

        // Hidden listings are only visible to the Host who owns them (as a preview)
        var userId = _userManager.GetUserId(User);
        var isOwner = userId != null && property.HostId == userId;
        if (!property.IsActive && !isOwner) return NotFound();

        return View(new PropertyDetailsViewModel
        {
            Property = property,
            HostName = property.Host?.FirstName ?? "your host",
            CheckIn = property.CheckInProcess,
            IsPreview = !property.IsActive,
            ChosenCheckIn = checkIn,
            ChosenCheckOut = checkOut,
            ChosenTravelers = travelers < 1 ? 1 : travelers
        });
    }
}