<#
.SYNOPSIS
Starts all Azure DevOps Server services in the correct dependency order.

.DESCRIPTION
Starts IIS, SQL Server, and Azure Pipelines Agent services needed for ADO Server.
Services are started in dependency order to ensure proper initialization.

.EXAMPLE
.\Start-ADOServices.ps1
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Define services in startup order (dependencies first)
$services = @(
    @{ Name = "MSSQLSERVER"; DisplayName = "SQL Server (MSSQLSERVER)" },
    @{ Name = "SQLSERVERAGENT"; DisplayName = "SQL Server Agent (MSSQLSERVER)" },
    @{ Name = "WAS"; DisplayName = "Windows Process Activation Service" },
    @{ Name = "W3SVC"; DisplayName = "IIS World Wide Web Publishing Service" },
    @{ Name = "WMSVC"; DisplayName = "IIS Web Management Service" },
    @{ Name = "TeamFoundationSshService"; DispalayName = "Azure DevOps Ssh Service" }
    @{ Name = "vstsagent.quantum.DVO.QUANTUM"; DisplayName = "Azure Pipelines Agent (quantum.DVO.QUANTUM)" }
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting ADO Server Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$startedCount = 0
$skippedCount = 0
$failedCount = 0

foreach ($service in $services) {
    $svc = Get-Service -Name $service.Name -ErrorAction SilentlyContinue
    
    if ($null -eq $svc) {
        Write-Host "⚠ SKIP: $($service.DisplayName) (service not found)" -ForegroundColor Yellow
        $skippedCount++
        continue
    }
    
    if ($svc.Status -eq "Running") {
        Write-Host "✓ OK: $($service.DisplayName) (already running)" -ForegroundColor Green
        continue
    }
    
    try {
        Write-Host "→ Starting: $($service.DisplayName)..." -ForegroundColor Cyan
        Start-Service -Name $service.Name -ErrorAction Stop
        
        # Give service a moment to stabilize
        Start-Sleep -Milliseconds 500
        
        # Verify it started
        $svc.Refresh()
        if ($svc.Status -eq "Running") {
            Write-Host "✓ SUCCESS: $($service.DisplayName) started" -ForegroundColor Green
            $startedCount++
        }
        else {
            Write-Host "✗ FAILED: $($service.DisplayName) did not start (status: $($svc.Status))" -ForegroundColor Red
            $failedCount++
        }
    }
    catch {
        Write-Host "✗ ERROR: $($service.DisplayName) - $_" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Started:  $startedCount" -ForegroundColor Green
Write-Host "Skipped:  $skippedCount" -ForegroundColor Yellow
if ($failedCount -gt 0) {
    Write-Host "Failed:   $failedCount" -ForegroundColor Red
}
Write-Host ""

if ($failedCount -gt 0) {
    exit 1
}
