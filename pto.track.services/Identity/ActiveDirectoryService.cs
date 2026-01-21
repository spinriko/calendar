using System.DirectoryServices;
using Microsoft.Extensions.Logging;

namespace pto.track.services.Identity;

/// <summary>
/// Production implementation that queries Active Directory using System.DirectoryServices.
/// Requires the machine to be domain-joined or have appropriate network access to the domain controller.
/// </summary>
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger)
    {
        _logger = logger;
    }

    public Task<AdUserAttributes?> GetUserAttributesAsync(string samAccountName)
    {
        try
        {
            // Get the default naming context (domain DN)
            using var rootDse = new DirectoryEntry("LDAP://RootDSE");
            var defaultNamingContext = rootDse.Properties["defaultNamingContext"].Value?.ToString();

            if (string.IsNullOrEmpty(defaultNamingContext))
            {
                _logger.LogError("Failed to retrieve defaultNamingContext from RootDSE");
                return Task.FromResult<AdUserAttributes?>(null);
            }

            using var searchRoot = new DirectoryEntry($"LDAP://{defaultNamingContext}");
            using var searcher = new DirectorySearcher(searchRoot)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={samAccountName}))",
                PropertiesToLoad =
                {
                    "employeeID",
                    "userPrincipalName",
                    "mail",
                    "displayName",
                    "memberOf"
                },
                SearchScope = SearchScope.Subtree
            };

            var result = searcher.FindOne();
            if (result == null)
            {
                _logger.LogWarning("User {SamAccountName} not found in Active Directory", samAccountName);
                return Task.FromResult<AdUserAttributes?>(null);
            }

            var attributes = new AdUserAttributes
            {
                EmployeeId = result.Properties["employeeID"].Count > 0
                    ? result.Properties["employeeID"][0]?.ToString()
                    : null,
                UserPrincipalName = result.Properties["userPrincipalName"].Count > 0
                    ? result.Properties["userPrincipalName"][0]?.ToString()
                    : null,
                Mail = result.Properties["mail"].Count > 0
                    ? result.Properties["mail"][0]?.ToString()
                    : null,
                DisplayName = result.Properties["displayName"].Count > 0
                    ? result.Properties["displayName"][0]?.ToString()
                    : null,
                MemberOf = result.Properties["memberOf"].Count > 0
                    ? result.Properties["memberOf"].Cast<string>().ToList()
                    : new List<string>()
            };

            _logger.LogInformation(
                "AD lookup for {SamAccountName}: employeeID={EmployeeId}, UPN={Upn}, mail={Mail}, displayName={DisplayName}, groups={GroupCount}",
                samAccountName, 
                attributes.EmployeeId ?? "(null)", 
                attributes.UserPrincipalName ?? "(null)",
                attributes.Mail ?? "(null)",
                attributes.DisplayName ?? "(null)",
                attributes.MemberOf.Count);

            return Task.FromResult<AdUserAttributes?>(attributes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Active Directory for {SamAccountName}", samAccountName);
            return Task.FromResult<AdUserAttributes?>(null);
        }
    }
}
