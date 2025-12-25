param(
    [Parameter(Mandatory = $true)][string]$EnvironmentName
)

function Write-Log($m) { Write-Host "[set-environment] $m" }

Write-Log "Setting ASPNETCORE_ENVIRONMENT to '$EnvironmentName' at Machine scope..."

try {
    [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", $EnvironmentName, "Machine")
    $current = [Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Machine")
    
    if ($current -eq $EnvironmentName) {
        Write-Log "✓ ASPNETCORE_ENVIRONMENT set to '$current'"
        exit 0
    } else {
        Write-Error "Failed to set ASPNETCORE_ENVIRONMENT (current='$current', expected='$EnvironmentName')"
        exit 1
    }
} catch {
    Write-Error "Error setting environment variable: $_"
    exit 1
}
