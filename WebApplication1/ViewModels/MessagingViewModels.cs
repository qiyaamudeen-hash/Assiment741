using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class InboxRow
{
    public int ConversationId { get; set; }
    public string OtherName { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int Unread { get; set; }
}

public class ChatViewModel
{
    public Conversation Conversation { get; set; } = null!;
    public string CurrentUserId { get; set; } = string.Empty;
    public string OtherName { get; set; } = string.Empty;
}

public class StartMessageViewModel
{
    public int PropertyId { get; set; }

    [ValidateNever]
    public string PropertyTitle { get; set; } = string.Empty;

    [ValidateNever]
    public string HostName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write a message.")]
    [StringLength(2000, ErrorMessage = "Messages can be up to 2000 characters.")]
    [Display(Name = "Your message")]
    public string Body { get; set; } = string.Empty;
}