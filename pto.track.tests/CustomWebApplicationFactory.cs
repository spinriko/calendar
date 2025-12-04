using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using pto.track;
using pto.track.data;
using pto.track.services.Authentication;
using pto.track.tests.Mocks;

namespace pto.track.tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("environment", "Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override connection string to prevent using real database
                var dict = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PtoTrackDbContext"] = string.Empty
                };
                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PtoTrackDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextImplDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PtoTrackDbContext));
                if (dbContextImplDescriptor != null)
                {
                    services.Remove(dbContextImplDescriptor);
                }

                // Add in-memory database for testing
                var dbName = "TestDb_" + Guid.NewGuid().ToString();
                services.AddDbContext<PtoTrackDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                // Remove existing IUserClaimsProvider registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserClaimsProvider));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                // Register TestUserClaimsProvider for tests
                services.AddScoped<IUserClaimsProvider, TestUserClaimsProvider>();

                // Add test authentication scheme
                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });

                // Ensure database is created and seeded
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }
    }

    // Test authentication handler for injecting role claims
    public class TestAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            Microsoft.AspNetCore.Authentication.ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Context.Request.Headers["X-Test-Role"].ToString();
            var claims = new List<System.Security.Claims.Claim>();
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
            }
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }
}
