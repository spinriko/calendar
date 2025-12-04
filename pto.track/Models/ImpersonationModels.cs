namespace pto.track.Models;

/// <summary>
/// Request model for setting impersonation data.
/// </summary>
public class UserImpersonationRequest
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Internal model for impersonation data stored in cookies.
/// </summary>
public class ImpersonationData
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
