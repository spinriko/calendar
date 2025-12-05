# User Impersonation Feature

## Overview

The User Impersonation feature allows administrators and testers to view the application from the perspective of different users without needing to log in as them. This is critical for:
- Testing role-based authorization (Admin, Manager, Approver, Employee)
- Debugging user-specific issues
- Demonstrating features to stakeholders
- QA testing different user scenarios

## Current State

Currently, there's an "Impersonate" box on the Absences page that is page-specific and not reusable across the application.

## Goal

Create a global, injectable impersonation component that:
1. Appears on all pages (when in development/mock mode)
2. Allows switching between different user roles and identities
3. Persists the impersonated user across page navigation
4. Updates the UI dynamically to reflect the impersonated user's permissions

## Implementation Steps

### Step 1: Create a Shared Partial View

Create `pto.track/Pages/Shared/_ImpersonationPanel.cshtml`:

```cshtml
@if (ViewData["ShowImpersonation"] as bool? ?? false)
{
    <div class="impersonation-panel position-fixed bottom-0 end-0 m-3 p-3 bg-warning border border-dark shadow-lg" style="z-index: 9999; max-width: 300px;">
        <div class="d-flex justify-content-between align-items-center mb-2">
            <strong>🎭 Impersonation</strong>
            <button type="button" class="btn-close btn-close-white" onclick="toggleImpersonationPanel()" aria-label="Close"></button>
        </div>
        
        <div class="mb-2">
            <label for="impersonateUser" class="form-label small">User:</label>
            <select id="impersonateUser" class="form-select form-select-sm">
                <option value="EMP001">Development User (Admin)</option>
                <option value="EMP002">Test Employee</option>
                <option value="MGR001">Test Manager</option>
                <option value="APR001">Test Approver</option>
                <option value="ADMIN001">Administrator</option>
            </select>
        </div>
        
        <div class="mb-2">
            <label class="form-label small">Roles:</label>
            <div class="form-check form-check-sm">
                <input class="form-check-input" type="checkbox" id="roleEmployee" checked>
                <label class="form-check-label small" for="roleEmployee">Employee</label>
            </div>
            <div class="form-check form-check-sm">
                <input class="form-check-input" type="checkbox" id="roleManager">
                <label class="form-check-label small" for="roleManager">Manager</label>
            </div>
            <div class="form-check form-check-sm">
                <input class="form-check-input" type="checkbox" id="roleApprover">
                <label class="form-check-label small" for="roleApprover">Approver</label>
            </div>
            <div class="form-check form-check-sm">
                <input class="form-check-input" type="checkbox" id="roleAdmin">
                <label class="form-check-label small" for="roleAdmin">Admin</label>
            </div>
        </div>
        
        <button class="btn btn-sm btn-primary w-100" onclick="applyImpersonation()">Apply</button>
        <button class="btn btn-sm btn-secondary w-100 mt-1" onclick="clearImpersonation()">Reset to Default</button>
    </div>
}

<script>
    function toggleImpersonationPanel() {
        const panel = document.querySelector('.impersonation-panel');
        panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
    }
    
    function applyImpersonation() {
        const employeeNumber = document.getElementById('impersonateUser').value;
        const roles = [];
        
        if (document.getElementById('roleEmployee').checked) roles.push('Employee');
        if (document.getElementById('roleManager').checked) roles.push('Manager');
        if (document.getElementById('roleApprover').checked) roles.push('Approver');
        if (document.getElementById('roleAdmin').checked) roles.push('Admin');
        
        // Store in localStorage for persistence
        localStorage.setItem('impersonatedUser', JSON.stringify({
            employeeNumber: employeeNumber,
            roles: roles
        }));
        
        // Reload page to apply impersonation
        window.location.reload();
    }
    
    function clearImpersonation() {
        localStorage.removeItem('impersonatedUser');
        window.location.reload();
    }
    
    // Load saved impersonation state on page load
    document.addEventListener('DOMContentLoaded', () => {
        const saved = localStorage.getItem('impersonatedUser');
        if (saved) {
            const data = JSON.parse(saved);
            document.getElementById('impersonateUser').value = data.employeeNumber;
            document.getElementById('roleEmployee').checked = data.roles.includes('Employee');
            document.getElementById('roleManager').checked = data.roles.includes('Manager');
            document.getElementById('roleApprover').checked = data.roles.includes('Approver');
            document.getElementById('roleAdmin').checked = data.roles.includes('Admin');
        }
    });
</script>
```

### Step 2: Update MockAuthenticationMiddleware

Modify `pto.track/Middleware/MockAuthenticationMiddleware.cs` to check for impersonation data:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    var authMode = _configuration["Authentication:Mode"] ?? "Mock";

    if (authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            // Check for impersonation cookie/header
            var impersonationData = context.Request.Cookies["ImpersonationData"];
            
            List<Claim> claims;
            
            if (!string.IsNullOrEmpty(impersonationData))
            {
                // Use impersonated user claims
                var impersonation = System.Text.Json.JsonSerializer.Deserialize<ImpersonationData>(impersonationData);
                claims = CreateClaimsForImpersonation(impersonation);
                _logger.LogDebug("Impersonating user: {EmployeeNumber} with roles: {Roles}", 
                    impersonation.EmployeeNumber, string.Join(", ", impersonation.Roles));
            }
            else
            {
                // Use default mock user claims
                claims = CreateDefaultMockClaims();
                _logger.LogDebug("Auto-authenticating default mock user");
            }

            var identity = new ClaimsIdentity(claims, "MockAuth");
            var principal = new ClaimsPrincipal(identity);
            await context.SignInAsync("Cookies", principal);
        }
    }

    await _next(context);
}

private List<Claim> CreateDefaultMockClaims()
{
    return new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "EMP001"),
        new Claim(ClaimTypes.Email, "developer@example.com"),
        new Claim(ClaimTypes.Name, "Development User"),
        new Claim("employeeNumber", "EMP001"),
        new Claim("objectGUID", "mock-ad-guid-12345"),
        new Claim(ClaimTypes.Role, "Employee"),
        new Claim(ClaimTypes.Role, "Manager"),
        new Claim(ClaimTypes.Role, "Approver"),
        new Claim(ClaimTypes.Role, "Admin")
    };
}

private List<Claim> CreateClaimsForImpersonation(ImpersonationData impersonation)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, impersonation.EmployeeNumber),
        new Claim(ClaimTypes.Email, $"{impersonation.EmployeeNumber.ToLower()}@example.com"),
        new Claim(ClaimTypes.Name, GetDisplayNameForEmployee(impersonation.EmployeeNumber)),
        new Claim("employeeNumber", impersonation.EmployeeNumber),
        new Claim("objectGUID", $"mock-guid-{impersonation.EmployeeNumber}")
    };

    foreach (var role in impersonation.Roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    return claims;
}

private string GetDisplayNameForEmployee(string employeeNumber)
{
    return employeeNumber switch
    {
        "EMP001" => "Development User",
        "EMP002" => "Test Employee",
        "EMP003" => "Test Manager",
        "EMP004" => "Test Approver",
        "EMP005" => "Administrator",
        _ => $"User {employeeNumber}"
    };
}

private class ImpersonationData
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
```

### Step 3: Create an API Endpoint for Impersonation

Create `pto.track/Controllers/ImpersonationController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing user impersonation (development/mock mode only).
/// </summary>
[ApiController]
[Route("api/impersonation")]
public class ImpersonationController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImpersonationController> _logger;

    public ImpersonationController(
        IConfiguration configuration,
        ILogger<ImpersonationController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sets impersonation data for the current session.
    /// Only works when Authentication:Mode is "Mock".
    /// </summary>
    [HttpPost]
    public IActionResult SetImpersonation([FromBody] ImpersonationRequest request)
    {
        var authMode = _configuration["Authentication:Mode"] ?? "Mock";
        
        if (!authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Impersonation only available in Mock authentication mode" });
        }

        var impersonationData = System.Text.Json.JsonSerializer.Serialize(request);
        
        Response.Cookies.Append("ImpersonationData", impersonationData, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(8)
        });

        _logger.LogInformation("Impersonation set for {EmployeeNumber} with roles {Roles}", 
            request.EmployeeNumber, string.Join(", ", request.Roles));

        return Ok(new { message = "Impersonation applied" });
    }

    /// <summary>
    /// Clears impersonation data, reverting to default mock user.
    /// </summary>
    [HttpDelete]
    public IActionResult ClearImpersonation()
    {
        Response.Cookies.Delete("ImpersonationData");
        
        _logger.LogInformation("Impersonation cleared");
        
        return Ok(new { message = "Impersonation cleared" });
    }
}

public class ImpersonationRequest
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
```

### Step 4: Update the JavaScript to Use the API

Update the script in `_ImpersonationPanel.cshtml`:

```javascript
async function applyImpersonation() {
    const employeeNumber = document.getElementById('impersonateUser').value;
    const roles = [];
    
    if (document.getElementById('roleEmployee').checked) roles.push('Employee');
    if (document.getElementById('roleManager').checked) roles.push('Manager');
    if (document.getElementById('roleApprover').checked) roles.push('Approver');
    if (document.getElementById('roleAdmin').checked) roles.push('Admin');
    
    try {
        const response = await fetch('/api/impersonation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                employeeNumber: employeeNumber,
                roles: roles
            })
        });
        
        if (response.ok) {
            // Store in localStorage for UI state persistence
            localStorage.setItem('impersonatedUser', JSON.stringify({
                employeeNumber: employeeNumber,
                roles: roles
            }));
            
            // Force re-authentication with new impersonation
            await fetch('/api/CurrentUser'); // This will trigger re-auth
            
            // Reload page to apply impersonation
            window.location.reload();
        } else {
            alert('Failed to apply impersonation');
        }
    } catch (error) {
        console.error('Error applying impersonation:', error);
        alert('Error applying impersonation');
    }
}

async function clearImpersonation() {
    try {
        const response = await fetch('/api/impersonation', {
            method: 'DELETE'
        });
        
        if (response.ok) {
            localStorage.removeItem('impersonatedUser');
            window.location.reload();
        } else {
            alert('Failed to clear impersonation');
        }
    } catch (error) {
        console.error('Error clearing impersonation:', error);
        alert('Error clearing impersonation');
    }
}
```

### Step 5: Add Impersonation Panel to Layout

Update `pto.track/Pages/Shared/_Layout.cshtml` to include the impersonation panel:

```cshtml
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- ... existing head content ... -->
</head>
<body>
    <!-- ... existing body content ... -->
    
    @if (ViewData["ShowImpersonation"] as bool? ?? false)
    {
        <partial name="_ImpersonationPanel" />
    }
    
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### Step 6: Enable Impersonation in Development

Update each page's code-behind to enable impersonation in development. For example, in `Absences.cshtml.cs`:

```csharp
public class AbsencesModel : PageModel
{
    private readonly IConfiguration _configuration;
    
    public AbsencesModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void OnGet()
    {
        // Enable impersonation panel in Mock mode only
        var authMode = _configuration["Authentication:Mode"] ?? "Mock";
        ViewData["ShowImpersonation"] = authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    }
}
```

### Step 7: Create a Base Page Model (Optional)

To avoid repeating the impersonation setup on every page, create a base page model:

```csharp
namespace pto.track.Pages;

public abstract class BasePageModel : PageModel
{
    protected readonly IConfiguration Configuration;
    
    protected BasePageModel(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    protected void EnableImpersonationIfMockMode()
    {
        var authMode = Configuration["Authentication:Mode"] ?? "Mock";
        ViewData["ShowImpersonation"] = authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    }
    
    protected override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        base.OnPageHandlerExecuting(context);
        EnableImpersonationIfMockMode();
    }
}
```

Then update page models to inherit from it:

```csharp
public class AbsencesModel : BasePageModel
{
    public AbsencesModel(IConfiguration configuration) : base(configuration)
    {
    }
    
    public void OnGet()
    {
        // Impersonation is automatically enabled via base class
    }
}
```

## Architecture: Two Authentication Paths

The impersonation system uses different authentication mechanisms for **production use** versus **automated testing**:

### Production/Development (Browser Usage)
- **Uses Cookie-Based Authentication**: `ImpersonationData` cookie
- **Flow**:
  1. User selects employee and roles in impersonation panel UI
  2. JavaScript calls `/api/impersonation` endpoint (POST)
  3. Controller sets `ImpersonationData` cookie with user data
  4. `MockAuthenticationMiddleware` reads cookie and creates claims
  5. Page reloads with new identity
- **Storage**: HTTP-only cookie (8-hour expiration) + localStorage (UI state only)
- **Security**: Cookie is HttpOnly, SameSite=Lax, validated in Mock mode only

### Automated Testing (Integration Tests)
- **Uses Header-Based Authentication**: `X-Test-Role` header
- **Flow**:
  1. Test creates HttpClient with specific role header
  2. `TestAuthHandler` reads header and creates claims
  3. Test executes request with role-based authorization
- **Purpose**: Simplifies test setup, avoids cookie management complexity
- **Separation**: `CustomWebApplicationFactory` registers `TestAuthHandler` for tests only

### Why Two Paths?

1. **Simplicity**: Tests remain clean without cookie serialization/deserialization
2. **Speed**: Header-based auth is faster in test execution
3. **Clarity**: Clear separation between test infrastructure and production code
4. **Realism**: Production code uses proper cookie-based flow matching real-world patterns

**Note**: The two paths are completely independent. Production code never uses headers for impersonation, and tests never use the `ImpersonationData` cookie.

## Testing the Impersonation Feature

### Test Scenarios

1. **Admin to Employee Switch**
   - Log in as default user (all roles)
   - Navigate to Groups page (should see admin content)
   - Open impersonation panel
   - Select "Test Employee" with only "Employee" role
   - Apply impersonation
   - Verify Groups page shows "Access Denied"

2. **Manager Testing**
   - Impersonate as "Test Manager" with "Manager" role
   - Navigate to Absences page
   - Verify can see team member absences
   - Verify can approve absences

3. **Role Combination Testing**
   - Set "Manager" + "Approver" roles
   - Verify combined permissions work correctly

4. **Persistence Testing**
   - Apply impersonation
   - Navigate between pages
   - Verify impersonation persists across navigation
   - Close browser and reopen (should persist for 8 hours)

5. **Reset Testing**
   - Click "Reset to Default"
   - Verify returns to default mock user with all roles

## Security Considerations

### Development Only

- **Never enable in production**: Impersonation should only work when `Authentication:Mode` is "Mock"
- Add checks in the controller and middleware to prevent usage outside development
- Consider adding an environment check: `if (!app.Environment.IsDevelopment())`

### Cookie Security

- Use `HttpOnly` to prevent JavaScript access
- Use `Secure` flag in production (HTTPS only)
- Use `SameSite` to prevent CSRF attacks
- Set reasonable expiration (8 hours max)

### Audit Logging

Add logging for impersonation actions:

```csharp
_logger.LogWarning("User {OriginalUser} impersonating as {ImpersonatedUser} with roles {Roles}", 
    originalUser, impersonatedUser, string.Join(", ", roles));
```

## Styling Considerations

### Make it Obvious

The impersonation panel should be visually distinct:
- Bright warning colors (yellow/orange)
- Fixed position (doesn't scroll away)
- High z-index (always on top)
- Clear "Impersonation Active" indicator

### Example CSS

```css
.impersonation-panel {
    background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%);
    border: 3px solid #d84315;
    border-radius: 8px;
    box-shadow: 0 4px 20px rgba(0,0,0,0.3);
}

.impersonation-panel::before {
    content: "⚠️ IMPERSONATION MODE";
    display: block;
    text-align: center;
    font-weight: bold;
    background: #d84315;
    color: white;
    margin: -12px -12px 12px -12px;
    padding: 4px;
    border-radius: 4px 4px 0 0;
}
```

## Future Enhancements

### User Selector Improvements

- Load users dynamically from Resources table
- Show user's actual name and email
- Filter by role
- Search functionality

### Advanced Role Testing

- Combine with feature flags
- Test specific permissions (not just roles)
- Simulate different organizational hierarchies

### Impersonation History

- Track who impersonated whom
- Show recent impersonations
- Quick switch between recent users

### Integration Testing

- Use impersonation API in integration tests
- Create test fixtures with specific user contexts
- Automate role-based test scenarios

## Troubleshooting

### Impersonation Not Working

1. **Check Authentication Mode**: Ensure `appsettings.Development.json` has `"Mode": "Mock"`
2. **Check Cookie**: Verify "ImpersonationData" cookie is set in browser DevTools
3. **Check Middleware Order**: Ensure `UseMockAuthentication()` is after `UseAuthentication()`
4. **Clear State**: Delete cookies and localStorage, then try again

### Roles Not Applying

1. **Check Claims**: Use browser DevTools Network tab to inspect `/api/CurrentUser` response
2. **Check Authorization**: Verify `[Authorize(Roles = "...")]` attributes match role names exactly
3. **Check Case Sensitivity**: Role names are case-sensitive ("Admin" ≠ "admin")

### Panel Not Appearing

1. **Check ViewData**: Ensure `ViewData["ShowImpersonation"]` is set to `true`
2. **Check Layout**: Verify `_Layout.cshtml` includes `<partial name="_ImpersonationPanel" />`
3. **Check CSS**: Panel might be hidden behind other elements (check z-index)

## Migration Path

### Phase 1: Extract from Absences Page
1. Create `_ImpersonationPanel.cshtml` partial
2. Move existing impersonation logic from Absences page
3. Test on Absences page only

### Phase 2: Add API Support
1. Create `ImpersonationController.cs`
2. Update middleware to read impersonation cookie
3. Update JavaScript to use API endpoints

### Phase 3: Global Rollout
1. Add impersonation panel to `_Layout.cshtml`
2. Create `BasePageModel` for easy adoption
3. Update all pages to inherit from `BasePageModel`

### Phase 4: Enhanced Features
1. Load users from database
2. Add impersonation history
3. Add permission-level testing (beyond roles)

## Related Documentation

- [AUTHENTICATION.md](./AUTHENTICATION.md) - Authentication system overview
- [TESTING.md](./TESTING.md) - Testing strategies including role-based tests
- [AUTHORIZATION_SETUP.md](./AUTHORIZATION_SETUP.md) - Role-based authorization configuration
