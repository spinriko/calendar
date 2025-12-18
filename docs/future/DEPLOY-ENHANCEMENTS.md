# Deploy Enhancements

This document captures recommended improvements to stabilize builds and harden deployments across environments.

## Post-Deploy Checks
- **Environment Variables:** After deployment, verify `ASPNETCORE_ENVIRONMENT` and `ConnectionStrings__PtoTrackDbContext` at Machine scope on the target server. Log their values. Restart the app pool/service to ensure new values are applied.
- **Health Endpoints:** Probe `/health/ready` (readiness) and `/health/live` (liveness) with retries and backoff. Fail the pipeline if non-200. Optionally check `/api/CurrentUser` for auth wiring.
- **Smoke Tests:** Fetch `PathBase` banner or a diagnostic endpoint to confirm the app recognizes its base path. Validate `/dist/asset-manifest.json` and a key bundle (e.g., `/dist/site.js`) resolve under the virtual directory.

## Pipeline Hardening
- **Environment Parameterization:** Use stage-specific variable groups (e.g., Dev, Test, Prod) for `sqlConnectionString` and other config, with appropriate secret scopes. Optionally integrate Azure Key Vault for secrets.
- **Rollback Strategy:** Preserve previous deployment (already backed up). On failure or failing smoke tests, automatically swap back to the backup folder and surface a clear failure in the pipeline.
- **Notifications & Gates:** Add manual approval gates for Prod, and send notifications on failures (Teams/Email). Include artifact links and last successful version.

## Build & Publish Integrity
- **Bundle Verification:** Ensure `npm ci` and `npm run build` execute prior to `dotnet publish`. Verify `wwwroot/dist/` and `asset-manifest.json` exist in the published artifact.
- **Static Asset Headers:** Keep `asset-manifest.json` served with `no-cache` and hashed bundles as `immutable` with long max-age (config in the app’s `HostingExtensions.ConfigureStaticFiles`).
- **Artifact Contents:** Confirm the packaged zip includes `wwwroot/dist` and that extraction preserves paths. Add a post-extract check for `asset-manifest.json` presence.

## Reverse Proxy & PathBase
- **PathBase Consistency:** Ensure the app’s `PathBase` matches the IIS virtual directory (e.g., `/pto-track`). Avoid stripping the subpath at the proxy unless the app config removes `PathBase` accordingly.
- **Rewrite Rules:** Preserve subpath when proxying so `UsePathBase` can trim it. Example intent: route `^pto-track/(.*)` to the backend without losing the `pto-track` segment.
- **Proxy Health:** Probe health and smoke-test endpoints through the proxy with the full mounted path to catch path mismatches early.

## Example Post-Deploy Scripts (PowerShell on target machines)
- **Environment & Connection String:**
  ```powershell
  # Set ASPNETCORE_ENVIRONMENT=Development (Machine)
  [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Machine")
  Write-Host "ASPNETCORE_ENVIRONMENT (Machine): " ([Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Machine"))

  # Set ConnectionStrings__PtoTrackDbContext (Machine)
  $conn = "$(sqlConnectionString)"  # resolved from variable group in the pipeline
  [Environment]::SetEnvironmentVariable("ConnectionStrings__PtoTrackDbContext", $conn, "Machine")
  Write-Host "ConnectionStrings__PtoTrackDbContext length: " ([Environment]::GetEnvironmentVariable("ConnectionStrings__PtoTrackDbContext", "Machine").Length)
  ```
- **Health + Smoke Tests:**
  ```powershell
  param(
    [string]$BaseUrl
  )

  $ErrorActionPreference = 'Stop'
  function Invoke-WithRetry($Url, $Attempts=5, $DelaySeconds=5) {
    for ($i=1; $i -le $Attempts; $i++) {
      try {
        $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 15
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300) { return $true }
      } catch { Start-Sleep -Seconds $DelaySeconds }
    }
    return $false
  }

  $ready = Invoke-WithRetry "$BaseUrl/health/ready"
  $live  = Invoke-WithRetry "$BaseUrl/health/live"
  $manifest = Invoke-WithRetry "$BaseUrl/dist/asset-manifest.json"
  $site = Invoke-WithRetry "$BaseUrl/dist/site.js"

  if (-not ($ready -and $live -and $manifest -and $site)) {
    throw "Post-deploy checks failed: ready=$ready live=$live manifest=$manifest site=$site"
  }
  Write-Host "Post-deploy checks passed."
  ```

## Operational Recommendations
- **Service/IIS Restart:** After setting machine-level variables, restart the service/app pool so the app picks up the new environment.
- **Observability:** Add application logs and request tracing around startup (log `UsePathBase` value, static file configuration, and map endpoints). Surface logs in the pipeline for quicker triage.
- **Documentation:** Keep environment-specific deployment notes updated, including PathBase, proxy rules, and variable groups.
