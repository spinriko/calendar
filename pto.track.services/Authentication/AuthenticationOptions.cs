namespace pto.track.services.Authentication;

/// <summary>
/// Configuration options for authentication providers.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// The authentication mode: "Mock", "ActiveDirectory", or "AzureAD"
    /// </summary>
    public string Mode { get; set; } = "Mock";

    /// <summary>
    /// Domain for Active Directory authentication (e.g., "CONTOSO")
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Azure AD Tenant ID (for Azure AD authentication)
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD Client ID (for Azure AD authentication)
    /// </summary>
    public string? ClientId { get; set; }
}
