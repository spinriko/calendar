# Authentication and Claims-Based Security

This application uses a flexible claims-based authentication system that supports both mock authentication (for development) and Active Directory integration (for production).

## Architecture Overview

The authentication system is built around the **Provider Pattern**, allowing you to switch between different authentication mechanisms without changing application code.

### Key Components

1. **IUserClaimsProvider** - Interface that abstracts user identity and claims
2. **MockUserClaimsProvider** - Development implementation with hardcoded user
3. **ActiveDirectoryClaimsProvider** - Production implementation reading from Windows/AD claims
4. **UserSyncService** - Automatically syncs authenticated users to the Resources table

## Development Setup (Current)

In your development environment, the application uses `MockUserClaimsProvider` which simulates an authenticated user without requiring Active Directory.

### Configuration: `appsettings.Development.json`

```json
{
  "Authentication": {
    "Mode": "Mock"
  }
}
```

### Mock User Details

The mock provider returns the following simulated user:
- **Employee Number**: EMP001
- **Email**: developer@example.com
- **Display Name**: Development User
- **AD ID**: mock-ad-guid-12345
- **Roles**: Employee, Manager, Approver

You can customize these values in `MockUserClaimsProvider.cs` to test different user scenarios.

## Production Setup (Corporate AD)

When you deploy to your corporate environment with Active Directory, simply change the authentication mode.

### Configuration: `appsettings.json`

```json
{
  "Authentication": {
    "Mode": "ActiveDirectory",
    "Domain": "CORP"
  }
}
```

### Windows Authentication Setup

In your corporate environment, you'll need to enable Windows Authentication in IIS or Kestrel:

#### IIS Configuration

1. In IIS Manager, select your application
2. Open **Authentication**
3. **Enable** Windows Authentication
4. **Disable** Anonymous Authentication (or keep enabled for mixed scenarios)

#### Kestrel Configuration (Program.cs)

For Kestrel with Windows Auth, add to `Program.cs`:

```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
```

And add the package:
```bash
dotnet add package Microsoft.AspNetCore.Authentication.Negotiate
```

### Active Directory Claims Mapping

The `ActiveDirectoryClaimsProvider` reads the following claims from AD:

| Application Field | AD Claim Types (in order of precedence) |
|------------------|----------------------------------------|
| Employee Number  | `employeeNumber`, `employeeID`, `NameIdentifier` |
| Email            | `Email`, `email`, `mail` |
| Display Name     | `Name`, `displayName`, `name` |
| AD Object ID     | `objectGUID`, `objectidentifier`, `oid` |
| Roles            | `Role`, `role`, `roles` |

## User Synchronization

The `UserSyncService` automatically creates/updates user records in the `Resources` table when users authenticate:

1. User authenticates via AD (or mock)
2. Claims are extracted via `IUserClaimsProvider`
3. `UserSyncService.EnsureCurrentUserExistsAsync()` is called
4. User is matched by Email → AD ID → Employee Number
5. If not found, a new `SchedulerResource` record is created
6. If found, the record is updated with latest claims data
7. `LastSyncDate` is updated to track when the user was last seen

### Role Mapping

User roles are automatically determined from AD group membership:

| AD Role/Group | Application Role | Is Approver |
|--------------|------------------|-------------|
| Admin        | Admin            | Yes         |
| Manager      | Manager          | Yes         |
| Approver     | Approver         | Yes         |
| (default)    | Employee         | No          |

## Using Claims in Your Code

### In Controllers

Inject `IUserClaimsProvider` to access current user information:

```csharp
public class AbsencesController : ControllerBase
{
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUserSyncService _userSync;
    
    public AbsencesController(
        IUserClaimsProvider claimsProvider,
        IUserSyncService userSync)
    {
        _claimsProvider = claimsProvider;
        _userSync = userSync;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMyAbsences()
    {
        // Get current user's database ID
        var userId = await _userSync.GetCurrentUserResourceIdAsync();
        if (userId == null)
            return Unauthorized();
            
        // Check roles
        if (_claimsProvider.IsInRole("Manager"))
        {
            // Manager-specific logic
        }
        
        // Get user info
        var email = _claimsProvider.GetEmail();
        var name = _claimsProvider.GetDisplayName();
        
        // ... your logic
    }
}
```

### In Razor Pages

```csharp
public class AbsencesModel : PageModel
{
    private readonly IUserClaimsProvider _claimsProvider;
    
    public string UserName { get; set; }
    public bool IsManager { get; set; }
    
    public async Task OnGetAsync()
    {
        UserName = _claimsProvider.GetDisplayName() ?? "Guest";
        IsManager = _claimsProvider.IsInRole("Manager");
    }
}
```

## Migration Path

### Phase 1: Development (Current)
- ✅ Use `Mock` authentication mode
- ✅ Test with simulated users
- ✅ Develop features without AD dependency

### Phase 2: Corporate Deployment
1. Deploy application to corporate environment
2. Update `appsettings.json` to `"Mode": "ActiveDirectory"`
3. Enable Windows Authentication in IIS/Kestrel
4. Ensure AD users have appropriate group memberships
5. First user login will auto-create their record in Resources table

### Phase 3: Advanced Scenarios (Optional)

#### Azure AD Integration
For Azure AD (cloud), create an `AzureADClaimsProvider`:

```csharp
public class AzureADClaimsProvider : IUserClaimsProvider
{
    // Implementation using Microsoft.Identity.Web
    // Read claims from Azure AD JWT tokens
}
```

Configuration:
```json
{
  "Authentication": {
    "Mode": "AzureAD",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

#### Multiple Authentication Providers
Support both Windows Auth and Anonymous (with manual login):

```csharp
builder.Services.AddAuthentication()
    .AddNegotiate()
    .AddCookie();
```

## Testing

### Unit Tests with Mock Authentication

```csharp
[Fact]
public async Task TestWithMockUser()
{
    var mockClaims = new MockUserClaimsProvider(httpContextAccessor);
    var service = new AbsenceService(context, mockClaims);
    
    // Test with mock user
}
```

### Integration Tests

The test projects automatically use mock authentication by default. To test with different users, create custom mock implementations:

```csharp
public class TestUserClaimsProvider : IUserClaimsProvider
{
    public string TestEmployeeNumber { get; set; } = "EMP999";
    public List<string> TestRoles { get; set; } = new() { "Employee" };
    
    // Implement interface with test values
}
```

## Security Best Practices

1. **Always use HTTPS** in production
2. **Never hardcode credentials** - use configuration
3. **Validate claims** before trusting them
4. **Log authentication events** for auditing
5. **Implement authorization policies** for sensitive operations
6. **Keep authentication packages updated**

## Troubleshooting

### "User is not authenticated" in development
- Check `appsettings.Development.json` has `"Mode": "Mock"`
- Verify `HttpContextAccessor` is registered in DI
- Ensure `MockUserClaimsProvider` is being used

### AD authentication not working
- Verify Windows Authentication is enabled in IIS
- Check AD user has access to the application
- Confirm claim types match your AD schema
- Use Fiddler/Browser Dev Tools to inspect authentication headers

### User not syncing to database
- Check `UserSyncService` is registered in DI
- Verify at least one identifier (Email, AD ID, or Employee Number) is present in claims
- Check database logs for constraint violations
- Ensure `LastSyncDate` column exists (migration 20251119205357)

## Additional Resources

- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Windows Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth)
- [Claims-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims)
