using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class GuestDocument
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    // One of IdOrPassport, ProofOfAddress or DriversLicense (a single value per upload)
    public RequiredDocument DocumentType { get; set; }

    // Name of the file on disk (a random GUID), never the guest's original name
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