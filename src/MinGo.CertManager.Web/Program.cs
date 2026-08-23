using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinGo.CertManager.Infrastructure.Configuration;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Core.Services;
using MinGo.CertManager.Infrastructure.Services;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Application.Services;
using MinGo.CertManager.Infrastructure.Jobs;
using MinGo.CertManager.Web.Extensions;
using MinGo.CertManager.Web.Middleware;
using Quartz;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
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

// 替换默认 UserManager 为自定义 ApplicationUserManager（自动赋予 User 角色）
builder.Services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();

// 配置 External Cookie 确保 OAuth 回调时能被正确读取
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IDnsProviderRepository, DnsProviderRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IAcmeService, AcmeService>();
builder.Services.AddScoped<IAcmeAccountCache, AcmeAccountCache>();
builder.Services.AddScoped<IAliyunDnsService, AliyunDnsService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

builder.Services.Configure<AcmeSettings>(
    builder.Configuration.GetSection(AcmeSettings.SectionName));
builder.Services.Configure<CertificateSettings>(
    builder.Configuration.GetSection(CertificateSettings.SectionName));
builder.Services.Configure<AliyunDnsSettings>(
    builder.Configuration.GetSection(AliyunDnsSettings.SectionName));
builder.Services.Configure<QuartzSettings>(
    builder.Configuration.GetSection(QuartzSettings.SectionName));
builder.Services.Configure<ApiKeySettings>(
    builder.Configuration.GetSection(ApiKeySettings.SectionName));
builder.Services.Configure<ForwardedHeadersSettings>(
    builder.Configuration.GetSection(ForwardedHeadersSettings.SectionName));

builder.Services.AddQuartz(q =>
{
    var quartzSettings = builder.Configuration.GetSection(QuartzSettings.SectionName).Get<QuartzSettings>();
    q.SchedulerId = quartzSettings?.SchedulerInstanceId ?? "MinGo-CertManager-Scheduler";
    q.SchedulerName = quartzSettings?.SchedulerName ?? "MinGo CertManager Scheduler";
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();

    var certificateJobKey = new JobKey("CertificateRenewalJob");
    q.AddJob<CertificateRenewalJob>(certificateJobKey, job =>
    {
        job.WithDescription("扫描并续签即将过期的证书");
    });

    q.AddTrigger(trigger => trigger
        .ForJob(certificateJobKey)
        .WithIdentity("CertificateRenewalTrigger")
        .WithDescription("每日执行一次证书续签扫描")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule
            .WithIntervalInHours(24)
            .RepeatForever()));

    // CertificateRequestJob 由控制器/页面动态创建并触发，无需在此预注册
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// 注册第三方 OAuth 登录提供程序（GitHub、Google 等）
builder.Services.AddOAuthLoginProviders(builder.Configuration);

var app = builder.Build();

// 反向代理支持：按配置启用 ForwardedHeaders 中间件
// 当部署在 nginx/Caddy/Traefik 后面时，设置 ForwardedHeaders.Enabled=true
// 应用自动读取 X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host 头
var forwardedHeadersSettings = app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value;
app.Logger.LogInformation("Forwarded Headers Enabled: {Enabled}", forwardedHeadersSettings.Enabled);
if (forwardedHeadersSettings.Enabled)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto
                         | ForwardedHeaders.XForwardedHost,
        KnownIPNetworks = { },
        KnownProxies = { }
    });
}


await app.MigrateDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseViteDevelopmentServer(true);
}
if (!forwardedHeadersSettings.Enabled)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 添加API密钥认证中间件（在 Antiforgery 之前，因为 ApiKey 不经过 CSRF）
app.UseApiKeyAuthentication();

app.UseAntiforgery();

app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

Log.Information("Application started successfully");

await app.RunAsync();

