# IIS Post-Deploy Health Checks

Use these scripts to verify the IIS site is running correctly after deployment.

## Simple site health check

```powershell
$siteName = "pto-track"
$healthUrl = "https://localhost/$siteName/health"

try {
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 15 -SkipCertificateCheck
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Health check passed (HTTP 200)"
        exit 0
    } else {
        Write-Host "✗ Health check failed (HTTP $($response.StatusCode))"
        exit 1
    }
} catch {
    Write-Host "✗ Health check request failed: $_"
    exit 1
}
```

## Windows Authentication smoke test

Verify that Windows Authentication (Negotiate) is working and the app receives claims:

```powershell
$siteName = "pto-track"
$claimsUrl = "https://localhost/$siteName/api/currentuser/debug/claims"

try {
    # Use default credentials (machine account when running on the server)
    $response = Invoke-WebRequest -Uri $claimsUrl `
        -UseDefaultCredentials `
        -UseBasicParsing `
        -TimeoutSec 15 `
        -SkipCertificateCheck
    
    if ($response.StatusCode -eq 200) {
        $claims = $response.Content | ConvertFrom-Json
        Write-Host "✓ Auth smoke test passed"
        Write-Host "  - Identity: $($claims.identityName)"
        Write-Host "  - Normalized: $($claims.normalizedIdentity)"
        Write-Host "  - Claims: $($claims.claimCount)"
        exit 0
    } else {
        Write-Host "✗ Auth test failed (HTTP $($response.StatusCode))"
        exit 1
    }
} catch {
    Write-Host "✗ Auth test request failed: $_"
    exit 1
}
```

## Full deployment verification script

```powershell
param(
    [string]$SiteName = 'pto-track',
    [string]$HostName = 'localhost',
    [int]$TimeoutSec = 15
)

function Write-Section($title) {
    Write-Host ""
    Write-Host "=== $title ===" -ForegroundColor Cyan
}

function Test-Endpoint($uri, $testName, $useAuth = $false) {
    try {
        $params = @{
            Uri                = $uri
            UseBasicParsing    = $true
            TimeoutSec         = $TimeoutSec
            SkipCertificateCheck = $true
        }
        if ($useAuth) { $params['UseDefaultCredentials'] = $true }
        
        $response = Invoke-WebRequest @params
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ $testName passed (HTTP 200)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ $testName failed (HTTP $($response.StatusCode))" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ $testName failed: $_" -ForegroundColor Red
        return $false
    }
}

Write-Section "Health & Auth Verification for $SiteName"

$healthUrl = "https://$HostName/$SiteName/health"
$claimsUrl = "https://$HostName/$SiteName/api/currentuser/debug/claims"

Write-Host "Target: $HostName"
Write-Host "Site: $SiteName"
Write-Host ""

$healthOk = Test-Endpoint $healthUrl "Health endpoint" $false
$authOk = Test-Endpoint $claimsUrl "Windows Auth (debug/claims)" $true

Write-Section "Summary"
if ($healthOk -and $authOk) {
    Write-Host "✓ All checks passed" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Some checks failed" -ForegroundColor Red
    exit 1
}
```

## Azure Pipelines integration

Add a post-deploy task in your release pipeline:

```yaml
- task: PowerShell@2
  displayName: 'Post-deploy: health check'
  inputs:
    targetType: 'filePath'
    filePath: '$(System.DefaultWorkingDirectory)/scripts/release/health-check.ps1'
    arguments: '-SiteName "pto-track" -HostName "localhost"'
```

Or inline:

```yaml
- task: PowerShell@2
  displayName: 'Post-deploy: health check'
  inputs:
    targetType: 'inline'
    script: |
      $healthUrl = 'https://localhost/pto-track/health'
      $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 15 -SkipCertificateCheck
      if ($response.StatusCode -ne 200) { throw "Health check failed with HTTP $($response.StatusCode)" }
      Write-Host "✓ Health check passed"
```
