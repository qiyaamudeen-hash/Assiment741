
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IActivityLogger
{
    Task LogAsync(string? userId, string action, string? details = null);
}

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ActivityLogger(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string? userId, string action, string? details = null)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}
