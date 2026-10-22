using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class UserManagementViewModel
{
    public string? Search { get; set; }
    public List<ApplicationUser> Guests { get; set; } = new();
    public List<ApplicationUser> Admins { get; set; } = new();
}