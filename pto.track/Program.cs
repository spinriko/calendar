using pto.track.Middleware;
using pto.track.services;
using pto.track;
using Microsoft.Extensions.Hosting.WindowsServices;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};
var builder = WebApplication.CreateBuilder(options);

// centralize minor configuration differences (local user-secrets, etc.)
builder.ConfigureAppConfiguration();

// centralize service registrations
builder.ConfigureServices();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Required for API controllers

// Add HttpContextAccessor for claims access
builder.Services.AddHttpContextAccessor();

// Add authentication - using cookie scheme for mock authentication in development
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnRedirectToLogin = context =>
        {
            // For API requests, return 401 instead of redirecting
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure database and register application services
builder.Services.AddSchedulerServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure pipeline and map endpoints using centralized helpers
app.ConfigurePipeline();

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
