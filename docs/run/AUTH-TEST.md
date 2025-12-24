# Authentication Test & Diagnostic Checklist

Use this checklist when testing integrated Windows authentication (or Negotiate) against the corp server. Paste any JSON output you capture into the ticket or chat and I will parse it and map claims to our app fields.

1) What to capture
- Full JSON body from: `GET /api/currentuser/debug/claims` (or `/api/currentuser` debug endpoint). Copy the whole JSON.
- Server log lines around the request — especially lines mentioning authentication handlers (e.g. `AuthenticationScheme: Negotiate`, `AuthenticationScheme: NTLM`, `AuthenticationScheme: Cookies`).
- The request headers for the same request (Authorization, Host, Origin, User-Agent).

2) Quick checks (if no creds arrive)
- Server: verify Windows Authentication is enabled (IIS) or `Negotiate` is configured in `Program.cs` (Kestrel) and package `Microsoft.AspNetCore.Authentication.Negotiate` is installed.
- Browser: ensure integrated auth is allowed for the site:
  - Internet Explorer / Edge (Windows Integrated Auth): add site to Intranet zone.
  - Chrome: uses OS settings; add site to Local intranet or launch Chrome with `--auth-server-whitelist="*.corp.example.com"` for testing.
  - Firefox: set `network.negotiate-auth.trusted-uris` in about:config.
- Confirm the server is being reached as the same hostname you configured for intranet/whitelist (SPN/host name matters for Kerberos).

3) Useful curl test (domain-joined machine)
```powershell
curl --negotiate -u : -i https://your.corp.site/api/currentuser/debug/claims
```
This attempts SPNEGO using your logged-in ticket and prints the response headers/body.

4) What the JSON will look like on corp server (expectations)
- `identityName`: `CORP\\jsmith` or UPN like `jsmith@corp.example.com`
- `authenticationType`: typically `Negotiate` (Kerberos) or `NTLM`
- `claims`: many entries — common useful ones:
  - `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` (domain\\user or display name)
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid` (user SID)
  - `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn` (UPN)
  - `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` or `mail`
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/objectguid` or `oid`
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid` (many group SIDs)

Example (representative):
```
{
  "identityName":"CORP\\jsmith",
  "authenticationType":"Negotiate",
  "isAuthenticated":true,
  "claims":[
    {"type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name","value":"CORP\\jsmith"},
    {"type":"http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid","value":"S-1-5-21-...-1001"},
    {"type":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn","value":"jsmith@corp.example.com"},
    {"type":"http://schemas.microsoft.com/ws/2008/06/identity/claims/objectguid","value":"a3a3b377-a2a7-4a14-9b2c-52e1643fb446"},
    {"type":"http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid","value":"S-1-5-21-...-513"}
  ]
}
```

5) Mapping to our app
- Our app reads claims into these application fields (in order of precedence):
  - employeeNumber: claim types like `employeeNumber`, `employeeID` or fallback.
  - email: `email`, `mail`, `upn` (if used for email).
  - display name: `name`, `displayName` or `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`.
  - AD id / object id: `objectGUID`, `objectidentifier`, or OIDC `oid`/`sub` depending on provider.
  - roles: AD group membership arrives as `groupsid` SIDs — the app maps known group SIDs or names to application roles (Admin/Manager/Approver). If only SIDs arrive, mapping requires lookup or configuration.

6) Common gotchas & fixes
- If you see no auth claims (anonymous): check server config (IIS Windows Auth enabled, Anonymous disabled) and browser intranet settings.
- If you see `NTLM` but not Kerberos and you expect Kerberos: check SPN configuration and host name used; Kerberos requires correct SPN and DNS name.
- Group claims are often SIDs — convert to names by querying AD (not done automatically). Our app contains mapping logic for configured SIDs/groups; if mapping fails, add the relevant SID to config or map in code.
- If corp uses Azure AD / OIDC, you'll see JWT claim keys (`sub`, `oid`, `upn`, `email`, `roles`) instead of WS URIs.

7) What to paste here
- Full JSON from `GET /api/currentuser/debug/claims` (body). I will parse and produce:
  - Exact claim URIs/keys our app receives.
  - Which app fields will be populated and which will be missing.
  - If group SIDs appear, I can list the SIDs and recommend mapping steps.

8) Next steps I can take for you
- Parse the JSON and produce a field→claim mapping with recommended changes.
- Add a short `docs/auth/AD-SETUP.md` describing IIS/Kestrel and browser steps for your corp environment.

---
File: `docs/run/AUTH-TEST.md`
