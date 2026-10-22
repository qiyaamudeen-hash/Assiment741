using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.ViewComponents;

public class UnreadMessagesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UnreadMessagesViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId == null) return Content(string.Empty);

        var unread = await _db.Messages.CountAsync(m =>
            m.ReadAt == null &&
            m.SenderId != userId &&
            (m.Conversation!.GuestId == userId || m.Conversation.HostId == userId));

        return View(unread);
    }
}