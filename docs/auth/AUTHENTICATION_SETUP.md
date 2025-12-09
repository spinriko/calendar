# Claims-Based Security Implementation Summary

## What Was Implemented

A complete claims-based authentication system that **works with mock authentication now** and **easily migrates to Active Directory** when deployed to your corporate environment.

## Files Created/Modified

### New Authentication Infrastructure

1. **`pto.track.services/Authentication/IUserClaimsProvider.cs`**
   - Interface abstracting user identity and claims
   - Methods: GetEmployeeNumber(), GetEmail(), GetDisplayName(), GetActiveDirectoryId(), IsAuthenticated(), GetRoles(), IsInRole()

2. **`pto.track.services/Authentication/MockUserClaimsProvider.cs`**
   - Development implementation with hardcoded test user
   - Default user: ADMIN001, admin@example.com, "Admin User", roles: Employee/Manager/Approver/Admin
   - **Supports impersonation**: Switch between Admin, Manager, Approver, and Employee roles via UI
   - No AD required - works on any machine

## Impersonation Feature (Mock Mode Only)

When running in Mock authentication mode (development), the application includes an **impersonation feature** that allows you to switch between different user roles without restarting the application. This is perfect for demos and testing.

### How to Use Impersonation

1. **Start the application** in Development mode (Mock authentication is enabled by default)

2. **Look for the impersonation panel** in the top-right corner of the Absences page (yellow banner with 🎭 icon)

3. **Select a role** from the dropdown:
   - **Admin (All Roles)**: Has all permissions - can manage everything
   - **Manager**: Can approve/reject requests, view all pending requests
   - **Approver**: Can approve/reject requests, view all pending requests
   - **Employee**: Can only create and view their own requests

4. **The page automatically refreshes** with the new user's context

### What Changes When You Impersonate

- **User identity**: Employee number, email, and display name change
- **Permissions**: Authorization checks reflect the selected role
- **Data visibility**: 
  - Employees see only their own pending requests
  - Managers/Approvers see all pending requests
- **Calendar interactions**: Create, approve, reject buttons appear based on role

### API Endpoints for Impersonation

```http
POST /api/currentuser/impersonate
Content-Type: application/json
{
  "role": "Manager"
}

POST /api/currentuser/clearimpersonation
```

The impersonation state is stored in a cookie that persists for 7 days, so you don't need to re-select your role each time you refresh the page.

3. **`pto.track.services/Authentication/ActiveDirectoryClaimsProvider.cs`**
   - Production implementation reading from Windows Authentication claims
   - Maps AD attributes to application fields
   - Automatically activated when Mode="ActiveDirectory"

4. **`pto.track.services/Authentication/AuthenticationOptions.cs`**
   - Configuration class for authentication settings
   - Supports Mode: Mock, ActiveDirectory, AzureAD

5. **`pto.track.services/UserSyncService.cs`**
   - Automatically creates/updates user records in Resources table
   - Matches users by Email → AD ID → Employee Number
   - Updates LastSyncDate on each login
   - Maps AD roles to application roles (Admin, Manager, Approver, Employee)

### Modified Files

1. **`pto.track.services/ServiceCollectionExtensions.cs`**
   - Registers IUserClaimsProvider based on configuration
   - Registers UserSyncService for automatic user synchronization

2. **`pto.track/Program.cs`**
   - Added HttpContextAccessor for claims access
   - Added UseAuthentication() middleware

3. **`pto.track/appsettings.Development.json`**
   - Added `"Authentication": { "Mode": "Mock" }`
   - Development uses mock authentication

4. **`pto.track/appsettings.json`**
   - Added `"Authentication": { "Mode": "ActiveDirectory", "Domain": "CORP" }`
   - Production ready for AD integration

5. **`pto.track/Controllers/CurrentUserController.cs`** (NEW)
   - API endpoint: `GET /api/currentuser` - Returns current user info
   - API endpoint: `GET /api/currentuser/role/{roleName}` - Check role membership

6. **`pto.track/Pages/Absences.cshtml`**
   - Loads current user on page init via `/api/currentuser`
   - Uses actual user ID for approvals/rejections (was hardcoded 1)
   - Detects if user is manager/approver for pending view logic

7. **`pto.track.services/pto.track.services.csproj`**
   - Added Microsoft.AspNetCore.Http.Abstractions package

## How It Works Now (Development)

1. **Application starts** → ServiceCollectionExtensions reads `Authentication:Mode` from config
2. **Mode is "Mock"** → Registers MockUserClaimsProvider
3. **User visits /Absences** → JavaScript calls `/api/currentuser`
4. **CurrentUserController** → Calls UserSyncService.EnsureCurrentUserExistsAsync()
5. **UserSyncService** → Gets mock claims, creates/updates user in Resources table
6. **Frontend receives** → User info (id, name, email, roles, isApprover)
7. **Frontend uses** → Real user ID for operations, role-based UI logic

### Current Mock User
- **ID**: Auto-assigned from database (synced on first request)
- **Employee Number**: EMP001
- **Email**: developer@example.com
- **Name**: Development User
- **Roles**: Employee, Manager, Approver
- **IsApprover**: true (because has Manager role)

## Migration to Corporate AD (Zero Code Changes)

### Step 1: Deploy to Corporate Environment
Copy all files to corporate server with AD access.

### Step 2: Update Configuration
The production `appsettings.json` already has:
```json
{
  "Authentication": {
    "Mode": "ActiveDirectory",
    "Domain": "CORP"
  }
}
```

### Step 3: Enable Windows Authentication

**Option A: IIS**
1. Open IIS Manager
2. Select your application
3. Authentication → Enable "Windows Authentication"
4. (Optional) Disable "Anonymous Authentication"

**Option B: Kestrel** (if not using IIS)
Add to Program.cs:
```csharp
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
```

And install package:
```bash
dotnet add pto.track package Microsoft.AspNetCore.Authentication.Negotiate
```

### Step 4: Deploy and Test
1. Application automatically switches to ActiveDirectoryClaimsProvider
2. Users authenticate via Windows/AD credentials
3. First login auto-creates their record in Resources table
4. Role/approver status determined by AD group membership

## Active Directory Mapping

The ActiveDirectoryClaimsProvider reads these AD claims:

| Application Field | AD Claim (in order) |
|------------------|-------------------|
| Employee Number | employeeNumber, employeeID, NameIdentifier |
| Email | Email, email, mail |
| Display Name | Name, displayName, name |
| AD Object ID | objectGUID, objectidentifier, oid |
| Roles | Role, role, roles |

### Role Mapping Logic
```
AD Group "Admin" → Role: "Admin", IsApprover: true
AD Group "Manager" → Role: "Manager", IsApprover: true  
AD Group "Approver" → Role: "Approver", IsApprover: true
(none) → Role: "Employee", IsApprover: false
```

## Testing the Setup

### Test Mock Authentication (Now)
```bash
dotnet run --project pto.track
# Navigate to https://localhost:5001/Absences
# Open browser console - you should see:
# "Current user: {id: 1, name: 'Development User', ...}"
# "Is manager/approver: true"
```

### Test Different Mock Users
Edit `MockUserClaimsProvider.cs`:
```csharp
public string? GetEmployeeNumber() => "EMP999";
public IEnumerable<string> GetRoles() => new[] { "Employee" }; // Regular employee
```

### Test AD Authentication (Corporate)
1. Deploy with `Mode: "ActiveDirectory"`
2. Enable Windows Auth in IIS
3. Access site from domain-joined computer
4. Should auto-login with your AD credentials
5. Check `/api/currentuser` - should show your real AD info

## API Endpoints

### Get Current User
```http
GET /api/currentuser
```

Response (Mock):
```json
{
  "id": 1,
  "name": "Development User",
  "email": "developer@example.com",
  "employeeNumber": "EMP001",
  "role": "Manager",
  "isApprover": true,
  "isActive": true,
  "department": null,
  "roles": ["Employee", "Manager", "Approver"]
}
```

Response (AD):
```json
{
  "id": 42,
  "name": "John Smith",
  "email": "john.smith@corp.com",
  "employeeNumber": "E12345",
  "role": "Employee",
  "isApprover": false,
  "isActive": true,
  "department": "Engineering",
  "roles": ["Employee"]
}
```

### Check Role
```http
GET /api/currentuser/role/Manager
```

Response:
```json
{
  "role": "Manager",
  "hasRole": true
}
```

## Using Claims in Your Code

### In Controllers
```csharp
public class MyController : ControllerBase
{
    private readonly IUserClaimsProvider _claims;
    private readonly IUserSyncService _userSync;
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Get database ID
        var userId = await _userSync.GetCurrentUserResourceIdAsync();
        
        // Check role
        if (_claims.IsInRole("Manager"))
        {
            // Manager logic
        }
        
        // Get info
        var email = _claims.GetEmail();
        var name = _claims.GetDisplayName();
    }
}
```

### In JavaScript
```javascript
// Load current user
const response = await DayPilot.Http.get("/api/currentuser");
const user = response.data;

// Use user info
console.log(`Hello ${user.name}`);
if (user.isApprover) {
    // Show approve/reject buttons
}

// Use in API calls
const data = {
    employeeId: user.id,
    approverId: user.id
};
```

## Benefits of This Approach

✅ **Works Now**: Develop without AD access using mock authentication  
✅ **Zero Code Changes**: Switch to AD by changing config only  
✅ **Type Safe**: Interface-based design, compile-time checking  
✅ **Auto Sync**: Users automatically created/updated in database  
✅ **Role-Based**: Supports multiple roles and permissions  
✅ **Testable**: Easy to create test implementations  
✅ **Flexible**: Can add Azure AD, OAuth, etc. later  
✅ **Clean**: Authentication logic separated from business logic  

## Next Steps

1. **Customize Mock User** (optional)
   - Edit `MockUserClaimsProvider.cs` to test different scenarios
   - Create multiple mock providers for different test users

2. **Test Role-Based Features**
   - Manager view vs Employee view in Absences page
   - Approve/Reject buttons (only visible to managers/approvers)

3. **Deploy to Corporate**
   - When ready, deploy with AD configuration
   - Enable Windows Auth in IIS
   - Test with real AD users

4. **Add Authorization Policies** (optional)
   - Use [Authorize] attributes on controllers
   - Define policies in Program.cs
   - Example: `[Authorize(Roles = "Manager")]`

## Troubleshooting

**Mock user not loading**
- Check browser console for errors
- Verify `/api/currentuser` endpoint returns 200
- Check `appsettings.Development.json` has `Mode: "Mock"`

**AD authentication not working**
- Verify Windows Auth enabled in IIS
- Check user is on domain-joined computer
- Confirm `appsettings.json` has `Mode: "ActiveDirectory"`
- Check AD user has app access permissions

**User not syncing to database**
- Check database for new record in Resources table
- Verify LastSyncDate is being updated
- Check logs for errors in UserSyncService

## Documentation

See [AUTHENTICATION.md](./AUTHENTICATION.md) for detailed documentation including:
- Complete architecture overview
- Step-by-step migration guide
- Advanced scenarios (Azure AD, multi-provider)
- Security best practices
- Troubleshooting guide
