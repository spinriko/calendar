<#
.SYNOPSIS
Stops all Azure DevOps Server services in the correct dependency order.

.DESCRIPTION
Stops Azure Pipelines Agent, IIS, and SQL Server services in reverse dependency order.
Services are stopped in the correct order to ensure clean shutdown.

.EXAMPLE
.\Stop-ADOServices.ps1
#>

param(
    [switch]$Force,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Define services in shutdown order (reverse of startup; dependents first)
$services = @(
    @{ Name = "vstsagent.localhost.DVO.QUANTUM-DVO"; DisplayName = "Azure Pipelines Agent (QUANTUM-DVO)" },
    @{ Name = "W3SVC"; DisplayName = "IIS World Wide Web Publishing Service" },
    @{ Name = "WAS"; DisplayName = "Windows Process Activation Service" },
    @{ Name = "WMSVC"; DisplayName = "IIS Web Management Service" },
    @{ Name = "SQLSERVERAGENT"; DisplayName = "SQL Server Agent (MSSQLSERVER)" },
    @{ Name = "MSSQLSERVER"; DisplayName = "SQL Server (MSSQLSERVER)" }
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stopping ADO Server Services" -ForegroundColor Cyan
if ($Force) { Write-Host "(FORCE mode: will forcefully stop)" -ForegroundColor Yellow }
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$stoppedCount = 0
$skippedCount = 0
$failedCount = 0

foreach ($service in $services) {
    $svc = Get-Service -Name $service.Name -ErrorAction SilentlyContinue
    
    if ($null -eq $svc) {
        Write-Host "⚠ SKIP: $($service.DisplayName) (service not found)" -ForegroundColor Yellow
        $skippedCount++
        continue
    }
    
    if ($svc.Status -eq "Stopped") {
        Write-Host "✓ OK: $($service.DisplayName) (already stopped)" -ForegroundColor Green
        continue
    }
    
    try {
        if ($Force) {
            Write-Host "→ Force stopping: $($service.DisplayName)..." -ForegroundColor Cyan
            Stop-Service -Name $service.Name -Force -ErrorAction Stop
        }
        else {
            # Check for dependent services
            $dependentServices = Get-Service -Name $service.Name | Select-Object -ExpandProperty DependentServices
            if ($dependentServices) {
                Write-Host "→ Stopping dependent services for: $($service.DisplayName)..." -ForegroundColor Cyan
                foreach ($dependent in $dependentServices) {
                    if ($dependent.Status -ne "Stopped") {
                        Write-Host "  → Stopping dependent: $($dependent.DisplayName)..." -ForegroundColor Cyan
                        Stop-Service -Name $dependent.Name -ErrorAction Stop
                    }
                }
            }
            Write-Host "→ Stopping: $($service.DisplayName)..." -ForegroundColor Cyan
            Stop-Service -Name $service.Name -ErrorAction Stop
        }
        
        # Give service a moment to shut down
        Start-Sleep -Milliseconds 500
        
        # Verify it stopped
        $svc.Refresh()
        if ($svc.Status -eq "Stopped") {
            Write-Host "✓ SUCCESS: $($service.DisplayName) stopped" -ForegroundColor Green
            $stoppedCount++
        }
        else {
            Write-Host "⚠ WARNING: $($service.DisplayName) did not stop cleanly (status: $($svc.Status))" -ForegroundColor Yellow
            if (-not $Force) {
                Write-Host "  → Retry with -Force flag if needed" -ForegroundColor Yellow
            }
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
Write-Host "Stopped:  $stoppedCount" -ForegroundColor Green
Write-Host "Skipped:  $skippedCount" -ForegroundColor Yellow
if ($failedCount -gt 0) {
    Write-Host "Failed:   $failedCount" -ForegroundColor Red
}
Write-Host ""

if ($failedCount -gt 0) {
    exit 1
}
