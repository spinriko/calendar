namespace pto.track.services.Authentication;

/// <summary>
/// Provides user claims and identity information.
/// Abstracts the authentication provider (mock, AD, Azure AD, etc.)
/// </summary>
public interface IUserClaimsProvider
{
    /// <summary>
    /// Gets the current authenticated user's employee number.
    /// </summary>
    string? GetEmployeeNumber();

    /// <summary>
    /// Gets the current authenticated user's email address.
    /// </summary>
    string? GetEmail();

    /// <summary>
    /// Gets the current authenticated user's display name.
    /// </summary>
    string? GetDisplayName();

    /// <summary>
    /// Gets the current authenticated user's Active Directory ID (objectId/GUID).
    /// </summary>
    string? GetActiveDirectoryId();

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Gets all roles for the current user.
    /// </summary>
    IEnumerable<string> GetRoles();

    /// <summary>
    /// Checks if the current user has a specific role.
    /// </summary>
    bool IsInRole(string role);
}
