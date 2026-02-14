using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Services;

namespace MinGo.CertManager.Web.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Migrating database...");
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migration completed.");

        await IdentitySeedService.SeedAdminUserAsync(app.Services, logger);
    }
}
