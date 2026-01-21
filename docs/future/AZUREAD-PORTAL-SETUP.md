### Azure Portal: Register pto.track-web and pto.track-api

This guide walks through step-by-step instructions to register two Azure AD applications for the pto.track migration described in AZUREAD-OIDC-MIGRATION.md. It covers: creating the API app (`pto.track-api`), creating the web app (`pto.track-web`), exposing scopes, adding delegated permissions, creating client secrets, assigning roles/groups, granting admin consent, and testing tokens.

Replace placeholder values with your environment values (tenant id, hostnames, client ids).

#### Prerequisites
- Azure subscription and Azure AD admin privileges (Application Administrator or Global Admin) for creating app registrations and granting consent.
- Known hostnames for the application (staging, production) and redirect URIs.

#### Naming convention used in examples
- API App registration: `pto.track-api`
- Web App registration: `pto.track-web`

1) Create the API app registration (`pto.track-api`)

- Portal: Azure Active Directory -> App registrations -> New registration
  - Name: pto.track-api
  - Supported account types: Accounts in this organizational directory only (Single tenant) or choose appropriate
  - Redirect URI: (leave blank for API)
  - Click Register

- After registration, note the `Application (client) ID` and `Directory (tenant) ID`.

- Expose an API (define scope)
  1. In the left menu of the app registration, click `Expose an API`.
  2. Set `Application ID URI` if not already set (e.g., `api://<client-id>` or `https://api.contoso.com/pto-track`).
  3. Click `Add a scope` and create a scope:
     - Scope name: `access_as_user` (example)
     - Who can consent: Admins and users
     - Admin consent display name: Access pto.track API as the signed-in user
     - Admin consent description: Allow the app to access pto.track API on behalf of the signed-in user.
     - State: Enabled
  4. Save. Note the full scope URI: e.g., `api://<pto.track-api-client-id>/access_as_user`.

- (Optional) Define App Roles (if you want role-based `roles` claim emitted)
  1. In `App registrations` -> `pto.track-api` -> `App roles` -> Create app role
     - Display name: Admin
     - Value: Admin
     - Description: Administrative access to pto.track
     - Allowed member types: Users/Groups
  2. Save. Later assign users/groups to this role in Enterprise Applications -> Users and groups.

2) Create the Web app registration (`pto.track-web`)

- Portal: Azure Active Directory -> App registrations -> New registration
  - Name: pto.track-web
  - Supported account types: Same as above
  - Redirect URI -> Web: `https://app.example.local/signin-oidc` (adjust for environment)
  - Click Register

- Note the `Application (client) ID` for `pto.track-web`.

- Add API permissions
  1. In `pto.track-web` -> `API permissions` -> `Add a permission` -> `My APIs` -> select `pto.track-api`.
  2. Choose delegated permissions and select the scope you added earlier (`access_as_user`).
  3. Click Add permissions.

- Create client secret (for server-side web app)
  1. In `pto.track-web` -> `Certificates & secrets` -> `New client secret`.
  2. Description: pto.track-web-secret, Expires: as appropriate.
  3. Save and copy the secret value immediately — store in Azure Key Vault or secure store.

3) Grant admin consent for the web app (so users don't prompt)

- In `pto.track-web` -> `API permissions`, click `Grant admin consent for <Tenant>` and confirm. This requires an admin.

4) Configure optional group/role assignments

- If you used App Roles on `pto.track-api`, assign users/groups:
  1. Azure AD -> Enterprise applications -> Select `pto.track-api` -> Users and groups -> Add user/group -> select role (Admin) -> Assign.

5) Configure Authentication settings for `pto.track-web`

- In `pto.track-web` -> `Authentication`:
  - Platform configurations: ensure Web is added with the redirect URI used in your app (e.g., `/signin-oidc`).
  - Under `Implicit grant and hybrid flows` do NOT enable implicit flows for server-side apps; use Authorization Code flow.
  - Under `Advanced settings`, set `Allow public client flows` depending on SPA usage — usually false for server-side web apps.

6) Configure single logout and front-channel logout (optional)

- In `Authentication`, set `Front-channel logout URL` and `Logout URL` if your app handles sign-out.

7) Update application configuration

- In your app's `appsettings.*.json` (production/staging) add entries:

```json
"AzureAd": {
  "Authority": "https://login.microsoftonline.com/<tenant-id>",
  "ClientId": "<pto.track-web-client-id>",
  "ClientSecret": "<client-secret>",
  "ApiScope": "api://<pto.track-api-client-id>/access_as_user",
  "ApiAudience": "api://<pto.track-api-client-id>"
}
```

Store `ClientSecret` in Azure Key Vault and reference it from the app configuration if possible.

8) Test: obtain tokens and call the API

- Client credentials (machine-to-machine, **requires application permission** on `pto.track-api`):
  ```bash
  curl -X POST -H "Content-Type: application/x-www-form-urlencoded" \
    -d "client_id=<pto.track-api-client-id>&scope=api://<pto.track-api-client-id>/.default&client_secret=<secret>&grant_type=client_credentials" \
    https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token
  ```

- Delegated user flow (web app requests access token on behalf of user):
  - Use browser sign-in to `pto.track-web` and ensure `Authorization Code` flow returns tokens. The web app should exchange the code for id_token + access_token for the `ApiScope`.

- Call API with bearer token:
  ```bash
  curl -H "Authorization: Bearer <access_token>" https://app.example.local/api/currentuser/debug/claims
  ```

9) Admin consent and enterprise applications

- If you need organization-wide consent, ensure admin grants consent for delegated permissions. You can also pre-assign the app to specific users/groups via Enterprise applications.

10) Troubleshooting

- `unauthorized` or `invalid_audience` — ensure `aud` in the token matches `ApiAudience`/resource.
- `AADSTS` errors on redirect — check redirect URI exact match and that client secret is valid.
- No `roles`/`groups` claim — if you used App Roles ensure the role is assigned; for groups, consider Graph API when overage occurs.
- ARR/proxy dropping Authorization header — confirm ARR preserves `Authorization` header or configure it to forward.

11) Best practices

- Use certificate credentials instead of client secrets where possible.
- Use Azure Key Vault and Managed Identity for secrets retrieval.
- Limit redirect URIs to exact production/staging hosts.
- Define App Roles on the API registration and manage assignments centrally.

12) Example manifest edits (app roles)

Edit `pto.track-api` manifest to include an app role block:

```json
"appRoles": [
  {
    "allowedMemberTypes": ["User"],
    "description": "Administrators",
    "displayName": "Admin",
    "id": "<new-guid>",
    "isEnabled": true,
    "value": "Admin"
  }
]
```

13) Next steps after registration

- Implement the OIDC/JWT middleware in `Program.cs`/`AppServiceExtensions.cs`.
- Map claims to application roles and adapt authorization policies.
- Update CI to populate Key Vault secrets and deployment configs.

If you want, I can now scaffold the exact `AddOpenIdConnect` / `AddJwtBearer` changes into `AppServiceExtensions.cs` using these registration values (placeholders), or produce a fill-in-the-blanks Azure Portal checklist PDF.
