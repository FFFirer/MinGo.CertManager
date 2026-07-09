using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinGo.CertManager.Core.Entities;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Infrastructure.Services;

public static class IdentitySeedService
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.EnsureCreatedAsync();

            var adminRoleExists = await roleManager.RoleExistsAsync("Admin");
            if (!adminRoleExists)
            {
                var result = await roleManager.CreateAsync(new IdentityRole("Admin"));
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin role created successfully");
                }
                else
                {
                    logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            var userRoleExists = await roleManager.RoleExistsAsync("User");
            if (!userRoleExists)
            {
                var result = await roleManager.CreateAsync(new IdentityRole("User"));
                if (result.Succeeded)
                {
                    logger.LogInformation("User role created successfully");
                }
                else
                {
                    logger.LogError("Failed to create User role: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // 兼容旧数据：先用旧用户名查找，再用邮箱查找
            var adminUser = await userManager.FindByNameAsync("admin")
                         ?? await userManager.FindByEmailAsync("admin@example.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "admin");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Default admin user created successfully with username 'admin@example.com' and password 'admin'");
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding identity data");
        }
    }
}
