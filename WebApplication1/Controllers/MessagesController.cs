using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;


[Authorize(Roles = Roles.Guest + "," + Roles.Host)]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _logger;

    public MessagesController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogger logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }



    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var rows = await _db.Conversations
            .Where(c => c.GuestId == userId || c.HostId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new InboxRow
            {
                ConversationId = c.Id,
                OtherName = c.GuestId == userId
                    ? c.Host!.FirstName + " " + c.Host.LastName
                    : c.Guest!.FirstName + " " + c.Guest.LastName,
                PropertyTitle = c.Property!.Title,
                LastMessageAt = c.LastMessageAt,
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Body).FirstOrDefault(),
                Unread = c.Messages.Count(m => m.ReadAt == null && m.SenderId != userId)
            })
            .ToListAsync();

        return View(rows);
    }



    public async Task<IActionResult> Chat(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _db.Conversations
            .Include(c => c.Property)
            .Include(c => c.Guest)
            .Include(c => c.Host)
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(c => c.Id == id && (c.GuestId == userId || c.HostId == userId));

        if (conversation == null) return NotFound();

       
        var now = DateTime.UtcNow;
        var changed = false;
        foreach (var message in conversation.Messages.Where(m => m.ReadAt == null && m.SenderId != userId))
        {
            message.ReadAt = now;
            changed = true;
        }
        if (changed) await _db.SaveChangesAsync();

        var other = conversation.GuestId == userId ? conversation.Host : conversation.Guest;

        return View(new ChatViewModel
        {
            Conversation = conversation,
            CurrentUserId = userId,
            OtherName = $"{other?.FirstName} {other?.LastName}".Trim()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? body)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && (c.GuestId == userId || c.HostId == userId));
        if (conversation == null) return NotFound();

        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            TempData["Error"] = "Please write a message.";
            return RedirectToAction(nameof(Chat), new { id });
        }
        if (text.Length > 2000)
        {
            TempData["Error"] = "Messages can be up to 2000 characters.";
            return RedirectToAction(nameof(Chat), new { id });
        }

        _db.Messages.Add(new Message { ConversationId = id, SenderId = userId, Body = text });
        conversation.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Chat), new { id });
    }


    [Authorize(Roles = Roles.Guest)]
    [HttpGet]
    public async Task<IActionResult> Start(int propertyId)
    {
        var guestId = _userManager.GetUserId(User)!;

        var property = await FindMessageableProperty(propertyId, guestId);
        if (property == null) return NotFound();

       
        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => c.PropertyId == propertyId && c.GuestId == guestId);
        if (existing != null)
            return RedirectToAction(nameof(Chat), new { id = existing.Id });

        return View(new StartMessageViewModel
        {
            PropertyId = propertyId,
            PropertyTitle = property.Title,
            HostName = property.Host?.FirstName ?? "the host"
        });
    }

    [Authorize(Roles = Roles.Guest)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartMessageViewModel model)
    {
        var guestId = _userManager.GetUserId(User)!;

        var property = await FindMessageableProperty(model.PropertyId, guestId);
        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.PropertyTitle = property.Title;
            model.HostName = property.Host?.FirstName ?? "the host";
            return View(model);
        }

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.PropertyId == property.Id && c.GuestId == guestId);

        var isNew = conversation == null;
        if (conversation == null)
        {
            conversation = new Conversation
            {
                PropertyId = property.Id,
                GuestId = guestId,
                HostId = property.HostId
            };
            _db.Conversations.Add(conversation);
        }

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.Messages.Add(new Message { SenderId = guestId, Body = model.Body.Trim() });
        await _db.SaveChangesAsync();

        if (isNew)
        {
            await _logger.LogAsync(guestId, "ConversationStarted",
                $"Conversation #{conversation.Id} about '{property.Title}' (#{property.Id})");
        }

        return RedirectToAction(nameof(Chat), new { id = conversation.Id });
    }

=
    private async Task<HostProperty?> FindMessageableProperty(int propertyId, string guestId)
    {
        var property = await _db.HostProperties
            .Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null) return null;

        if (!property.IsActive &&
            !await _db.Bookings.AnyAsync(b => b.PropertyId == propertyId && b.GuestId == guestId))
            return null;

        return property;
    }
}