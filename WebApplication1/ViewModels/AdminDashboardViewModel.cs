using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class AdminDashboardViewModel
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<HostApplication> PendingApplications { get; set; } = new();
}