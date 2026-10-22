
using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;


namespace WebApplication1.Data;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        string[] roles = { Roles.Guest, Roles.Host, Roles.Admin, Roles.SuperAdmin };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = config["SeedSuperAdmin:Email"]!;
        var password = config["SeedSuperAdmin:Password"]!;

        var superAdmin = await userManager.FindByEmailAsync(email);
        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin"
            };

            var result = await userManager.CreateAsync(superAdmin, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        }
    }
}