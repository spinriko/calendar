% Azure AD / OpenID Connect migration for pto.track

This document describes steps to migrate the application from Windows Integrated Authentication (NTLM/Negotiate) to an OAuth2/OpenID Connect solution using Azure AD (or an on-prem alternative such as ADFS). It covers planning, Azure AD app registration, application code changes (ASP.NET Core), reverse-proxy/ARR notes, testing, and troubleshooting. Use this as a checklist and implementation guide.

## Overview and goals
- Replace connection-oriented Windows auth flows (NTLM/SPNEGO) with token-based authentication (OIDC for UI, OAuth2 Bearer for APIs).
- Allow ARR/reverse-proxy + Kestrel hosting without Kerberos delegation complexity.
- Support both interactive users (OIDC Authorization Code) and APIs (JWT access tokens).
- Support on-prem Active Directory integration via Azure AD Connect or use ADFS as an OIDC provider if necessary.

## High-level options
- Azure AD (recommended for cloud + hybrid): use Azure AD or Azure AD B2C as IdP.
- ADFS (on-prem): supports Kerberos/NTLM and can emit tokens via OIDC/SAML; useful if you cannot use Azure AD.
- Hybrid: use Azure AD Connect to sync on-prem AD to Azure AD, then use Azure AD as IdP.

## Planning and prerequisites
- Decide whether the app will accept:
  - OIDC interactive sign-in (UI) + cookie session for users
  - JWT Bearer tokens (API, machine-to-machine)
- Identify hostnames used by clients and API audience values.
- Collect Azure tenant and subscription info (or ADFS endpoints) and admin credentials for app registration.
- Ensure HTTPS is enforced in all environments.

## Azure AD: App registrations and permissions
You'll create two app registrations (recommended):

- `pto.track-web` (the browser-facing web app)
  - Platform: Web
  - Redirect URI: https://app.example.local/signin-oidc (adjust for your environment)
  - Implicit/OAuth flows: use Authorization Code with PKCE for SPAs; for server-side web apps use code flow.
  - Configure application roles if you want role claims injected.

- `pto.track-api` (the backend API)
  - Expose an API scope (e.g., `api://<client-id>/access_as_user` or `api://pto.track-api/.default`)
  - Add delegated scopes and application permissions as necessary

Steps (Azure Portal):
1. Register `pto.track-api` -> `Expose an API` -> Add a scope (e.g., `access_as_user`). Note the Application (client) ID and the scope URI.
2. Register `pto.track-web` -> Authentication -> add platform `Web` and set Redirect URI to `/signin-oidc` endpoint.
3. In `pto.track-web`, under API permissions, add delegated permission to call `pto.track-api` scope.
4. (Optional) Define App Roles on `pto.track-api` manifest to allow role assignment to users/groups.

For on-prem AD users to sign-in with Azure AD, use Azure AD Connect to sync identities (hybrid) or configure federation to ADFS.

## App code changes (ASP.NET Core)
This section shows example configuration for supporting both UI (OIDC + cookies) and API (JwtBearer).

1) Add packages

Install the relevant NuGet packages:

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

2) Configure authentication in `Program.cs` / `Startup.cs`

Example that supports cookies + OIDC for UI and JwtBearer for API:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// 1) Add authentication
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
    options.Authority = builder.Configuration["AzureAd:Authority"]; // https://login.microsoftonline.com/{tenant}
    options.ClientId = builder.Configuration["AzureAd:ClientId"];
    options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.Scope.Add("offline_access");
    options.Scope.Add(builder.Configuration["AzureAd:ApiScope"]);
});

// 2) Add JWT Bearer for API endpoints (if you expose API controllers)
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options => {
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:ApiAudience"]; // or use the scope/audience
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true
        };
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

Notes:
- If your API and web app are the same process, ensure you correctly select authentication scheme for endpoints: use `[Authorize(AuthenticationSchemes = "Bearer")]` for API controllers, and default cookies/OIDC for UI.
- For simple APIs that accept bearer tokens only, you can remove the OpenIdConnect registration and only configure JwtBearer.

3) App settings (example `appsettings.Production.json`)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "Authority": "https://login.microsoftonline.com/<tenant-id>",
    "ClientId": "<pto.track-web-client-id>",
    "ClientSecret": "<client-secret-from-keyvault>",
    "ApiScope": "api://<pto.track-api-client-id>/access_as_user",
    "ApiAudience": "api://<pto.track-api-client-id>"
  }
}
```

Store `ClientSecret` in Azure Key Vault or other secret store in production.

## Mapping claims, roles and groups
- Azure AD can emit `roles` (app roles) and `groups` claims. When groups overflow, Azure emits a `hasgroups` claim or requires Graph API calls.
- Options:
  - Use App Roles: define roles on the API app registration and assign users/groups to roles — easier for role checks (`RequireRole`).
  - Use groups claim: map group SIDs/ids to application roles; handle overage by calling Microsoft Graph with `on_behalf_of` flow.
- Example: require role `Admin` in policy and assign the role in the Azure Portal to specific users/groups.

## Reverse proxy / ARR notes
- ARR rewriting to Kestrel is simpler with bearer tokens: the Authorization header `Authorization: Bearer <token>` is plain HTTP and will be proxied to the backend.
- Ensure ARR does not strip `Authorization` header. If you must, configure URL Rewrite to preserve it.
- Enforce HTTPS. Configure forwarded headers in ASP.NET Core when behind proxy:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

## Testing
- Web UI (interactive): use browser. Add host to Intranet zone if testing Windows integrated fallback, but with OIDC you should see redirect to Azure login page.
- API testing using curl (client credentials or with an access token):
  - Get token (client credentials): use `curl`/Postman to POST to Azure token endpoint:

```bash
curl -X POST -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=<client-id>&scope=<api-scope>/.default&client_secret=<secret>&grant_type=client_credentials" \
  https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token
```

  - Call API with bearer token:

```bash
curl -H "Authorization: Bearer <access_token>" https://app.example.local/api/endpoint
```

## Integration tests and local development
- Keep the existing test mock authentication for automated tests. For end-to-end tests, use a test tenant or a local test issuer (IdentityServer4/duende or test token generator).
- For local development, you can run the app with `appsettings.Development.json` pointing to a test Azure AD app or use `dotnet user-secrets` to store client secret.

## CI/CD and secrets
- Store ClientSecret in Azure Key Vault and use Managed Identity from your build/release agent or pipeline to fetch secrets.
- Ensure redirect URIs are registered for your deployment slots and environments.

## Migration checklist (recommended phased rollout)
1. Create API app registration and expose scopes.
2. Create web app registration and configure redirect URIs.
3. Update app to accept JWTs and/or OIDC; add configuration and secrets.
4. Test API-only flow using client credentials in a development environment.
5. Test interactive sign-in for UI locally against test tenant.
6. Deploy to staging, update ARR to preserve Authorization header and enforce HTTPS.
7. Roll out to production and gradually disable Windows Integrated Authentication.

## On-prem AD / ADFS considerations
- If you cannot use Azure AD, ADFS can act as an OIDC provider. Register the apps in ADFS and use the ADFS OIDC endpoints in `Authority`.
- If you have on-prem AD but want Azure AD as IdP, set up Azure AD Connect to sync identities (password hash sync or pass-through auth) or enable federation with ADFS.

## Troubleshooting
- `401` / `403` errors:
  - Check token audience and issuer in the token (`az account get-access-token` or decode JWT at jwt.ms).
  - Ensure token scope includes your API scope.
- `groups` overage: call Graph API when `hasgroups` present.
- ARR dropping `Authorization` header: ensure proxy preserves headers and proxy rules do not remove Authorization.
- Mixed scheme confusion: ensure endpoints meant for API require `Bearer` scheme only; annotate controllers with authentication scheme attributes.

## Example small middleware for mapping forwarded identity (if you temporarily accept proxy-auth)
```csharp
app.Use(async (context, next) => {
    if (!context.User.Identity.IsAuthenticated)
    {
        var forwarded = context.Request.Headers["X-Forwarded-User"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded) && /* validate source IP */ true)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, forwarded) };
            var id = new ClaimsIdentity(claims, "Forwarded");
            context.User = new ClaimsPrincipal(id);
        }
    }
    await next();
});
```

## Final notes and recommendations
- Azure AD is the most straightforward long-term solution if you can integrate on-prem AD via Azure AD Connect or federation. It simplifies proxying and scales well.
- Plan to map app roles and group memberships during migration rather than rely on NTLM/Windows groups at the server.
- Keep test/mocks for unit and integration testing and consider using Microsoft.Identity.Web for easier integration with Azure AD and token caching.

---
If you'd like, I will:
- scaffold the `AddJwtBearer` and `AddOpenIdConnect` configuration directly into `AppServiceExtensions.cs` (or `Program.cs`) in this repo,
- or create an Azure AD portal step-by-step doc with exact values to paste into the portal for `pto.track-web` and `pto.track-api`.
Tell me which next action you prefer.
