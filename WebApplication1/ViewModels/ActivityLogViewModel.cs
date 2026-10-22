namespace WebApplication1.ViewModels;

public class ActivityLogRow
{
    public DateTime CreatedAt { get; set; }
    public string User { get; set; } = "—";
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class ActivityLogViewModel
{
    public List<ActivityLogRow> Rows { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();
    public string? Filter { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}