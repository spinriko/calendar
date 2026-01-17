param(
    [Parameter(Mandatory = $true)][string]$TempFolder,
    [Parameter(Mandatory = $true)][string]$BackupFolder,
    [Parameter(Mandatory = $true)][string]$ArchiveRoot,
    [Parameter(Mandatory = $true)][int]$RetentionDays,
    [Parameter(Mandatory = $false)][bool]$SkipCleanup = $false
)

function Write-Log($m) { Write-Host "[cleanup] $m" }

if ($SkipCleanup) {
    Write-Log "Skipping cleanup because SkipCleanup is true"
    exit 0
}

Write-Log "Starting post-deployment cleanup..."

# Remove temp folder
if (Test-Path $TempFolder) {
    Write-Log "Removing temp folder: $TempFolder"
    Remove-Item -Path $TempFolder -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Log "No temp folder at $TempFolder"
}

# Ensure archive root exists
if (-not (Test-Path $ArchiveRoot)) {
    Write-Log "Creating archive root: $ArchiveRoot"
    New-Item -ItemType Directory -Force -Path $ArchiveRoot | Out-Null
}

# Archive and prune backups
if (Test-Path $BackupFolder) {
    $ts = (Get-Date -Format 'yyyyMMddHHmmss')
    $dest = Join-Path $ArchiveRoot ("backup_$ts")
    Write-Log "Archiving backup $BackupFolder -> $dest"
    Move-Item -Path $BackupFolder -Destination $dest -Force

    # Prune older archives
    Write-Log "Pruning archives older than $RetentionDays days..."
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem $ArchiveRoot -Directory | Where-Object { $_.LastWriteTime -lt $cutoffDate } | ForEach-Object {
        Write-Log "Pruning archive: $($_.FullName)"
        Remove-Item -Recurse -Force $_.FullName
    }
} else {
    Write-Log "No backup folder found at $BackupFolder"
}

Write-Log "Cleanup complete"
exit 0
