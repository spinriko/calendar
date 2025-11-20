using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.Authentication;

namespace pto.track.services;

public class UserSyncService : IUserSyncService
{
    private readonly PtoTrackDbContext _context;
    private readonly IUserClaimsProvider _claimsProvider;

    public UserSyncService(PtoTrackDbContext context, IUserClaimsProvider claimsProvider)
    {
        _context = context;
        _claimsProvider = claimsProvider;
    }

    /// <inheritdoc />
    public async Task<SchedulerResource?> EnsureCurrentUserExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!_claimsProvider.IsAuthenticated())
        {
            return null;
        }

        var email = _claimsProvider.GetEmail();
        var adId = _claimsProvider.GetActiveDirectoryId();
        var employeeNumber = _claimsProvider.GetEmployeeNumber();

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(adId) && string.IsNullOrEmpty(employeeNumber))
        {
            return null;
        }

        // Try to find existing user by Email, AD ID, or Employee Number
        SchedulerResource? resource = null;

        if (!string.IsNullOrEmpty(email))
        {
            resource = await _context.Resources.FirstOrDefaultAsync(r => r.Email == email, cancellationToken);
        }

        if (resource == null && !string.IsNullOrEmpty(adId))
        {
            resource = await _context.Resources.FirstOrDefaultAsync(r => r.ActiveDirectoryId == adId, cancellationToken);
        }

        if (resource == null && !string.IsNullOrEmpty(employeeNumber))
        {
            resource = await _context.Resources.FirstOrDefaultAsync(r => r.EmployeeNumber == employeeNumber, cancellationToken);
        }

        var displayName = _claimsProvider.GetDisplayName() ?? email ?? "Unknown User";
        var roles = _claimsProvider.GetRoles().ToList();

        if (resource == null)
        {
            // Create new user
            resource = new SchedulerResource
            {
                Name = displayName,
                Email = email,
                EmployeeNumber = employeeNumber,
                ActiveDirectoryId = adId,
                Role = DetermineRole(roles),
                IsApprover = roles.Contains("Approver", StringComparer.OrdinalIgnoreCase)
                    || roles.Contains("Manager", StringComparer.OrdinalIgnoreCase),
                IsActive = true,
                LastSyncDate = DateTime.UtcNow
            };

            _context.Resources.Add(resource);
        }
        else
        {
            // Update existing user
            resource.Name = displayName;
            resource.Email = email;
            resource.EmployeeNumber = employeeNumber;
            resource.ActiveDirectoryId = adId;
            resource.Role = DetermineRole(roles);
            resource.IsApprover = roles.Contains("Approver", StringComparer.OrdinalIgnoreCase)
                || roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);
            resource.LastSyncDate = DateTime.UtcNow;
            resource.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return resource;
    }

    /// <inheritdoc />
    public async Task<int?> GetCurrentUserResourceIdAsync(CancellationToken cancellationToken = default)
    {
        var resource = await EnsureCurrentUserExistsAsync(cancellationToken);
        return resource?.Id;
    }

    private string DetermineRole(List<string> roles)
    {
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return "Admin";
        if (roles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            return "Manager";
        if (roles.Contains("Approver", StringComparer.OrdinalIgnoreCase))
            return "Approver";

        return "Employee";
    }
}
