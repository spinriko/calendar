using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pto.track.data;
using pto.track.Models;
using Xunit;

namespace pto.track.tests;

/// <summary>
/// Integration tests for the impersonation feature.
/// Ensures that when impersonation is active, all API endpoints return data
/// for the impersonated user, not the actual authenticated user.
/// </summary>
public class ImpersonationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImpersonationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private CancellationToken GetTimeoutToken()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return cts.Token;
    }

    private HttpClient GetClientWithMockAuth(Action<PtoTrackDbContext>? seed = null)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PtoTrackDbContext"] = string.Empty,
                    ["Authentication:Mode"] = "Mock" // Enable impersonation
                };
                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<PtoTrackDbContext>) ||
                    d.ServiceType == typeof(PtoTrackDbContext) ||
                    (d.ImplementationType != null && d.ImplementationType == typeof(PtoTrackDbContext))
                ).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                var dbName = "ImpersonationTestDb_" + Guid.NewGuid().ToString();
                services.AddDbContext<PtoTrackDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
                    db.Database.EnsureCreated();

                    if (seed != null)
                    {
                        db.Resources.RemoveRange(db.Resources);
                        db.SaveChanges();
                        seed(db);
                    }
                }
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true  // This makes the client automatically handle cookies like a browser!
        });

        return client;
    }

    private void SeedTestUsers(PtoTrackDbContext db)
    {
        db.Resources.AddRange(
            new Resource
            {
                Id = 1,
                Name = "Test Employee 1",
                Email = "employee@test.com",
                EmployeeNumber = "EMP001",
                Role = "Employee",
                IsApprover = false,
                IsActive = true
            },
            new Resource
            {
                Id = 2,
                Name = "Test Employee 2",
                Email = "employee2@test.com",
                EmployeeNumber = "EMP002",
                Role = "Employee",
                IsApprover = false,
                IsActive = true
            },
            new Resource
            {
                Id = 3,
                Name = "Test Manager",
                Email = "manager@test.com",
                EmployeeNumber = "MGR001",
                Role = "Manager",
                IsApprover = true,
                IsActive = true
            },
            new Resource
            {
                Id = 4,
                Name = "Test Approver",
                Email = "approver@test.com",
                EmployeeNumber = "APR001",
                Role = "Approver",
                IsApprover = true,
                IsActive = true
            },
            new Resource
            {
                Id = 5,
                Name = "Administrator",
                Email = "admin@test.com",
                EmployeeNumber = "ADMIN001",
                Role = "Admin",
                IsApprover = true,
                IsActive = true
            }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task CurrentUser_WithoutImpersonation_ReturnsDefaultUser()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Act
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Default mock user is EMP001 (Test Employee 1)
        Assert.Equal("EMP001", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Employee", user.GetProperty("role").GetString());
    }

    [Fact]
    public async Task CurrentUser_WithImpersonation_ReturnsImpersonatedUser()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Set impersonation to EMP002 (Test Employee - Employee role only)
        var impersonationRequest = new
        {
            employeeNumber = "EMP002",
            roles = new[] { "Employee" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Should return impersonated user (EMP002)
        Assert.Equal("EMP002", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Employee", user.GetProperty("role").GetString());
        Assert.False(user.GetProperty("isApprover").GetBoolean());

        // Verify roles array contains only Employee
        var roles = user.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString())
            .ToList();
        Assert.Single(roles);
        Assert.Contains("Employee", roles);
    }

    [Fact]
    public async Task CurrentUser_ImpersonatedAsManager_ReturnsManagerRole()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Set impersonation to MGR001 (Test Manager - Employee + Manager roles)
        var impersonationRequest = new
        {
            employeeNumber = "MGR001",
            roles = new[] { "Employee", "Manager" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("MGR001", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Manager", user.GetProperty("role").GetString()); // Highest priority role
        Assert.True(user.GetProperty("isApprover").GetBoolean());

        // Verify roles array contains Employee and Manager
        var roles = user.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString())
            .ToList();
        Assert.Contains("Employee", roles);
        Assert.Contains("Manager", roles);
    }

    [Fact]
    public async Task CurrentUser_ImpersonatedAsApprover_ReturnsApproverRole()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Set impersonation to APR001 (Test Approver - Employee + Approver roles)
        var impersonationRequest = new
        {
            employeeNumber = "APR001",
            roles = new[] { "Employee", "Approver" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("APR001", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Approver", user.GetProperty("role").GetString());
        Assert.True(user.GetProperty("isApprover").GetBoolean());

        // Verify roles array
        var roles = user.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString())
            .ToList();
        Assert.Contains("Employee", roles);
        Assert.Contains("Approver", roles);
    }

    [Fact]
    public async Task CurrentUser_ImpersonatedAsAdmin_ReturnsAdminRole()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Set impersonation to ADMIN001 (Administrator - all roles including Admin)
        var impersonationRequest = new
        {
            employeeNumber = "ADMIN001",
            roles = new[] { "Employee", "Manager", "Approver", "Admin" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ADMIN001", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Admin", user.GetProperty("role").GetString()); // Admin is highest priority
        Assert.True(user.GetProperty("isApprover").GetBoolean());

        // Verify roles array contains all roles
        var roles = user.GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString())
            .ToList();
        Assert.Contains("Employee", roles);
        Assert.Contains("Manager", roles);
        Assert.Contains("Approver", roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public async Task Impersonation_EmployeeCanOnlyViewOwnAbsences()
    {
        // Arrange
        var client = GetClientWithMockAuth(db =>
        {
            SeedTestUsers(db);

            // Add absences for different employees
            db.AbsenceRequests.AddRange(
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 2, // EMP002's absence (pending)
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddDays(1),
                    Reason = "Employee's own absence",
                    Status = AbsenceStatus.Pending
                },
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 3, // MGR001's absence (pending - should NOT see)
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddDays(1),
                    Reason = "Manager's pending absence",
                    Status = AbsenceStatus.Pending
                },
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 3, // MGR001's absence (approved - SHOULD see)
                    Start = DateTime.UtcNow.AddDays(2),
                    End = DateTime.UtcNow.AddDays(3),
                    Reason = "Manager's approved absence",
                    Status = AbsenceStatus.Approved
                }
            );
            db.SaveChanges();
        });

        // Impersonate as EMP002 (Employee)
        var impersonationRequest = new
        {
            employeeNumber = "EMP002",
            roles = new[] { "Employee" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act - Get absences
        var response = await client.GetAsync($"/api/absences?start={DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}&end={DateTime.UtcNow.AddDays(7):yyyy-MM-dd}", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var absences = await response.Content.ReadFromJsonAsync<JsonElement>();
        var absenceArray = absences.EnumerateArray().ToList();

        // Employee should see:
        // 1. Their own pending absence (EmployeeId = 2)
        // 2. Manager's approved absence (EmployeeId = 3, Status = Approved)
        // NOT: Manager's pending absence
        Assert.Equal(2, absenceArray.Count);
        Assert.Contains(absenceArray, a => a.GetProperty("employeeId").GetInt32() == 2);
        Assert.Contains(absenceArray, a =>
            a.GetProperty("employeeId").GetInt32() == 3 &&
            a.GetProperty("status").GetString() == "Approved");
    }

    [Fact]
    public async Task Impersonation_ManagerCanViewAllAbsences()
    {
        // Arrange
        var client = GetClientWithMockAuth(db =>
        {
            SeedTestUsers(db);

            db.AbsenceRequests.AddRange(
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 2,
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddDays(1),
                    Reason = "Employee pending absence",
                    Status = AbsenceStatus.Pending
                },
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 3,
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddDays(1),
                    Reason = "Manager approved absence",
                    Status = AbsenceStatus.Approved
                },
                new AbsenceRequest
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = 4,
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddDays(1),
                    Reason = "Approver rejected absence",
                    Status = AbsenceStatus.Rejected
                }
            );
            db.SaveChanges();
        });

        // Impersonate as MGR001 (Manager)
        var impersonationRequest = new
        {
            employeeNumber = "MGR001",
            roles = new[] { "Employee", "Manager" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act - Get absences
        var response = await client.GetAsync($"/api/absences?start={DateTime.UtcNow:yyyy-MM-dd}&end={DateTime.UtcNow.AddDays(7):yyyy-MM-dd}", GetTimeoutToken());

        // Assert
        response.EnsureSuccessStatusCode();
        var absences = await response.Content.ReadFromJsonAsync<JsonElement>();
        var absenceArray = absences.EnumerateArray().ToList();

        // Manager should see all absences (Pending, Approved, Rejected)
        Assert.Equal(3, absenceArray.Count);
        Assert.Contains(absenceArray, a => a.GetProperty("status").GetString() == "Pending");
        Assert.Contains(absenceArray, a => a.GetProperty("status").GetString() == "Approved");
        Assert.Contains(absenceArray, a => a.GetProperty("status").GetString() == "Rejected");
    }

    [Fact]
    public async Task Impersonation_RolePriorityCorrect()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Test Admin has highest priority
        var adminRequest = new { employeeNumber = "ADMIN001", roles = new[] { "Employee", "Manager", "Approver", "Admin" } };
        var adminResponse = await client.PostAsJsonAsync("/api/impersonation", adminRequest, GetTimeoutToken());
        adminResponse.EnsureSuccessStatusCode();

        var adminUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", adminUser.GetProperty("role").GetString());

        // Test Manager priority over Approver and Employee
        var managerRequest = new { employeeNumber = "MGR001", roles = new[] { "Employee", "Manager" } };
        var managerResponse = await client.PostAsJsonAsync("/api/impersonation", managerRequest, GetTimeoutToken());
        managerResponse.EnsureSuccessStatusCode();

        var managerUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Manager", managerUser.GetProperty("role").GetString());

        // Test Approver priority over Employee
        var approverRequest = new { employeeNumber = "APR001", roles = new[] { "Employee", "Approver" } };
        var approverResponse = await client.PostAsJsonAsync("/api/impersonation", approverRequest, GetTimeoutToken());
        approverResponse.EnsureSuccessStatusCode();

        var approverUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approver", approverUser.GetProperty("role").GetString());
    }
    [Fact]
    public async Task Impersonation_SwitchingUsers_UpdatesContextCorrectly()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Start as Employee
        var empRequest = new { employeeNumber = "EMP002", roles = new[] { "Employee" } };
        var empResponse = await client.PostAsJsonAsync("/api/impersonation", empRequest, GetTimeoutToken());
        empResponse.EnsureSuccessStatusCode();

        var empUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMP002", empUser.GetProperty("employeeNumber").GetString());
        Assert.Equal("Employee", empUser.GetProperty("role").GetString());

        // Switch to Manager
        var mgrRequest = new { employeeNumber = "MGR001", roles = new[] { "Employee", "Manager" } };
        var mgrResponse = await client.PostAsJsonAsync("/api/impersonation", mgrRequest, GetTimeoutToken());
        mgrResponse.EnsureSuccessStatusCode();

        var mgrUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MGR001", mgrUser.GetProperty("employeeNumber").GetString());
        Assert.Equal("Manager", mgrUser.GetProperty("role").GetString());
        Assert.True(mgrUser.GetProperty("isApprover").GetBoolean());

        // Switch to Admin
        var adminRequest = new { employeeNumber = "ADMIN001", roles = new[] { "Employee", "Manager", "Approver", "Admin" } };
        var adminResponse = await client.PostAsJsonAsync("/api/impersonation", adminRequest, GetTimeoutToken());
        adminResponse.EnsureSuccessStatusCode();

        var adminUser = await (await client.GetAsync("/api/currentuser", GetTimeoutToken())).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ADMIN001", adminUser.GetProperty("employeeNumber").GetString());
        Assert.Equal("Admin", adminUser.GetProperty("role").GetString());
    }

    [Fact]
    public async Task ClearImpersonation_ResetsToDefaultUser()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        // Set impersonation
        var impersonationRequest = new
        {
            employeeNumber = "EMP002",
            roles = new[] { "Employee" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Verify impersonation is active
        var impersonatedResponse = await client.GetAsync("/api/currentuser", GetTimeoutToken());
        var impersonatedUser = await impersonatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMP002", impersonatedUser.GetProperty("employeeNumber").GetString());

        // Act - Clear impersonation
        var clearResponse = await client.DeleteAsync("/api/impersonation", GetTimeoutToken());
        clearResponse.EnsureSuccessStatusCode();

        // Assert - Should return to default user
        var response = await client.GetAsync("/api/currentuser", GetTimeoutToken());
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("EMP001", user.GetProperty("employeeNumber").GetString());
        Assert.Equal("Employee", user.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Impersonation_PersistsAcrossMultipleRequests()
    {
        // Arrange
        var client = GetClientWithMockAuth(SeedTestUsers);

        var impersonationRequest = new
        {
            employeeNumber = "MGR001",
            roles = new[] { "Employee", "Manager" }
        };
        var setResponse = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());
        setResponse.EnsureSuccessStatusCode();

        // Act - Make multiple requests
        var response1 = await client.GetAsync("/api/currentuser", GetTimeoutToken());
        var user1 = await response1.Content.ReadFromJsonAsync<JsonElement>();

        var response2 = await client.GetAsync("/api/currentuser", GetTimeoutToken());
        var user2 = await response2.Content.ReadFromJsonAsync<JsonElement>();

        var response3 = await client.GetAsync("/api/currentuser", GetTimeoutToken());
        var user3 = await response3.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - All requests should return the same impersonated user
        Assert.Equal("MGR001", user1.GetProperty("employeeNumber").GetString());
        Assert.Equal("MGR001", user2.GetProperty("employeeNumber").GetString());
        Assert.Equal("MGR001", user3.GetProperty("employeeNumber").GetString());

        Assert.Equal("Manager", user1.GetProperty("role").GetString());
        Assert.Equal("Manager", user2.GetProperty("role").GetString());
        Assert.Equal("Manager", user3.GetProperty("role").GetString());
    }
    [Fact]
    public async Task Impersonation_OnlyWorksInMockMode()
    {
        // Arrange - Create client with Authentication:Mode != Mock
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            // Set environment to Testing to prevent Program.cs from registering SQL Server
            builder.UseSetting("environment", "Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "Production" // Not Mock
                };
                config.AddInMemoryCollection(dict);
            });

            // Configure InMemory DB
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<PtoTrackDbContext>(options =>
                {
                    options.UseInMemoryDatabase("ImpersonationTestDb_ProdMode_" + Guid.NewGuid().ToString());
                });
            });
        });

        var client = factory.CreateClient();

        // Act
        var impersonationRequest = new
        {
            employeeNumber = "EMP002",
            roles = new[] { "Employee" }
        };
        var response = await client.PostAsJsonAsync("/api/impersonation", impersonationRequest, GetTimeoutToken());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Mock authentication mode", content);
    }
}
