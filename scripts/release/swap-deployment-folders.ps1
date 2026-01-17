param(
    [Parameter(Mandatory = $true)][string]$DeploymentFolder,
    [Parameter(Mandatory = $true)][string]$BackupFolder,
    [Parameter(Mandatory = $true)][string]$TempFolder
)

function Write-Log($m) { Write-Host "[swap-deployment] $m" }

Write-Log "Running script diagnostics..."
$scriptPath = $MyInvocation.MyCommand.Path
Write-Log "  Invoked from: $scriptPath"
if (Test-Path $scriptPath) {
    $scriptInfo = Get-Item $scriptPath
    $hash = Get-FileHash -Algorithm SHA256 -Path $scriptPath
    Write-Log "  LastWriteTime: $($scriptInfo.LastWriteTime)"
    Write-Log "  SHA256: $($hash.Hash)"
    Write-Log "  First 5 lines of script:"
    Get-Content -Path $scriptPath -TotalCount 5 | ForEach-Object { Write-Host "[swap-deployment]   $_" }
} else {
    Write-Log "  WARNING: Script path not found on target host"
}

Write-Log "Swapping deployment folders..."
Write-Log "  Current deployment: $DeploymentFolder"
Write-Log "  Backup folder: $BackupFolder"
Write-Log "  New files: $TempFolder"

try {
    # Remove old backup if it exists
    if (Test-Path $BackupFolder) {
        Write-Log "Removing old backup at $BackupFolder..."
        Remove-Item $BackupFolder -Recurse -Force
    }
    
    # Move current to backup
    if (Test-Path $DeploymentFolder) {
        Write-Log "Moving current deployment to backup..."
        Move-Item -Path $DeploymentFolder -Destination $BackupFolder -Force
    }
    
    # Move new to current
    Write-Log "Moving new deployment from $TempFolder to $DeploymentFolder..."
    Move-Item -Path $TempFolder -Destination $DeploymentFolder -Force
    
    Write-Log "Deployment swap complete"
    exit 0
} catch {
    Write-Error "Error swapping deployment folders: $_"
    exit 1
}
