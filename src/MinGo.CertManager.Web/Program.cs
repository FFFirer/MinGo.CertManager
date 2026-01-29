using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Jobs;
using MinGo.CertManager.Infrastructure.Quartz;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Infrastructure.Services;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting MinGo.CertManager.Web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddHttpClient();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("MinGo.CertManager.Infrastructure")));

    builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
    builder.Services.AddScoped<IDnsProviderRepository, DnsProviderRepository>();
    builder.Services.AddScoped<ICertificateService, CertificateService>();
    builder.Services.AddScoped<IDnsValidationService, AliyunDnsValidationService>();
    builder.Services.AddScoped<IAcmeService, AcmeService>();

    builder.Services.Configure<AcmeSettings>(
        builder.Configuration.GetSection(AcmeSettings.SectionName));
    builder.Services.Configure<CertificateSettings>(
        builder.Configuration.GetSection(CertificateSettings.SectionName));
    builder.Services.Configure<AliyunDnsSettings>(
        builder.Configuration.GetSection(AliyunDnsSettings.SectionName));
    builder.Services.Configure<QuartzSettings>(
        builder.Configuration.GetSection(QuartzSettings.SectionName));

    builder.Services.AddQuartz(q =>
    {
        var quartzSettings = builder.Configuration.GetSection(QuartzSettings.SectionName).Get<QuartzSettings>();
        q.SchedulerId = quartzSettings?.SchedulerInstanceId ?? "MinGo-CertManager-Scheduler";
        q.SchedulerName = quartzSettings?.SchedulerName ?? "MinGo CertManager Scheduler";
        q.UseSimpleTypeLoader();
        q.UseInMemoryStore();
    });

    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    var app = builder.Build();

    var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.MapControllers();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    Log.Information("Application started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
