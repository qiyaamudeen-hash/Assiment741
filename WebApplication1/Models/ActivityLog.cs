namespace WebApplication1.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }         
    public string Action { get; set; } = string.Empty;   
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}