using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using pto.track.services.Identity;

namespace pto.track
{
    /// <summary>
    /// Claims enricher that queries Active Directory for additional user attributes
    /// (employeeID, UPN, email, displayName, memberOf groups) and adds them as claims.
    /// </summary>
    public class ClaimsEnricher : IClaimsTransformation
    {
        private readonly ILogger<ClaimsEnricher> _logger;
        private readonly IActiveDirectoryService _adService;

        public ClaimsEnricher(ILogger<ClaimsEnricher> logger, IActiveDirectoryService adService)
        {
            _logger = logger;
            _adService = adService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Only enrich if we have a Windows identity name (DOMAIN\username)
            if (principal.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(principal.Identity.Name))
            {
                return principal;
            }

            // Check if already enriched to avoid duplicate AD lookups
            if (principal.HasClaim(c => c.Type == "ad_enriched"))
            {
                return principal;
            }

            try
            {
                // Extract sAMAccountName from DOMAIN\username
                var identityName = principal.Identity.Name;
                var samAccountName = identityName.Contains('\\')
                    ? identityName.Split('\\')[1]
                    : identityName;

                _logger.LogInformation("Enriching claims for user {SamAccountName}", samAccountName);

                // Query AD for user attributes
                var adAttributes = await _adService.GetUserAttributesAsync(samAccountName);

                if (adAttributes != null)
                {
                    var identity = (ClaimsIdentity)principal.Identity;

                    // Add employeeID as primary identifier for ADP matching
                    if (!string.IsNullOrEmpty(adAttributes.EmployeeId))
                    {
                        identity.AddClaim(new Claim("employeeID", adAttributes.EmployeeId));
                        identity.AddClaim(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/employeenumber", adAttributes.EmployeeId));
                    }

                    // Add UPN
                    if (!string.IsNullOrEmpty(adAttributes.UserPrincipalName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Upn, adAttributes.UserPrincipalName));
                    }

                    // Add email if not already present
                    if (!string.IsNullOrEmpty(adAttributes.Mail) && !principal.HasClaim(c => c.Type == ClaimTypes.Email))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, adAttributes.Mail));
                    }

                    // Add display name
                    if (!string.IsNullOrEmpty(adAttributes.DisplayName))
                    {
                        identity.AddClaim(new Claim("displayName", adAttributes.DisplayName));
                        identity.AddClaim(new Claim(ClaimTypes.GivenName, adAttributes.DisplayName));
                    }

                    // Add group memberships
                    if (adAttributes.MemberOf != null)
                    {
                        foreach (var group in adAttributes.MemberOf)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, group));
                        }
                        
                        _logger.LogDebug("Added {GroupCount} group memberships for {SamAccountName}: {Groups}",
                            adAttributes.MemberOf.Count, 
                            samAccountName,
                            string.Join(", ", adAttributes.MemberOf.Select(g => g.Split(',')[0]))); // Just CN= part
                    }

                    // Mark as enriched
                    identity.AddClaim(new Claim("ad_enriched", "true"));

                    _logger.LogInformation("Successfully enriched claims for {SamAccountName} with employeeID {EmployeeId}, UPN {Upn}, email {Email}",
                        samAccountName, 
                        adAttributes.EmployeeId ?? "(none)",
                        adAttributes.UserPrincipalName ?? "(none)",
                        adAttributes.Mail ?? "(none)");
                }
                else
                {
                    _logger.LogWarning("No AD attributes found for user {SamAccountName}", samAccountName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching claims from Active Directory for {IdentityName}", principal.Identity.Name);
                // Continue without enrichment rather than failing authentication
            }

            return principal;
        }
    }
}
