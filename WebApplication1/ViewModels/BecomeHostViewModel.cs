using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class BecomeHostViewModel
{
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

    [Display(Name = "Photos")]
    public List<IFormFile> Images { get; set; } = new();
}