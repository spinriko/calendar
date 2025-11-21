using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace pto.track.tests;

public class ImpersonationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImpersonationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetImpersonation_InMockMode_SetsCorrectRole()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient();

        var request = new { role = "Manager" };

        // Act
        var response = await client.PostAsJsonAsync("/api/currentuser/impersonate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal("Manager", result["role"].ToString());

        // Verify cookie was set
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.Contains("MockImpersonation=Manager"));
    }

    [Fact]
    public async Task SetImpersonation_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient();

        var request = new { role = "InvalidRole" };

        // Act
        var response = await client.PostAsJsonAsync("/api/currentuser/impersonate", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetImpersonation_InActiveDirectoryMode_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "ActiveDirectory"
                });
            });
        }).CreateClient();

        var request = new { role = "Manager" };

        // Act
        var response = await client.PostAsJsonAsync("/api/currentuser/impersonate", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Contains("only available in Mock", result["message"].ToString());
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Approver")]
    [InlineData("Employee")]
    public async Task SetImpersonation_WithValidRoles_ReturnsOk(string role)
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient();

        var request = new { role };

        // Act
        var response = await client.PostAsJsonAsync("/api/currentuser/impersonate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClearImpersonation_RemovesCookie()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient();

        // First set impersonation
        await client.PostAsJsonAsync("/api/currentuser/impersonate", new { role = "Manager" });

        // Act
        var response = await client.PostAsync("/api/currentuser/clearimpersonation", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify cookie was deleted (Set-Cookie with expires in the past)
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            Assert.Contains(cookies, c => c.Contains("MockImpersonation") && c.Contains("expires"));
        }
    }

    [Fact]
    public async Task GetCurrentUser_AfterImpersonation_ReturnsImpersonatedUser()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true // Important: preserve cookies between requests
        });

        // Set impersonation to Employee
        await client.PostAsJsonAsync("/api/currentuser/impersonate", new { role = "Employee" });

        // Act
        var response = await client.GetAsync("/api/currentuser");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(user);
        Assert.Equal("EMP001", user["employeeNumber"].ToString());
        Assert.Equal("employee@example.com", user["email"].ToString());
    }

    [Fact]
    public async Task GetCurrentUser_ImpersonatingManager_ReturnsManagerWithCorrectRoles()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Set impersonation to Manager
        await client.PostAsJsonAsync("/api/currentuser/impersonate", new { role = "Manager" });

        // Act
        var response = await client.GetAsync("/api/currentuser");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        Assert.NotNull(user);
        Assert.Equal("MGR001", user["employeeNumber"].GetString());
        Assert.Equal("manager@example.com", user["email"].GetString());
        Assert.Equal("Manager", user["role"].GetString());
        Assert.True(user["isApprover"].GetBoolean());
    }

    [Fact]
    public async Task GetCurrentUser_ImpersonatingAdmin_HasAllRoles()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Set impersonation to Admin
        await client.PostAsJsonAsync("/api/currentuser/impersonate", new { role = "Admin" });

        // Act
        var response = await client.GetAsync("/api/currentuser");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(user);
        Assert.Equal("ADMIN001", user["employeeNumber"].ToString());
        Assert.Equal("admin@example.com", user["email"].ToString());

        // Admin should have all roles
        var roles = user["roles"] as System.Text.Json.JsonElement?;
        Assert.NotNull(roles);
        var roleArray = roles.Value.EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("Admin", roleArray);
        Assert.Contains("Manager", roleArray);
        Assert.Contains("Approver", roleArray);
        Assert.Contains("Employee", roleArray);
    }

    [Fact]
    public async Task GetCurrentUser_InMockMode_ReturnsMockModeFlag()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Mock"
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/currentuser");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        Assert.NotNull(user);
        Assert.True(user["isMockMode"].GetBoolean());
    }
}
