# User Impersonation (Current Implementation)

This document describes how impersonation currently works in the codebase.

## Summary

- Impersonation is implemented and available in **Mock** (development) mode.
- A shared UI partial (`pto.track/Pages/Shared/_ImpersonationPanel.cshtml`) renders the impersonation box across pages when enabled.
- The UI persists impersonation choices to a server cookie (`ImpersonationData`) via the `ImpersonationController` API. Server middleware reads that cookie and applies the impersonated identity for the request.
- Integration tests do not rely on the UI; tests use the `X-Test-Claims` header together with a test-only `IClaimsTransformation` (`TestIdentityEnricher`) to simulate per-request claims and roles.

## Where the code lives

- UI partial: `pto.track/Pages/Shared/_ImpersonationPanel.cshtml`
- API controller: `pto.track/Controllers/ImpersonationController.cs` (POST to set, DELETE to clear impersonation)
- Middleware: `pto.track/Middleware/MockAuthenticationMiddleware.cs` (applies impersonation data when `Authentication:Mode` is `Mock`)
- Test helpers: `pto.track.tests/CustomWebApplicationFactory.cs`, `pto.track.tests/TestIdentityEnricher.cs`, `pto.track.tests/TestIIdentityEnricher.cs`, `pto.track.tests/Mocks/TestUserClaimsProvider.cs`

## How impersonation works (runtime)

1. The application configuration (`Authentication:Mode`) controls behavior. In development this is typically `Mock` (see `appsettings.Development.json`).
2. The impersonation UI calls `POST /api/impersonation` with an `ImpersonationRequest` payload; the controller stores a serialized `ImpersonationData` cookie (HttpOnly).
3. `MockAuthenticationMiddleware` runs on each request in Mock mode; it reads `ImpersonationData`, deserializes it, constructs a `ClaimsPrincipal` with the requested roles/claims, and signs in that principal for the request.
4. Application code observes the impersonated identity via `IUserClaimsProvider` or `HttpContext.User` as usual.

## How impersonation works (integration tests)

- Integration tests enforce `ASPNETCORE_ENVIRONMENT=Testing` and the test host forces `Authentication:Mode=Mock`, preventing production-only auth handlers (e.g., Negotiate) from being registered in the TestHost.
- The test project registers a minimal `Test` authentication scheme (`TestAuthHandler`) that only authenticates the request.
- Per-request claims for tests are applied by `TestIdentityEnricher` (an `IClaimsTransformation` implementation) which reads `X-Test-Claims` and mutates the `ClaimsPrincipal` before authorization runs.
- `X-Test-Claims` header format: comma-separated key=value pairs. Example:

```
X-Test-Claims: role=Admin,name=Integration Tester,email=test@example.com,employeeNumber=EMP123
```

Prefer `X-Test-Claims` in new tests; legacy `X-Test-Role` remains supported by some helpers as a fallback.

## Impersonation API

- POST `/api/impersonation` — body: `{ "employeeNumber": "EMP001", "roles": ["Manager","Employee"] }` — stores `ImpersonationData` cookie (Mock mode only).
- DELETE `/api/impersonation` — clears the impersonation cookie.

Both endpoints validate that `Authentication:Mode` is `Mock` and return a 400 if impersonation is attempted while not in Mock mode.

## Examples

Client-side (apply impersonation):

```javascript
await fetch('/api/impersonation', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ employeeNumber: 'EMP002', roles: ['Employee'] })
});
window.location.reload();
```

Integration test (preferred):

```csharp
client.DefaultRequestHeaders.Add("X-Test-Claims", "role=Admin,name=Test,email=test@local");
var resp = await client.GetAsync("/api/groups");
resp.StatusCode.Should().Be(HttpStatusCode.OK);
```

## Security and operational notes

- UI-driven impersonation must only be enabled in non-production environments. All server-side checks gate impersonation on `Authentication:Mode == "Mock"`.
- The impersonation cookie is HttpOnly; set `Secure=true` when serving the app over HTTPS in environments where that applies.
- Tests use header-driven claims injection (`X-Test-Claims`) because it is deterministic and avoids feature gaps in the TestHost.

## Troubleshooting

- If impersonation isn't taking effect:
  - Confirm `Authentication:Mode` is `Mock` in the configuration used to run the app.
  - Confirm the `ImpersonationData` cookie exists in the browser (DevTools → Application → Cookies).
  - Check server logs — `MockAuthenticationMiddleware` logs impersonation events at info/debug level.

## Extensions

- To add extra impersonation attributes, update the `ImpersonationRequest` DTO and the middleware mapping logic.
- To disable UI impersonation entirely, remove the shared partial or gate it behind a feature flag.

If you want, I can also (a) remove remaining legacy `X-Test-Role` references, or (b) add a short `docs/run/TESTING.md` snippet showing common `X-Test-Claims` usage in integration tests.
# User Impersonation Feature

## Overview

The User Impersonation feature allows administrators and testers to view the application from the perspective of different users without needing to log in as them. This is critical for:
- Testing role-based authorization (Admin, Manager, Approver, Employee)
- Debugging user-specific issues
- Demonstrating features to stakeholders
- QA testing different user scenarios

## Current State

Currently, there's an "Impersonate" box on the Absences page that is page-specific and not reusable across the application.

### Test considerations

When running integration tests, impersonation UI is not required — tests use the `X-Test-Claims` header and the test host `TestIdentityEnricher` to simulate per-request claims and roles. This avoids relying on UI-driven impersonation for automated tests and keeps integration tests deterministic.

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
            # User Impersonation — current state

            This file used to contain a proposed implementation; that work is now complete and in the codebase. The content below describes the current, accurate behavior and how to work with impersonation safely.

            ## Summary

            - Impersonation is implemented and available in **Mock** (development) mode.
            - The UI partial is included where enabled and stores impersonation state in a cookie (`ImpersonationData`) so the server middleware can apply the impersonated identity.
            - Integration tests do not rely on the UI; tests use the `X-Test-Claims` header together with a test-only `IClaimsTransformation` (`TestIdentityEnricher`) to simulate per-request claims. This keeps tests deterministic and avoids TestHost issues when production auth handlers are registered.

            ## Where the code lives

            - UI partial: `pto.track/Pages/Shared/_ImpersonationPanel.cshtml` (renders the impersonation controls in Mock mode)
            - API controller: `pto.track/Controllers/ImpersonationController.cs` (POST to set, DELETE to clear impersonation)
            - Middleware: `pto.track/Middleware/MockAuthenticationMiddleware.cs` (applies impersonation data when `Authentication:Mode` is `Mock`)
            - Test helpers: `pto.track.tests/CustomWebApplicationFactory.cs`, `pto.track.tests/TestIdentityEnricher.cs`, `pto.track.tests/TestIIdentityEnricher.cs`, and `pto.track.tests/Mocks/TestUserClaimsProvider.cs`

            ## How impersonation works (runtime)

            1. The app configuration (`Authentication:Mode`) determines behavior. In development `appsettings.Development.json` it is typically `Mock`.
            2. The impersonation UI calls `POST /api/impersonation` with an `ImpersonationRequest` payload. The controller stores serialized impersonation data in the `ImpersonationData` cookie (HttpOnly).
            3. `MockAuthenticationMiddleware` (executing during request processing in Mock mode) reads `ImpersonationData`, deserializes it, and creates a `ClaimsPrincipal` with the requested roles/attributes, then signs in the principal for the request.
            4. Controllers and services observe the impersonated identity via `IUserClaimsProvider` / `HttpContext.User` as usual.

            ## How impersonation works (integration tests)

            - Tests force `ASPNETCORE_ENVIRONMENT=Testing` and the test host forces `Authentication:Mode=Mock` so production-only auth handlers (e.g., Negotiate) are not registered inside the TestHost.
            - Tests register a minimal `Test` authentication scheme (`TestAuthHandler`) that simply authenticates the request.
            - Per-request claims are injected by `TestIdentityEnricher` (registered as `IClaimsTransformation`) which reads the `X-Test-Claims` header and appends claims to the `ClaimsPrincipal` before authorization runs.
            - Example header format:

            ```
            X-Test-Claims: role=Admin,name=Integration Tester,email=test@example.com,employeeNumber=EMP123
            ```

            Prefer `X-Test-Claims` in new tests. Some legacy helpers still accept `X-Test-Role` as a fallback.

            ## API: Impersonation endpoints

            - POST `/api/impersonation` — body: `{ "employeeNumber": "EMP001", "roles": ["Manager","Employee"] }` — Stores `ImpersonationData` cookie (Mock mode only).
            - DELETE `/api/impersonation` — Clears the impersonation cookie.

            These endpoints validate `Authentication:Mode` and return 400 if impersonation is attempted in non-Mock modes.

            ## Examples

            Client-side (apply impersonation):

            ```javascript
            await fetch('/api/impersonation', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ employeeNumber: 'EMP002', roles: ['Employee'] })
            });
            window.location.reload();
            ```

            Integration test (preferred):

            ```csharp
            client.DefaultRequestHeaders.Add("X-Test-Claims", "role=Admin,name=Test,email=test@local");
            var resp = await client.GetAsync("/api/groups");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            ```

            ## Security and operational notes

            - Impersonation must only be enabled in non-production environments. The middleware and controller explicitly check `Authentication:Mode == "Mock"` before accepting impersonation data.
            - The impersonation cookie is HttpOnly but may be configured `Secure`/`SameSite` depending on your environment. Do not enable UI-driven impersonation in production.
            - Tests use header-driven claims injection (`X-Test-Claims`) instead of cookie-based UI impersonation to keep automated runs deterministic and avoid server feature gaps in the TestHost.

            ## Troubleshooting

            - If impersonation does not apply:
              - Confirm `Authentication:Mode` is `Mock` in the app config used to run the site.
              - Confirm the `ImpersonationData` cookie is present (browser dev tools → Application → Cookies).
              - Check server logs — `MockAuthenticationMiddleware` logs impersonation events at debug/info level.

            ## Extending or changing behavior

            - To add new attributes to impersonation, update the `ImpersonationRequest` DTO and the middleware code that maps DTO fields to claims.
            - To disable UI-driven impersonation entirely, remove the partial or guard it behind a feature flag.

            ---

            If you'd like, I can also: (a) remove remaining legacy references to `X-Test-Role`, or (b) add a short developer note in `docs/run/TESTING.md` showing common `X-Test-Claims` test patterns.
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
- [TESTING.md](../run/TESTING.md) - Testing strategies including role-based tests
- [AUTHORIZATION_SETUP.md](./AUTHORIZATION_SETUP.md) - Role-based authorization configuration
