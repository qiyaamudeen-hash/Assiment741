using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles = Roles.Guest)]
public class BookingsController : Controller
{
    private const int MaxNights = 30;
    private const int MaxTravelers = 16;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;

    public BookingsController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    // ---------- My bookings ----------

    public async Task<IActionResult> Index()
    {
        var guestId = _userManager.GetUserId(User)!;

        var bookings = await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p!.Images)
            .Where(b => b.GuestId == guestId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    public async Task<IActionResult> Details(int id)
    {
        var booking = await OwnBooking(id);
        if (booking == null) return NotFound();

        return View(booking);
    }

    // ---------- Live price quote (called by the booking form's JavaScript) ----------

    [HttpGet]
    public async Task<IActionResult> Quote(int propertyId, DateOnly? checkIn, DateOnly? checkOut)
    {
        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);
        if (property == null) return NotFound();

        var error = ValidateStay(checkIn, checkOut, travelers: 1);
        if (error != null) return BadRequest(new { error });

        if (await HasOverlap(propertyId, checkIn!.Value, checkOut!.Value))
            return Conflict(new { error = "Those dates aren't available." });

        var nights = checkOut.Value.DayNumber - checkIn.Value.DayNumber;
        return Json(BookingPricing.Calculate(property.PricePerNight, property.CleaningFee, nights));
    }

    // ---------- Request a booking ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int propertyId, DateOnly? checkIn, DateOnly? checkOut, int travelers)
    {
        var property = await _db.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);
        if (property == null) return NotFound();

        // Send the guest back to the property page with what they typed still filled in
        IActionResult Back(string message)
        {
            TempData["BookingError"] = message;
            return RedirectToAction("Details", "Properties", new
            {
                id = propertyId,
                checkIn = checkIn?.ToString("yyyy-MM-dd"),
                checkOut = checkOut?.ToString("yyyy-MM-dd"),
                travelers
            });
        }

        var error = ValidateStay(checkIn, checkOut, travelers);
        if (error != null) return Back(error);

        var nights = checkOut!.Value.DayNumber - checkIn!.Value.DayNumber;

        // Never trust prices from the browser: always recalculate here
        var price = BookingPricing.Calculate(property.PricePerNight, property.CleaningFee, nights);

        // Serializable stops two guests grabbing the same dates at the same moment
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (await HasOverlap(propertyId, checkIn.Value, checkOut.Value))
            return Back("Sorry, those dates are no longer available.");

        var booking = new Booking
        {
            PropertyId = propertyId,
            GuestId = _userManager.GetUserId(User)!,
            CheckInDate = checkIn.Value,
            CheckOutDate = checkOut.Value,
            NumberOfGuests = travelers,
            NightlyRate = price.NightlyRate,
            CleaningFee = price.CleaningFee,
            ServiceFee = price.ServiceFee,
            TotalPrice = price.Total,
            Status = BookingStatus.Pending
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _logger.LogAsync(booking.GuestId, "BookingRequested",
            $"Booking #{booking.Id} for '{property.Title}' ({checkIn:yyyy-MM-dd} to {checkOut:yyyy-MM-dd}), total {price.Total:N2}");

        TempData["Success"] = "Your booking request has been sent to the host.";
        return RedirectToAction(nameof(Details), new { id = booking.Id });
    }

    // ---------- Cancel ----------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await OwnBooking(id);
        if (booking == null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Approved) || booking.CheckInDate <= today)
        {
            TempData["Error"] = "This booking can no longer be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var previous = booking.Status;
        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();

        await _logger.LogAsync(booking.GuestId, "BookingCancelled",
            $"Booking #{booking.Id} for '{booking.Property!.Title}' cancelled by the guest (was {previous})");

        TempData["Success"] = "Your booking has been cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Helpers ----------

    private string? ValidateStay(DateOnly? checkIn, DateOnly? checkOut, int travelers)
    {
        if (checkIn == null || checkOut == null)
            return "Please choose your check-in and check-out dates.";

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (checkIn.Value < today)
            return "Check-in can't be in the past.";

        if (checkOut.Value <= checkIn.Value)
            return "Check-out must be after check-in.";

        if (checkOut.Value.DayNumber - checkIn.Value.DayNumber > MaxNights)
            return $"Stays are limited to {MaxNights} nights.";

        if (travelers < 1 || travelers > MaxTravelers)
            return $"Number of travellers must be between 1 and {MaxTravelers}.";

        return null;
    }

    // Pending and approved bookings both hold the dates.
    // Strict < and > mean a check-out day can be the next guest's check-in day.
    private Task<bool> HasOverlap(int propertyId, DateOnly checkIn, DateOnly checkOut) =>
        _db.Bookings.AnyAsync(b =>
            b.PropertyId == propertyId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
            b.CheckInDate < checkOut &&
            b.CheckOutDate > checkIn);

    // Only ever returns a booking that belongs to the logged-in guest
    private async Task<Booking?> OwnBooking(int id)
    {
        var guestId = _userManager.GetUserId(User)!;
        return await _db.Bookings
            .Include(b => b.Property).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId);
    }
}