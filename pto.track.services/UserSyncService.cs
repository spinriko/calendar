using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.Authentication;

namespace pto.track.services;

public class UserSyncService : IUserSyncService
{
    private readonly PtoTrackDbContext _context;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UserSyncService(PtoTrackDbContext context, IUserClaimsProvider claimsProvider, IUnitOfWork unitOfWork)
    {
        _context = context;
        _claimsProvider = claimsProvider;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Resource?> EnsureCurrentUserExistsAsync(CancellationToken cancellationToken = default)
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

        var resource = await FindExistingUserAsync(email, adId, employeeNumber, cancellationToken);
        var displayName = _claimsProvider.GetDisplayName() ?? email ?? "Unknown User";
        var roles = _claimsProvider.GetRoles().ToList();

        if (resource == null)
        {
            resource = CreateNewUser(displayName, email, employeeNumber, adId, roles);
            _context.Resources.Add(resource);
        }
        else
        {
            UpdateExistingUser(resource, displayName, email, employeeNumber, adId, roles);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return resource;
    }

    private async Task<Resource?> FindExistingUserAsync(string? email, string? adId, string? employeeNumber, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(email))
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Email == email, cancellationToken);
            if (resource != null) return resource;
        }

        if (!string.IsNullOrEmpty(adId))
        {
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.ActiveDirectoryId == adId, cancellationToken);
            if (resource != null) return resource;
        }

        if (!string.IsNullOrEmpty(employeeNumber))
        {
            return await _context.Resources.FirstOrDefaultAsync(r => r.EmployeeNumber == employeeNumber, cancellationToken);
        }

        return null;
    }

    private Resource CreateNewUser(string displayName, string? email, string? employeeNumber, string? adId, List<string> roles)
    {
        return new Resource
        {
            Name = displayName,
            Email = email,
            EmployeeNumber = employeeNumber,
            ActiveDirectoryId = adId,
            Role = DetermineRole(roles),
            IsApprover = IsApproverOrManager(roles),
            IsActive = true,
            LastSyncDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private void UpdateExistingUser(Resource resource, string displayName, string? email, string? employeeNumber, string? adId, List<string> roles)
    {
        resource.Name = displayName;
        resource.Email = email;
        resource.EmployeeNumber = employeeNumber;
        resource.ActiveDirectoryId = adId;
        resource.Role = DetermineRole(roles);
        resource.IsApprover = IsApproverOrManager(roles);
        resource.LastSyncDate = DateTime.UtcNow;
        resource.ModifiedDate = DateTime.UtcNow;
    }

    private bool IsApproverOrManager(List<string> roles)
    {
        return roles.Contains("Approver", StringComparer.OrdinalIgnoreCase)
            || roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);
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
