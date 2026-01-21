 # IIS Deployment Plan — pto.track as IIS Site (Default Web Site)

This document explains how to deploy `pto.track` as an IIS site under `Default Web Site` (in‑process hosting) and how to configure IIS to perform Windows authentication (Negotiate/Kerberos) so the app receives pass‑through authentication. It includes code changes, `web.config` settings, PowerShell deployment scripts (idempotent), Azure Pipelines integration notes, and post‑deploy health checks.

Intended outcome
- Run `pto.track` as an IIS application under `Default Web Site` named `pto-track` served from the same physical path as current deployment artifacts.
- IIS will authenticate users using Windows Authentication (Negotiate preferred), and the app will receive the authenticated principal (in‑process hosting).
- Deployment scripts are idempotent: they only change app pools / apps if necessary and do not restart IIS unless required.

Prerequisites
- Windows Server with IIS and the following features installed: `Web-Server`, `Web-WebServer`, `Web-Asp-Net45` (if required), `Web-Http-Redirect`, `Web-Filtering`, and the `Windows Authentication` IIS feature.
- Administrative privileges on the server to manage IIS and app pools.
- The service account that is currently used by the Kestrel service (if you want to reuse it) or a domain account for the new app pool (username + password).
- The publish artifacts for the app (the same files you currently deploy to the Kestrel host).

High-level steps
1. Remove existing ARR rewrite rules on the corp server (you will do this manually as you stated).
2. (Optional) Stop IIS if required for safe deployment; otherwise operate idempotently.
3. Ensure no conflicting app pool named `pto-track` exists with a mismatched identity; remove only if it exists and identity mismatches.
4. Ensure no conflicting application exists at `Default Web Site/pto-track`; remove only if path mismatch.
5. Deploy published files to the target physical path.
6. Create app pool `pto-track` with the desired identity (domain account) only if it does not exist or has different identity.
7. Create the application `/pto-track` under `Default Web Site` pointing to the deployed path and set its app pool.
8. Configure IIS Authentication for the application: disable anonymous, enable Windows Authentication and set providers with Negotiate preferred.
9. Ensure the `web.config` includes ANCM settings for in‑process hosting (see snippet below).
10. (Optional) Restart IIS if you had to change global IIS settings or the ANCM module requires it.
11. Perform post‑deploy health checks.

Code changes required in the application
1. Enable Negotiate authentication in the app conditionally when configuration selects `Windows` or `ActiveDirectory` mode. Example `Program.cs` change:

```csharp
// detect auth mode from configuration
var authMode = builder.Configuration["Authentication:Mode"]; // e.g. "Mock" | "Windows" | "AzureAD"
if (authMode == "Windows" || authMode == "ActiveDirectory")
{
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
// retain other authentication registrations for other modes

app.UseAuthentication();
app.UseAuthorization();
```

Notes:
- `AddNegotiate()` uses the server environment to accept Windows authentication tokens. When hosted in‑process under IIS, the IIS worker (`w3wp`) receives the Windows token and the Negotiate middleware will accept it.
- If you currently have test-only `TestAuthHandler` wiring, ensure configuration for `Testing` environment still selects `Mock`.

2. Optionally add `IClaimsTransformation` to enrich the principal after Windows auth. This will run for both Negotiate and OIDC flows:

```csharp
services.AddTransient<IClaimsTransformation, MyClaimsEnricher>();
```

web.config (in‑process) snippet
Place this `web.config` in the site physical path alongside your published files. This config uses InProcess hostingModel. Adjust `processPath`/arguments for your publish layout if necessary.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <security>
      <requestFiltering />
    </security>
    <aspNetCore processPath="dotnet" 
                arguments=".\pto.track.dll"
                stdoutLogEnabled="false"
                hostingModel="InProcess" />
  </system.webServer>
</configuration>
```

Important: for InProcess hosting the app runs inside `w3wp` and IIS performs Windows Authentication itself (no `forwardWindowsAuthToken` required).

PowerShell deployment scripts
The following scripts are idempotent and designed to be invoked from your pipeline. They require the `WebAdministration` module (installed with IIS).

Predeploy: `scripts\release\before-deploy-iis.ps1`

```powershell
param(
  [string]$PhysicalPath,
  [string]$AppPoolName = 'pto-track',
  [string]$AppName = 'pto-track',
  [string]$AppPoolUser = '',
  [string]$AppPoolPassword = '',
  [switch]$StopIISIfNeeded
)

Import-Module WebAdministration

function Write-Log($m){ Write-Host "[before-deploy] $m" }

Write-Log "Checking IIS features and WebAdministration module..."
if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
  throw "WebAdministration module not available. Ensure IIS Management Scripts & Tools installed."
}

# Optional: stop IIS if requested
if ($StopIISIfNeeded) {
  Write-Log "Stopping IIS (w3svc)"
  Stop-Service W3SVC -ErrorAction Stop
}

# Remove existing app if path mismatch
$siteAppPath = "Default Web Site/$AppName"
if (Test-Path IIS:\Sites\"Default Web Site\$AppName") {
  $existingPhysical = (Get-WebApplication -Site 'Default Web Site' -Name $AppName).physicalPath
  if ($existingPhysical -and ($existingPhysical -ne $PhysicalPath)) {
    Write-Log "Existing app found with different physical path. Removing application $siteAppPath"
    Remove-WebApplication -Site 'Default Web Site' -Name $AppName -Confirm:$false
  } else {
    Write-Log "Application exists and points to same path; leaving in place."
  }
}

# App pool: only remove if exists and identity mismatches
if (Test-Path IIS:\AppPools\$AppPoolName) {
  $pool = Get-Item IIS:\AppPools\$AppPoolName
  $currentUser = $pool.processModel.userName
  if ($AppPoolUser -and ($currentUser -ne $AppPoolUser)) {
    Write-Log "AppPool exists but identity mismatch (current='$currentUser', desired='$AppPoolUser'). Removing pool."
    Remove-WebAppPool $AppPoolName
  } else { Write-Log "AppPool exists and identity matches (or no desired user provided)." }
}

Write-Log "Pre-deploy checks complete."

# Start IIS back if stopped earlier
if ($StopIISIfNeeded) { Start-Service W3SVC }

Write-Log "Done."
```

Deploy: use your existing artifact copy to the target `$PhysicalPath`. (Pipeline artifact copy / robocopy step)

Postdeploy: `scripts\release\finish-deploy-iis.ps1`

```powershell
param(
  [string]$PhysicalPath,
  [string]$AppPoolName = 'pto-track',
  [string]$AppName = 'pto-track',
  [string]$AppPoolUser = '',
  [string]$AppPoolPassword = ''
)

Import-Module WebAdministration

function Write-Log($m){ Write-Host "[finish-deploy] $m" }

# Create app pool if missing
if (-not (Test-Path IIS:\AppPools\$AppPoolName)) {
  Write-Log "Creating app pool $AppPoolName"
  New-WebAppPool -Name $AppPoolName
  $pool = Get-Item IIS:\AppPools\$AppPoolName
  $pool.processModel.identityType = 3 # SpecificUser
  if ($AppPoolUser) { $pool.processModel.userName = $AppPoolUser }
  if ($AppPoolPassword) { $pool.processModel.password = $AppPoolPassword }
  Set-Item IIS:\AppPools\$AppPoolName $pool
  # .NET CLR: No Managed Code for ASP.NET Core
  Set-ItemProperty IIS:\AppPools\$AppPoolName -Name managedRuntimeVersion -Value ''
} else {
  Write-Log "App pool $AppPoolName already exists. Ensuring identity is set if provided."
  $pool = Get-Item IIS:\AppPools\$AppPoolName
  if ($AppPoolUser -and ($pool.processModel.userName -ne $AppPoolUser)) {
    Write-Log "Updating app pool identity to $AppPoolUser"
    $pool.processModel.userName = $AppPoolUser
    $pool.processModel.password = $AppPoolPassword
    $pool.processModel.identityType = 3
    Set-Item IIS:\AppPools\$AppPoolName $pool
  }
}

# Create or update application under Default Web Site
if (-not (Test-Path IIS:\Sites\"Default Web Site\$AppName")) {
  Write-Log "Creating application Default Web Site/$AppName -> $PhysicalPath"
  New-WebApplication -Site 'Default Web Site' -Name $AppName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName
} else {
  Write-Log "Application exists; ensuring application pool and physical path"
  $app = Get-WebApplication -Site 'Default Web Site' -Name $AppName
  if ($app.applicationPool -ne $AppPoolName) { Set-ItemProperty IIS:\Sites\"Default Web Site\$AppName" -Name applicationPool -Value $AppPoolName }
  if ($app.physicalPath -ne $PhysicalPath) { Set-ItemProperty IIS:\Sites\"Default Web Site\$AppName" -Name physicalPath -Value $PhysicalPath }
}

# Configure authentication: anonymous off, windows auth on
Write-Log "Configuring authentication for application"
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name enabled -Value false
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name enabled -Value true

# Ensure Negotiate provider present first (fallback to NTLM)
Write-Log "Configuring Windows auth providers (Negotiate,NTLM)"
$providers = Get-WebConfiguration "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName"
# Remove all and add Negotiate then NTLM
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -AtElement @{value='Negotiate'} -ErrorAction SilentlyContinue
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -AtElement @{value='NTLM'} -ErrorAction SilentlyContinue
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -Value @{value='Negotiate'}
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -Value @{value='NTLM'}

Write-Log "Recycling app pool"
Restart-WebAppPool $AppPoolName

Write-Log "Done."
```

Notes on the scripts
- They use the `WebAdministration` provider (`IIS:\`) which requires running as an administrator and that the IIS Management Scripts & Tools feature is installed.
- They are intentionally conservative: they *only* remove the existing app pool if the identity differs, and they do not stop/restart IIS unless requested.
- You can wrap the deployment artifact copy between the `before-deploy-iis.ps1` and `finish-deploy-iis.ps1` steps in your pipeline.

Azure Pipelines integration (example tasks)
Add three pipeline steps in your release job: `Pre-deploy`, `Deploy`, `Post-deploy`.

- Pre-deploy (PowerShell on target machine via WinRM or deployment agent):
```yaml
- task: PowerShell@2
  displayName: 'Pre-deploy: IIS checks'
  inputs:
    targetType: 'inline'
    script: |
      & .\scripts\release\before-deploy-iis.ps1 -PhysicalPath "C:\inetpub\wwwroot\pto-track" -AppPoolUser "DOMAIN\KestrelSvc" -AppPoolPassword "$(appPoolPassword)" -StopIISIfNeeded:$false
```

- Deploy (copy files)
```yaml
- task: CopyFiles@2
  displayName: 'Copy publish artifacts'
  inputs:
    SourceFolder: '$(Build.ArtifactStagingDirectory)'
    Contents: '**'
    TargetFolder: 'C:\inetpub\wwwroot\pto-track'
```

- Post-deploy (configure app pool and app, enable auth, health check):
```yaml
- task: PowerShell@2
  displayName: 'Post-deploy: configure IIS app and healthcheck'
  inputs:
    targetType: 'inline'
    script: |
      & .\scripts\release\finish-deploy-iis.ps1 -PhysicalPath 'C:\inetpub\wwwroot\pto-track' -AppPoolUser 'DOMAIN\KestrelSvc' -AppPoolPassword '$(appPoolPassword)'
      # health check
      $hc = Invoke-WebRequest -Uri 'https://localhost/pto-track/health' -UseBasicParsing -ErrorAction SilentlyContinue
      if ($hc.StatusCode -ne 200) { throw 'Health check failed' }
```

Post‑deploy health checks
- Check the main health endpoint (e.g., `/health`) and an auth test endpoint (e.g., `/api/currentuser/debug/claims`) to confirm Windows auth propagation. For the auth test, use a browser where integrated auth is enabled or run `curl.exe --negotiate -u :` from a domain-joined client.

Example health checks (PowerShell):
```powershell
# simple site health
$r = Invoke-WebRequest -Uri 'https://localhost/pto-track/health' -UseBasicParsing -TimeoutSec 15
if ($r.StatusCode -ne 200) { throw 'Health endpoint returned non-200' }

# windows auth smoke test from server (uses machine credentials; useful if service account has rights)
try {
  $r2 = Invoke-WebRequest -Uri 'https://localhost/pto-track/api/currentuser/debug/claims' -UseDefaultCredentials -UseBasicParsing -TimeoutSec 15
  if ($r2.StatusCode -ne 200) { Write-Host "Auth smoke test returned $($r2.StatusCode)" }
} catch { Write-Host "Auth smoke test failed: $_" }
```

Additional considerations & caveats
- SPN/Delegation: If you previously relied on Kerberos delegation through ARR, moving to in‑process hosting under IIS will change which account receives the Kerberos ticket. Ensure SPNs map to the site hostname and the account that the app pool runs under if delegation or constrained delegation is needed.
- App pool identity: choose a domain account with least privileges required. If you reuse the Kestrel service account, ensure it has `Log on as a service` and appropriate permissions to the physical path and any resources.
- Logging/monitoring: ensure your existing logging settings are preserved and that files are writable by the app pool identity.
- Firewalls / TLS: ensure certs are present for TLS; remove previous HTTPS exception once you validate site works.

If you want, I can:
- scaffold the `AddNegotiate()` and `IClaimsTransformation` changes into `AppServiceExtensions.cs`,
- create the `scripts/release/before-deploy-iis.ps1` and `scripts/release/finish-deploy-iis.ps1` files in the repo, and
- add the pipeline YAML snippet into `azure-pipelines.yml` behind a conditional deployment stage.

---
Document version: 2025-12-24
