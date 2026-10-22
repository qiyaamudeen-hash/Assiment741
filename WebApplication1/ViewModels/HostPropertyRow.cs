using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class HostPropertyRow
{
    public HostProperty Property { get; set; } = null!;
    public ApplicationStatus ApprovalStatus { get; set; }
    public string? AdminNotes { get; set; }
    public bool HasCheckIn { get; set; }
}