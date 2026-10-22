using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser? Sender { get; set; }

    [Required, StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Set when the other person opens the conversation; null means unread
    public DateTime? ReadAt { get; set; }
}