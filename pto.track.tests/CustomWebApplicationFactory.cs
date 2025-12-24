using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using pto.track;
using pto.track.data;
using pto.track.services.Authentication;
using pto.track.tests.Mocks;
using Microsoft.AspNetCore.Builder;

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

            // Ensure tests run with Mock authentication mode regardless of
            // appsettings.json to avoid registering Negotiate (Kestrel-only)
            // authentication handlers in the test server.
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
                {
                    var dict = new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["Authentication:Mode"] = "Mock"
                    };
                    config.AddInMemoryCollection(dict!);
                }
            });

            builder.ConfigureTestServices(services =>
            {
                // Remove existing IUserClaimsProvider registration (so tests can inject test provider)
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserClaimsProvider));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                // Register TestUserClaimsProvider for tests
                services.AddScoped<IUserClaimsProvider, TestUserClaimsProvider>();

                // Ensure IHttpContextAccessor is available for TestIdentityEnricher
                services.AddHttpContextAccessor();

                // Ensure a safe IIdentityEnricher is registered for tests so enrichment
                // doesn't attempt external lookups or throw. Replace any existing
                // registration with the NoOp implementation used in the app by default.
                var enricherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(pto.track.services.Identity.IIdentityEnricher));
                if (enricherDescriptor != null)
                {
                    services.Remove(enricherDescriptor);
                }
                // Use a test implementation for IIdentityEnricher which reads X-Test-Claims
                // so tests can drive enriched attribute responses without external dependencies.
                services.AddSingleton<pto.track.services.Identity.IIdentityEnricher, TestIIdentityEnricher>();

                // Register a test ClaimsTransformation that can augment the ClaimsPrincipal
                // from headers like X-Test-Claims. This runs after authentication and
                // before authorization so tests can control roles/claims per-request.
                services.AddSingleton<Microsoft.AspNetCore.Authentication.IClaimsTransformation, TestIdentityEnricher>();

                // Add test authentication scheme and make it the default for tests
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });

                // Ensure Test is the effective default even if the app registered
                // other schemes (like Cookies) earlier. Post-configure authentication
                // options so the Test scheme is chosen during authorization.
                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(opts =>
                {
                    opts.DefaultAuthenticateScheme = "Test";
                    opts.DefaultChallengeScheme = "Test";
                    opts.DefaultSignInScheme = "Test";
                });

                // Previously we injected a startup filter to set HttpContext.User from headers.
                // With the ClaimsTransformation approach we no longer need that startup filter.

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
            // Minimal test authentication: mark the request authenticated but do not
            // inject role claims here. `IClaimsTransformation` will apply role/claim
            // enrichment controlled by `X-Test-Claims`.
            var identity = new System.Security.Claims.ClaimsIdentity(authenticationType: "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }
}
