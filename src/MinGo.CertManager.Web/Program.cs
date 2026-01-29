using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinGo.CertManager.Infrastructure.Data;
using MinGo.CertManager.Infrastructure.Jobs;
using MinGo.CertManager.Infrastructure.Quartz;
using MinGo.CertManager.Infrastructure.Repositories;
using MinGo.CertManager.Infrastructure.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MinGo.CertManager.Infrastructure")));

builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IDnsProviderRepository, DnsProviderRepository>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IDnsValidationService, AliyunDnsValidationService>();

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "MinGo-CertManager-Scheduler";
    q.SchedulerName = "MinGo CertManager Scheduler";
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
