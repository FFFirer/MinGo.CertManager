using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Infrastructure.Services;
using MinGo.CertManager.Web.Extensions;
using Quartz;
using Serilog;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddViteServices();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MinGo.CertManager.Infrastructure")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IDnsProviderRepository, DnsProviderRepository>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IDnsValidationService, AliyunDnsValidationService>();
builder.Services.AddScoped<IAcmeService, AcmeService>();
builder.Services.AddScoped<IAcmeAccountCache, AcmeAccountCache>();
builder.Services.AddScoped<IAliyunDnsService, AliyunDnsService>();

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

await app.MigrateDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if(app.Environment.IsDevelopment())
{
    app.UseViteDevelopmentServer(true);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Log.Information("Application started successfully");

await app.RunAsync();

