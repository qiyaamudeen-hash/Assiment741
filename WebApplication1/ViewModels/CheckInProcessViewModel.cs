using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication1.ViewModels;

public class CheckInProcessViewModel
{
    public int PropertyId { get; set; }

  
    [ValidateNever]
    public string PropertyTitle { get; set; } = string.Empty;

    [Required, DataType(DataType.Time)]
    [Display(Name = "Check-in time")]
    public TimeOnly CheckInTime { get; set; } = new(15, 0);

    [Required, DataType(DataType.Time)]
    [Display(Name = "Check-out time")]
    public TimeOnly CheckOutTime { get; set; } = new(11, 0);

    [Required, StringLength(2000)]
    [Display(Name = "House rules")]
    public string HouseRules { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Check-in instructions (private)")]
    public string? CheckInInstructions { get; set; }

    [Display(Name = "Proof of address")]
    public bool RequireProofOfAddress { get; set; }

    [Display(Name = "Driver's licence")]
    public bool RequireDriversLicense { get; set; }
}