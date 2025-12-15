# IIS Reverse Proxy Setup for Kestrel-Hosted ASP.NET Core Apps (Finalized Guidance)

This guide explains how to configure IIS as a reverse proxy for an ASP.NET Core application running as a Windows Service (or standalone Kestrel process), based on your working production setup.

## Key Points
- **web.config is not needed** in your published app folder for reverse proxy scenarios.
- IIS acts only as a reverse proxy, forwarding all requests under a path (e.g., /pto-track) to your Kestrel app.
- All proxy and rewrite rules are set at the IIS site/server level, not in your app’s web.config.

## Steps

### 1. Prerequisites
- [Application Request Routing (ARR)](https://www.iis.net/downloads/microsoft/application-request-routing) and [URL Rewrite](https://www.iis.net/downloads/microsoft/url-rewrite) must be installed on your IIS server.

### 2. Enable Proxy in ARR
```powershell
Import-Module WebAdministration
Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.webServer/proxy" -name "enabled" -value "True"
```

### 3. Add a Single Catch-All URL Rewrite Rule
- In IIS Manager, select your site and open **URL Rewrite**.
- Add a **Blank rule** with these settings:

  - **Name:** ReverseProxyToKestrel
  - **Match URL:**
    - Requested URL: Matches the Pattern
    - Using: Regular Expressions
    - Pattern: `^(.*)$`
  - **Conditions:**
    - Add: `{REQUEST_FILENAME}` Is Not a File
    - (Do NOT add the directory check)
  - **Action:**
    - Action type: Rewrite
    - Rewrite URL: `http://localhost:5139/pto-track/{R:1}`
    - Append query string: checked
    - Stop processing of subsequent rules: checked

### 4. Remove Physical Directory Conflicts
- Ensure there is **no physical pto-track directory** in your IIS site’s root (e.g., C:\inetpub\wwwroot or wherever your IIS site points).
- Remove any static files (index.html, default.htm, etc.) from the IIS site’s root that could conflict.

### 5. Restart IIS (if needed)
```powershell
iisreset
```

## How It Works
- All requests to `/pto-track/*` (including `/pto-track/`) are proxied to your Kestrel app at `http://localhost:5139/pto-track/*`.
- Your ASP.NET Core app’s routing (including Razor Pages like Index.cshtml) handles all requests.
- No static file or directory checks by IIS interfere with proxying.

## Troubleshooting
- If you see 403.14 errors for the root, ensure the directory check is **not** present in the rule’s conditions.
- If you see static files or directory listings, ensure there is no physical pto-track directory in the IIS site’s root.

---

This setup is robust and matches your working production configuration. If you need to proxy a different path or port, adjust the rule accordingly.
