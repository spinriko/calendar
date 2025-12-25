param(
    [Parameter(Mandatory = $true)][string]$ConnectionString
)

function Write-Log($m) { Write-Host "[set-connection-string] $m" }

Write-Log "Setting ConnectionStrings__PtoTrackDbContext at Machine scope..."

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error "ConnectionString parameter is empty or null"
    exit 1
}

try {
    [Environment]::SetEnvironmentVariable("ConnectionStrings__PtoTrackDbContext", $ConnectionString, "Machine")
    $current = [Environment]::GetEnvironmentVariable("ConnectionStrings__PtoTrackDbContext", "Machine")
    
    if ($current) {
        Write-Log "ConnectionStrings__PtoTrackDbContext set (length: $($current.Length))"
        exit 0
    } else {
        Write-Error "Failed to set ConnectionStrings__PtoTrackDbContext"
        exit 1
    }
} catch {
    Write-Error "Error setting connection string: $_"
    exit 1
}
