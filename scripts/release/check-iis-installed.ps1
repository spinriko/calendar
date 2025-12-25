param(
    [string]$FeatureName = 'Web-Server'
)

function Write-Log($m) { Write-Host "[check-iis] $m" }

$feature = Get-WindowsFeature -Name $FeatureName -ErrorAction SilentlyContinue

if (-not $feature) {
    Write-Log "ERROR: Feature '$FeatureName' not found on this system."
    exit 1
}

if (-not $feature.Installed) {
    Write-Error "IIS feature '$FeatureName' is not installed. Deployment cannot continue."
    exit 1
}

Write-Log "✓ IIS feature '$FeatureName' is installed."
exit 0
