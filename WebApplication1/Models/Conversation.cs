namespace WebApplication1.Models;

public class Conversation
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }

    public string GuestId { get; set; } = string.Empty;
    public ApplicationUser? Guest { get; set; }

    // Copied from the property's Host when the conversation starts
    public string HostId { get; set; } = string.Empty;
    public ApplicationUser? Host { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public List<Message> Messages { get; set; } = new();
}