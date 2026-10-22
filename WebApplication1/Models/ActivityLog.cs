namespace WebApplication1.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }          // null for failed logins by unknown emails
    public string Action { get; set; } = string.Empty;   // e.g. "Login", "RoleChange"
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}