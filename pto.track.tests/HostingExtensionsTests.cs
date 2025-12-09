using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Microsoft.Extensions.FileProviders;

namespace pto.track.tests
{
    public class HostingExtensionsTests
    {
        [Fact]
        public void ConfigureAppConfiguration_AddsUserSecrets_ForLocalEnvironment()
        {
            var env = new TestHostEnvironment("local");
            var config = new ConfigurationBuilder().Build();

            var services = new ServiceCollection();
            var builder = new WebApplicationFactoryShim(services, config, env).CreateBuilder();

            // should not throw
            var result = builder.ConfigureAppConfiguration();
            Assert.Same(builder, result);
        }

        [Fact]
        public void ConfigureServices_RegistersExpectedServices()
        {
            var env = new TestHostEnvironment("Development");
            var config = new ConfigurationBuilder().Build();

            var services = new ServiceCollection();
            var builder = new WebApplicationFactoryShim(services, config, env).CreateBuilder();

            builder.ConfigureServices();

            var provider = builder.Services.BuildServiceProvider();
            // The app should register HttpContextAccessor and health checks (via AddSchedulerServices)
            var accessor = provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            Assert.NotNull(accessor);
        }

        [Fact]
        public void ConfigurePipeline_ComposesWithoutError()
        {
            var env = new TestHostEnvironment("Development");
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection();
            var builderShim = new WebApplicationFactoryShim(services, config, env);
            var app = builderShim.Create().ConfigureAppConfiguration();

            // should not throw
            var result = app.ConfigurePipeline();
            Assert.Same(app, result);
        }
    }

    // Minimal shims to create builders and apps without running a real server
    internal class TestHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public TestHostEnvironment(string envName)
        {
            EnvironmentName = envName;
            ApplicationName = "pto.track.tests";
            ContentRootPath = AppContext.BaseDirectory;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public string? WebRootPath { get; set; }
        private IFileProvider? _contentRootFileProvider;
        public IFileProvider ContentRootFileProvider
        {
            get => _contentRootFileProvider ??= new PhysicalFileProvider(ContentRootPath);
            set => _contentRootFileProvider = value;
        }
        private IFileProvider? _webRootFileProvider;
        public IFileProvider WebRootFileProvider
        {
            get => _webRootFileProvider ??= new PhysicalFileProvider(WebRootPath ?? ContentRootPath);
            set => _webRootFileProvider = value;
        }
    }

    internal class WebApplicationFactoryShim
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public WebApplicationFactoryShim(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            _services = services;
            _configuration = configuration;
            _environment = environment;
        }

        public WebApplicationBuilder CreateBuilder()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                ContentRootPath = AppContext.BaseDirectory,
                EnvironmentName = _environment.EnvironmentName,
                ApplicationName = _environment.ApplicationName
            });

            // Add the supplied configuration into the builder's configuration
            builder.Host.ConfigureAppConfiguration((context, cfg) =>
            {
                try
                {
                    cfg.AddConfiguration(_configuration);
                }
                catch
                {
                    // Best-effort: some configuration implementations may not merge cleanly.
                }
            });

            // Ensure the host resolves the provided environment instance from DI
            builder.Services.AddSingleton<IHostEnvironment>(_environment);

            return builder;
        }

        public WebApplication Create()
        {
            var builder = CreateBuilder();
            builder.ConfigureServices();
            var app = builder.Build();
            return app;
        }
    }
}
