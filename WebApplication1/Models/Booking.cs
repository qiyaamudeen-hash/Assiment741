using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }

    public string GuestId { get; set; } = string.Empty;
    public ApplicationUser? Guest { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }

  
    [Column(TypeName = "decimal(18,2)")]
    public decimal NightlyRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }          

    [StringLength(500)]
    public string? HostNotes { get; set; }            
    public DateTime? RulesAcceptedAt { get; set; }
    public List<GuestDocument> Documents { get; set; } = new();


    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
}