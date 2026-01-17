<#
.SYNOPSIS
Sets Azure DevOps Server–related services to Manual startup.

.DESCRIPTION
Updates the startup type for Azure Pipelines Agent, IIS, and SQL Server
services so they do not auto-start and consume CPU/RAM when not developing.

.EXAMPLE
.\Set-ADOServicesToManual.ps1
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Same service list as Stop-ADOServices.ps1
$services = @(
    @{ Name = "vstsagent.localhost.DVO.QUANTUM-DVO"; DisplayName = "Azure Pipelines Agent (QUANTUM-DVO)" },
    @{ Name = "W3SVC"; DisplayName = "IIS World Wide Web Publishing Service" },
    @{ Name = "WAS"; DisplayName = "Windows Process Activation Service" },
    @{ Name = "WMSVC"; DisplayName = "IIS Web Management Service" },
    @{ Name = "SQLSERVERAGENT"; DisplayName = "SQL Server Agent (MSSQLSERVER)" },
    @{ Name = "MSSQLSERVER"; DisplayName = "SQL Server (MSSQLSERVER)" }
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setting ADO Server Services to MANUAL" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$updatedCount = 0
$skippedCount = 0
$failedCount = 0

foreach ($service in $services) {
    $svc = Get-Service -Name $service.Name -ErrorAction SilentlyContinue

    if ($null -eq $svc) {
        Write-Host "⚠ SKIP: $($service.DisplayName) (service not found)" -ForegroundColor Yellow
        $skippedCount++
        continue
    }

    try {
        Write-Host "→ Setting startup type to Manual: $($service.DisplayName)..." -ForegroundColor Cyan
        Set-Service -Name $service.Name -StartupType Manual -ErrorAction Stop

        Write-Host "✓ SUCCESS: $($service.DisplayName) set to Manual" -ForegroundColor Green
        $updatedCount++
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
Write-Host "Updated:  $updatedCount" -ForegroundColor Green
Write-Host "Skipped:  $skippedCount" -ForegroundColor Yellow
if ($failedCount -gt 0) {
    Write-Host "Failed:   $failedCount" -ForegroundColor Red
}
Write-Host ""

if ($failedCount -gt 0) {
    exit 1
}
