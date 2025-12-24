using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace pto.track.tests;

public class TestIdentityEnricherIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TestIdentityEnricherIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task XTestClaims_WithRoleAdmin_AllowsAccess()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "role=Admin");

        // Call the debug claims endpoint to verify the enricher added the role claim
        var response = await client.GetAsync("/api/currentuser/debug/claims");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Admin", json);
    }

    [Fact]
    public async Task XTestClaims_NoRole_Forbidden()
    {
        var client = _factory.CreateClient();
        // No X-Test-Claims header -> should be treated as non-admin
        var response = await client.GetAsync("/api/groups");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
