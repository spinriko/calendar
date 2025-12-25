param(
    [Parameter(Mandatory = $true)][string]$ServiceName
)

function Write-Log($m) { Write-Host "[stop-service] $m" }

Write-Log "Checking for service '$ServiceName'..."

try {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Log "Service '$ServiceName' exists. Stopping and removing..."
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName
        Write-Log "✓ Service '$ServiceName' was stopped and removed."
        exit 0
    }
    else {
        Write-Log "Service '$ServiceName' does not exist (nothing to remove)."
        exit 0
    }
}
catch {
    Write-Error "Error stopping service '$ServiceName': $_"
    exit 1
}
