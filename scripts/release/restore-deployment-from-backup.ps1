param(
    [Parameter(Mandatory = $true)][string]$DeploymentFolder,
    [Parameter(Mandatory = $true)][string]$BackupFolder,
    [Parameter(Mandatory = $false)][string]$AppPoolName = 'pto-track',
    [Parameter(Mandatory = $false)][string]$ArchiveRoot = 'C:\self-contained_apps\deployment_archives'
)

function Write-Log($m) { Write-Host "[restore-deploy] $m" }

try {
    Write-Log "Starting restore from backup"
    if (-not (Test-Path $BackupFolder)) {
        Write-Error "Backup folder not found: $BackupFolder"
        exit 1
    }

    # Ensure archive root exists
    if (-not (Test-Path $ArchiveRoot)) { New-Item -ItemType Directory -Force -Path $ArchiveRoot | Out-Null }

    $timestamp = (Get-Date -Format 'yyyyMMddHHmmss')
    $failedArchive = Join-Path $ArchiveRoot "failed_$timestamp"

    # Move current deployment out of the way
    if (Test-Path $DeploymentFolder) {
        Write-Log "Archiving current deployment to $failedArchive"
        Move-Item -Path $DeploymentFolder -Destination $failedArchive -Force
    }

    # Restore backup -> deployment
    Write-Log "Restoring backup $BackupFolder -> $DeploymentFolder"
    Move-Item -Path $BackupFolder -Destination $DeploymentFolder -Force

    # Restart app pool
    try {
        Import-Module WebAdministration -ErrorAction Stop
        Write-Log "Restarting app pool: $AppPoolName"
        Restart-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 2
    }
    catch {
        Write-Log "Warning: could not restart app pool via WebAdministration: $_"
    }

    # Run a quick local health probe (liveness)
    $liveUrl = "http://localhost/pto-track/health/live"
    Write-Log "Probing liveness at $liveUrl"
    $ok = $false
    try {
        $resp = Invoke-WebRequest -Uri $liveUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) { $ok = $true }
    } catch {
        Write-Log "Liveness probe failed: $($_.Exception.Message)"
    }

    if ($ok) {
        Write-Log "Restore succeeded and app is responding on /health/live"
        exit 0
    }
    else {
        Write-Error "Restore completed but liveness probe failed. Check server logs at $failedArchive"
        exit 2
    }
}
catch {
    Write-Error "Restore failed: $_"
    exit 1
}