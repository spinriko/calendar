<#
.SYNOPSIS
  Evaluate code metrics JSON and fail CI if thresholds are exceeded.

.DESCRIPTION
  Reads a metrics JSON (default: artifacts/metrics/metrics.json) produced by the
  `metrics-runner` tool and compares aggregated/derived metrics to provided
  thresholds. Exits with code 1 when thresholds are violated, otherwise 0.
#>

param(
    [string]$Path = "artifacts/metrics/metrics.json",
    [int]$MaxTotalCyclomatic = 0,
    [double]$MinAvgMaintainabilityIndex = 0.0,
    [switch]$Verbose
)

if (-not (Test-Path -Path $Path)) {
    Write-Error "Metrics file not found: $Path"
    exit 2
}

try {
    $raw = Get-Content -Raw -Path $Path
    $metrics = $raw | ConvertFrom-Json
}
catch {
    Write-Error "Failed to read or parse metrics JSON: $_"
    exit 2
}

# Determine total cyclomatic complexity
if ($null -ne $metrics.aggregatedCyclomatic) {
    $totalCyclomatic = [int]$metrics.aggregatedCyclomatic
}
elseif ($null -ne $metrics.totalCyclomatic) {
    $totalCyclomatic = [int]$metrics.totalCyclomatic
}
elseif ($metrics.files) {
    $totalCyclomatic = ($metrics.files | Measure-Object -Property cyclomatic -Sum).Sum
}
else {
    $totalCyclomatic = 0
}

# Determine average Maintainability Index (MI)
$miValues = @()
if ($metrics.files) {
    foreach ($f in $metrics.files) {
        if ($null -ne $f.maintainabilityIndex) { $miValues += [double]$f.maintainabilityIndex }
    }
}

if ($miValues.Count -gt 0) {
    $avgMI = [math]::Round(($miValues | Measure-Object -Sum).Sum / $miValues.Count, 2)
}
else {
    $avgMI = 0.0
}

if ($Verbose) {
    Write-Host "Metrics file: $Path"
    Write-Host "TotalCyclomatic: $totalCyclomatic"
    Write-Host "Average MaintainabilityIndex: $avgMI"
}

$failed = $false
if ($MaxTotalCyclomatic -gt 0 -and $totalCyclomatic -gt $MaxTotalCyclomatic) {
    Write-Host "FAIL: Total cyclomatic complexity ($totalCyclomatic) exceeds threshold ($MaxTotalCyclomatic)." -ForegroundColor Yellow
    $failed = $true
}

if ($MinAvgMaintainabilityIndex -gt 0 -and $avgMI -lt $MinAvgMaintainabilityIndex) {
    Write-Host "FAIL: Average Maintainability Index ($avgMI) is below threshold ($MinAvgMaintainabilityIndex)." -ForegroundColor Yellow
    $failed = $true
}

if ($failed) { exit 1 } else { Write-Host "OK: Metrics are within thresholds." -ForegroundColor Green; exit 0 }
