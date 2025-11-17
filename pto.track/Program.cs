using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Project.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure DB provider - use SQL Server
var connStr = builder.Configuration.GetConnectionString("SchedulerDbContext");

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<SchedulerDbContext>(options => options.UseSqlServer(connStr));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

// Ensure the database is migrated/created at startup.
using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchedulerDbContext>();
        // Prefer migrations when available so schema upgrades work on deployment
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred migrating or creating the DB.");
    }
}

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
