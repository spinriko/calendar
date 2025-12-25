param(
    [Parameter(Mandatory = $false)]
    [string]$HealthUrl = "https://localhost/pto-track/health",
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 15
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    Write-Log "IIS health check started"
    Write-Log "Health URL: $HealthUrl"
    Write-Log "Timeout: $TimeoutSeconds seconds"
    
    Write-Host "Running health checks..."
    
    $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec $TimeoutSeconds -SkipCertificateCheck
    
    if ($response.StatusCode -eq 200) {
        Write-Log "✓ Health check passed (HTTP 200)"
        Write-Host "##vso[task.complete result=Succeeded;]Health check passed"
        exit 0
    } else {
        throw "Health check returned HTTP $($response.StatusCode)"
    }
}
catch {
    Write-Log "ERROR: Health check failed: $_"
    Write-Error "Health check failed: $_"
    Write-Host "##vso[task.logissue type=error]IIS health check failed"
    exit 1
}
