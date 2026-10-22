using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class PropertyCard
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public decimal PricePerNight { get; set; }
    public string? ImageUrl { get; set; }
}

public class PropertyListViewModel
{
    public List<PropertyCard> Properties { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public string? City { get; set; }
    public PropertyType? Type { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class PropertyDetailsViewModel
{
    public HostProperty Property { get; set; } = null!;
    public string HostName { get; set; } = string.Empty;
    public CheckInProcess? CheckIn { get; set; }
    public bool IsPreview { get; set; }
    public string? ChosenCheckIn { get; set; }
    public string? ChosenCheckOut { get; set; }
    public int ChosenTravelers { get; set; } = 1;
}
