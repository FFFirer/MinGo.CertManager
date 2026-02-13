using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Infrastructure.Data;

namespace MinGo.CertManager.Web.Extensions;

public static class MigrationExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Migrating database...");
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        logger.LogInformation("Database migration completed.");
    }
}
