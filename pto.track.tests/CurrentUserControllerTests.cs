using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using pto.track.services.Authentication;
using Moq;
using pto.track.Controllers;
using pto.track.services;
using pto.track.services.Identity;
using Xunit;

namespace pto.track.tests
{
    public class CurrentUserControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CurrentUserControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetCurrentUser_UsesClaimRoles_WhenMockMode()
        {
            // Arrange: create controller with mocks to assert role priority
            var mockClaims = new Mock<IUserClaimsProvider>();
            mockClaims.Setup(x => x.IsAuthenticated()).Returns(true);
            mockClaims.Setup(x => x.GetRoles()).Returns(new[] { "Manager", "Employee" });

            var mockUserSync = new Mock<IUserSyncService>();
            var resource = new pto.track.data.Resource { Id = 1, Name = "Test", Role = "Employee", IsApprover = false };
            mockUserSync.Setup(x => x.EnsureCurrentUserExistsAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(resource);

            var mockResourceService = new Mock<IResourceService>().Object;
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Mock"
            }).Build();

            var enricher = new Mock<IIdentityEnricher>();
            enricher.Setup(e => e.EnrichAsync(It.IsAny<string>(), default)).ReturnsAsync(new Dictionary<string, string?>());

            var controller = new CurrentUserController(mockClaims.Object, mockUserSync.Object, mockResourceService, config, enricher.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };

            // Act
            var result = await controller.GetCurrentUser();

            // Assert: serialize the anonymous result to JSON and inspect
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("Manager", root.GetProperty("role").GetString());
            Assert.True(root.GetProperty("isApprover").GetBoolean());
        }

        [Fact]
        public async Task GetAllClaims_IncludesEnriched_WhenIdentityPresent()
        {
            // Arrange: instantiate controller directly with a TestEnricher and an authenticated user
            var mockClaims = new Mock<IUserClaimsProvider>();
            mockClaims.Setup(x => x.IsAuthenticated()).Returns(true);
            mockClaims.Setup(x => x.GetRoles()).Returns(new string[] { "Employee" });

            var mockUserSync = new Mock<IUserSyncService>();
            mockUserSync.Setup(x => x.EnsureCurrentUserExistsAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new pto.track.data.Resource { Id = 1, Name = "T", Role = "Employee", IsApprover = false });

            var mockResourceService = new Mock<IResourceService>().Object;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Mode"] = "Mock" }).Build();
            var enricher = new TestEnricher();

            var controller = new CurrentUserController(mockClaims.Object, mockUserSync.Object, mockResourceService, config, enricher);
            var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var identity = new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "DOMAIN\\TestUser") }, "TestAuth");
            ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            // Act
            var result = await controller.GetAllClaims();

            // Assert via JSON
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("Enriched", out var enrichedElem));
            Assert.Equal("extra-value", enrichedElem.GetProperty("someKey").GetString());
        }

        private class TestEnricher : IIdentityEnricher
        {
            public Task<IDictionary<string, string?>> EnrichAsync(string normalizedIdentity, System.Threading.CancellationToken cancellationToken = default)
            {
                IDictionary<string, string?> dict = new Dictionary<string, string?> { ["someKey"] = "extra-value" };
                return Task.FromResult(dict);
            }
        }
    }
}
