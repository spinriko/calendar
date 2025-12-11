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
            // Ensure the test host runs in the Testing environment so app-level
            // environment checks (like registering InMemory DB) trigger reliably.
            // Also set the process environment variable early so EF model-level
            // seeding (which checks ASPNETCORE_ENVIRONMENT) behaves the same.
            System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove existing IUserClaimsProvider registration (so tests can inject test provider)
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

                // If running under Testing environment, ensure the app uses a shared InMemoryDatabaseRoot
                // so test code and the app share the same in-memory database instance.
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
                {
                    // Register a shared InMemoryDatabaseRoot and replace the IDbContextStrategy
                    var root = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
                    services.AddSingleton(root);

                    // Replace the registered IDbContextStrategy (if present) with one that uses the shared root
                    var stratDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(pto.track.services.DbContextStrategies.IDbContextStrategy));
                    if (stratDescriptor != null)
                    {
                        services.Remove(stratDescriptor);
                    }
                    services.AddSingleton<pto.track.services.DbContextStrategies.IDbContextStrategy>(
                        new pto.track.services.DbContextStrategies.InMemoryDbContextStrategy(dbName: "PtoTrack_Testing", root: root));
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
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder) { }

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
