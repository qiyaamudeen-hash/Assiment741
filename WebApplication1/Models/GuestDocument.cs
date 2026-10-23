using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class GuestDocument
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }


    public RequiredDocument DocumentType { get; set; }

   
    [Required]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    public string? ReviewNote { get; set; }
}