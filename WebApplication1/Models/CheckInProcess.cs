using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class CheckInProcess
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty? Property { get; set; }

    public TimeOnly CheckInTime { get; set; } = new TimeOnly(15, 0);
    public TimeOnly CheckOutTime { get; set; } = new TimeOnly(11, 0);

    [Required, StringLength(2000)]
    public string HouseRules { get; set; } = string.Empty;

    // Private: only revealed to a guest once their documents are verified
    [StringLength(2000)]
    public string? CheckInInstructions { get; set; }

    public RequiredDocument RequiredDocuments { get; set; } = RequiredDocument.IdOrPassport;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}