using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class EditPropertyViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Property type")]
    public PropertyType PropertyType { get; set; }

    [Range(1, 1000000, ErrorMessage = "Enter a price per night greater than 0.")]
    [Display(Name = "Price per night")]
    public decimal PricePerNight { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Cleaning fee")]
    public decimal CleaningFee { get; set; }

    // For display only, never posted back
    [ValidateNever]
    public List<PropertyImage> Images { get; set; } = new();
}