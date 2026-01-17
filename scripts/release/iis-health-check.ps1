param(
    [Parameter(Mandatory = $false)]
    [string]$HealthUrl = "https://localhost/pto-track/health/live",
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 60,

    [Parameter(Mandatory = $false)]
    [bool]$UseDefaultCredentials = $false,

    [Parameter(Mandatory = $false)]
    [int]$ReadyTimeoutSeconds = 30,

    [Parameter(Mandatory = $false)]
    [bool]$CheckReady = $true
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
    
    # PowerShell 5.1-compatible certificate bypass (UseBasicParsing is required on 5.1)
    $prevCallback = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
    try {
        # Prefer modern TLS; fall back to TLS12 where needed (PS 5.1)
        try { [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls13 } catch { }
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

        # Poll until the timeout expires (friendly retry for slow startups)
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $success = $false
        while ((Get-Date) -lt $deadline) {
            try {
                $resp = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 10 -UseDefaultCredentials:$UseDefaultCredentials -ErrorAction Stop
                if ($resp.StatusCode -eq 200) {
                    $success = $true
                    break
                }
                else {
                    Write-Log "Health probe returned HTTP $($resp.StatusCode). Retrying..."
                }
            }
            catch {
                Write-Log "Health probe attempt failed: $($_.Exception.Message). Retrying..."
            }
            Start-Sleep -Seconds 2
        }
    }
    finally {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $prevCallback
    }
    
    if ($success) {
        Write-Log "Health check passed (HTTP 200)"

        if ($CheckReady) {
            # After liveness success, poll readiness endpoint
            $readyUrl = [System.Uri]::new($HealthUrl)
            $readyUrl = $readyUrl.AbsoluteUri -replace '/live$', '/ready'
            Write-Log "Probing readiness at $readyUrl (timeout $ReadyTimeoutSeconds seconds)"

            $deadlineReady = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
            $readySuccess = $false
            while ((Get-Date) -lt $deadlineReady) {
                try {
                    $respReady = Invoke-WebRequest -Uri $readyUrl -UseBasicParsing -TimeoutSec 10 -UseDefaultCredentials:$UseDefaultCredentials -ErrorAction Stop
                    if ($respReady.StatusCode -eq 200) {
                        $readySuccess = $true
                        break
                    }
                    else {
                        Write-Log "Readiness probe returned HTTP $($respReady.StatusCode). Retrying..."
                    }
                }
                catch {
                    Write-Log "Readiness probe attempt failed: $($_.Exception.Message). Retrying..."
                }
                Start-Sleep -Seconds 2
            }

            if (-not $readySuccess) {
                throw "Readiness check failed after $ReadyTimeoutSeconds seconds"
            }
            else {
                Write-Log "Readiness check passed (HTTP 200)"
            }
        }

        Write-Host "##vso[task.complete result=Succeeded;]Health check passed"
        exit 0
    }
    else {
        throw "Health check failed after $TimeoutSeconds seconds"
    }
}
catch {
    Write-Log "ERROR: Health check failed: $_"
    Write-Error "Health check failed: $_"
    Write-Host "##vso[task.logissue type=error]IIS health check failed"
    exit 1
}
