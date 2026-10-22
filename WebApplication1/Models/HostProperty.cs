using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class HostProperty
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; }

    public bool IsActive { get; set; }

    public string HostId { get; set; } = string.Empty;
    public ApplicationUser? Host { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PropertyImage> Images { get; set; } = new();
    public CheckInProcess? CheckInProcess { get; set; }   

}