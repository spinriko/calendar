using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.Authentication;
using pto.track.services.Identity;

namespace pto.track.services;

public class UserSyncService : IUserSyncService
{
    private readonly PtoTrackDbContext _context;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(
        PtoTrackDbContext context, 
        IUserClaimsProvider claimsProvider, 
        IUnitOfWork unitOfWork,
        ILogger<UserSyncService> logger)
    {
        _context = context;
        _claimsProvider = claimsProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
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
            _logger.LogInformation("Created new user {DisplayName} with employeeID {EmployeeNumber}", displayName, employeeNumber);
        }
        else
        {
            UpdateExistingUser(resource, displayName, email, employeeNumber, adId, roles);
            _logger.LogInformation("Updated existing user {DisplayName} (ID: {ResourceId})", displayName, resource.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return resource;
    }

    private async Task<Resource?> FindExistingUserAsync(string? email, string? adId, string? employeeNumber, CancellationToken cancellationToken)
    {
        // Priority 1: Match by employeeID (from AD) to AssociateId (from ADP)
        // This is the primary join key between AD and ADP systems
        if (!string.IsNullOrEmpty(employeeNumber))
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.AssociateId == employeeNumber, cancellationToken);
            
            if (resource != null)
            {
                _logger.LogDebug("Matched user by employeeID {EmployeeNumber} to AssociateId", employeeNumber);
                return resource;
            }

            // Fallback: Check legacy EmployeeNumber field for backward compatibility
            resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.EmployeeNumber == employeeNumber, cancellationToken);
            
            if (resource != null)
            {
                _logger.LogDebug("Matched user by legacy EmployeeNumber {EmployeeNumber}", employeeNumber);
                return resource;
            }
        }

        // Priority 2: Match by email as fallback
        if (!string.IsNullOrEmpty(email))
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.Email == email, cancellationToken);
            
            if (resource != null)
            {
                _logger.LogDebug("Matched user by email {Email}", email);
                
                // If we matched by email but have employeeID, update AssociateId for future matches
                if (!string.IsNullOrEmpty(employeeNumber) && string.IsNullOrEmpty(resource.AssociateId))
                {
                    _logger.LogInformation("Backfilling AssociateId {EmployeeNumber} for user {Email}", employeeNumber, email);
                    resource.AssociateId = employeeNumber;
                }
                
                return resource;
            }
        }

        // Priority 3: Match by AD ID as last resort
        if (!string.IsNullOrEmpty(adId))
        {
            var resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.ActiveDirectoryId == adId, cancellationToken);
            
            if (resource != null)
            {
                _logger.LogDebug("Matched user by AD ID {AdId}", adId);
                return resource;
            }
        }

        _logger.LogWarning("No existing user found for email={Email}, adId={AdId}, employeeNumber={EmployeeNumber}", 
            email, adId, employeeNumber);
        return null;
    }

    private Resource CreateNewUser(string displayName, string? email, string? employeeNumber, string? adId, List<string> roles)
    {
        return new Resource
        {
            Name = displayName,
            Email = email,
            EmployeeNumber = employeeNumber,
            AssociateId = employeeNumber, // Map employeeID from AD to AssociateId for ADP matching
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
        
        // Update AssociateId if we have employeeID and it's not already set
        if (!string.IsNullOrEmpty(employeeNumber) && string.IsNullOrEmpty(resource.AssociateId))
        {
            resource.AssociateId = employeeNumber;
            _logger.LogInformation("Set AssociateId {EmployeeNumber} for existing user {ResourceId}", employeeNumber, resource.Id);
        }
        
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

    /// <inheritdoc />
    public async Task<UserSyncResult> SyncUserOnDemandAsync(string? immutableId, string? email, string? employeeNumber, CancellationToken cancellationToken = default)
    {
        // Stub implementation: on-demand user sync from external identity sources.
        // Will call identity service and ADP data mart APIs to fetch current user data.
        // For now, return placeholder result with logged sync timestamp.
        
        var result = new UserSyncResult
        {
            Success = true,
            UserId = immutableId ?? email ?? employeeNumber,
            Message = "On-demand sync stubbed; implementation pending API contracts",
            SyncedAt = DateTime.UtcNow
        };

        // TODO: Implement on-demand fetch from identity service and ADP APIs
        // TODO: Normalize and compare with staging tables
        // TODO: Apply group and role mappings
        // TODO: Upsert to Resources, UserGroups, UserRoles tables
        // TODO: Handle sync errors and validation failures

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<UserSyncBatchResult> SyncAllUsersAsync(CancellationToken cancellationToken = default)
    {
        // Stub implementation: batch sync of all users from external identity sources.
        // Used by background worker for periodic full synchronization.
        // For now, return placeholder result.

        var result = new UserSyncBatchResult
        {
            TotalProcessed = 0,
            Created = 0,
            Updated = 0,
            Failed = 0,
            CompletedAt = DateTime.UtcNow
        };

        // TODO: Implement batch fetch from identity service and ADP APIs
        // TODO: Normalize and materialize into staging tables
        // TODO: Compare existing vs. fetched (added, updated, removed)
        // TODO: Apply group and role hierarchies
        // TODO: Upsert to Users, Groups, UserGroups, UserRoles, Roles tables
        // TODO: Maintain audit trail (sync timestamp, counts, errors)
        // TODO: Handle idempotency and retry logic

        return await Task.FromResult(result);
    }
}
