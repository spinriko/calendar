param(
    [Parameter(Mandatory = $true)][string]$DeploymentFolder,
    [Parameter(Mandatory = $true)][string]$BackupFolder,
    [Parameter(Mandatory = $true)][string]$TempFolder
)

function Write-Log($m) { Write-Host "[swap-deployment] $m" }

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
    
    Write-Log "✓ Deployment swap complete"
    exit 0
} catch {
    Write-Error "Error swapping deployment folders: $_"
    exit 1
}
