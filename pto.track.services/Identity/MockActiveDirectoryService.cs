using Microsoft.Extensions.Logging;

namespace pto.track.services.Identity;

/// <summary>
/// Mock implementation of IActiveDirectoryService for testing.
/// Returns predefined test data for specific usernames.
/// </summary>
public class MockActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<MockActiveDirectoryService> _logger;
    private readonly Dictionary<string, AdUserAttributes> _testUsers;

    public MockActiveDirectoryService(ILogger<MockActiveDirectoryService> logger)
    {
        _logger = logger;
        _testUsers = new Dictionary<string, AdUserAttributes>(StringComparer.OrdinalIgnoreCase)
        {
            ["testuser"] = new AdUserAttributes
            {
                EmployeeId = "12345",
                UserPrincipalName = "testuser@example.com",
                Mail = "testuser@example.com",
                DisplayName = "Test User",
                MemberOf = new List<string> { "CN=Employees,OU=Groups,DC=example,DC=com" }
            },
            ["manager"] = new AdUserAttributes
            {
                EmployeeId = "67890",
                UserPrincipalName = "manager@example.com",
                Mail = "manager@example.com",
                DisplayName = "Test Manager",
                MemberOf = new List<string>
                {
                    "CN=Managers,OU=Groups,DC=example,DC=com",
                    "CN=Approvers,OU=Groups,DC=example,DC=com"
                }
            },
            ["admin"] = new AdUserAttributes
            {
                EmployeeId = "99999",
                UserPrincipalName = "admin@example.com",
                Mail = "admin@example.com",
                DisplayName = "Test Administrator",
                MemberOf = new List<string>
                {
                    "CN=Domain Admins,OU=Groups,DC=example,DC=com",
                    "CN=Administrators,OU=Groups,DC=example,DC=com"
                }
            }
        };
    }

    public Task<AdUserAttributes?> GetUserAttributesAsync(string samAccountName)
    {
        _logger.LogDebug("MockActiveDirectoryService: Looking up {SamAccountName}", samAccountName);
        
        if (_testUsers.TryGetValue(samAccountName, out var attributes))
        {
            _logger.LogDebug("MockActiveDirectoryService: Found test user {SamAccountName} with employeeID {EmployeeId}",
                samAccountName, attributes.EmployeeId);
            return Task.FromResult<AdUserAttributes?>(attributes);
        }

        _logger.LogDebug("MockActiveDirectoryService: User {SamAccountName} not found in test data", samAccountName);
        return Task.FromResult<AdUserAttributes?>(null);
    }

    /// <summary>
    /// Adds or updates a test user in the mock directory.
    /// </summary>
    public void AddTestUser(string samAccountName, AdUserAttributes attributes)
    {
        _testUsers[samAccountName] = attributes;
    }

    /// <summary>
    /// Removes a test user from the mock directory.
    /// </summary>
    public void RemoveTestUser(string samAccountName)
    {
        _testUsers.Remove(samAccountName);
    }

    /// <summary>
    /// Clears all test users from the mock directory.
    /// </summary>
    public void ClearTestUsers()
    {
        _testUsers.Clear();
    }
}
