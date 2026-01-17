<#
.SYNOPSIS
Comprehensive system performance and health diagnostics for Windows.

.DESCRIPTION
Displays real-time system metrics including CPU, memory, disk, network, and process statistics.
Identifies if the machine is under stress and highlights concerning metrics.
Requires elevation (Administrator).

.EXAMPLE
.\Get-SystemPerformance.ps1

.NOTES
Run with elevation for full details.
#>

param(
    [switch]$HighFreq,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
if ($Verbose) { $VerbosePreference = "Continue" }

$sep80 = [string]::new('=', 80)
$sep80dash = [string]::new('-', 80)

# Check elevation
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "⚠️  WARNING: Not running with elevation. Some metrics will be unavailable." -ForegroundColor Yellow
}

Write-Host $sep80
Write-Host "SYSTEM PERFORMANCE & HEALTH DIAGNOSTICS" -ForegroundColor Cyan
Write-Host $sep80
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# CPU CONFIGURATION & UTILIZATION
# ============================================================================
Write-Host "CPU CONFIGURATION & UTILIZATION" -ForegroundColor Cyan
Write-Host $sep80dash

$cpu = Get-WmiObject Win32_Processor
Write-Host "Model:           $($cpu.Name)"
Write-Host "Cores:           $($cpu.NumberOfCores)"
Write-Host "Logical CPUs:    $($cpu.NumberOfLogicalProcessors)"
Write-Host "Max Clock Speed: $($cpu.MaxClockSpeed) MHz"
Write-Host "Current Clock:   $($cpu.CurrentClockSpeed) MHz"
Write-Host "L2 Cache:        $($cpu.L2CacheSize) KB"
Write-Host "L3 Cache:        $($cpu.L3CacheSize) KB"

# CPU Usage
$cpuUsage = Get-WmiObject Win32_PerfFormattedData_PerfOS_Processor | Where-Object { $_.Name -eq "_Total" } | Select-Object -ExpandProperty "PercentProcessorTime"
Write-Host "Current Usage:   $($cpuUsage)%" -ForegroundColor $(if ($cpuUsage -gt 80) { "Red" } elseif ($cpuUsage -gt 60) { "Yellow" } else { "Green" })
Write-Host ""

# ============================================================================
# MEMORY USAGE
# ============================================================================
Write-Host "MEMORY (RAM)" -ForegroundColor Cyan
Write-Host $sep80dash

$os = Get-WmiObject Win32_OperatingSystem
$totalMemMB = [math]::Round($os.TotalVisibleMemorySize / 1024, 2)
$freeMemMB = [math]::Round($os.FreePhysicalMemory / 1024, 2)
$usedMemMB = $totalMemMB - $freeMemMB
$memPercent = [math]::Round(($usedMemMB / $totalMemMB) * 100, 2)

Write-Host "Total Memory:    $totalMemMB MB"
Write-Host "Used:            $usedMemMB MB"
Write-Host "Free:            $freeMemMB MB"
Write-Host "% Used:          $memPercent%" -ForegroundColor $(if ($memPercent -gt 85) { "Red" } elseif ($memPercent -gt 70) { "Yellow" } else { "Green" })

$pageFileTotal = Get-WmiObject Win32_PageFile | Measure-Object -Property AllocatedBaseSize -Sum | Select-Object -ExpandProperty Sum
$pageFileFree = Get-WmiObject Win32_PageFile | Measure-Object -Property CurrentUsage -Sum | Select-Object -ExpandProperty Sum
if ($pageFileTotal -gt 0) {
    $pageFilePercent = [math]::Round(($pageFileFree / $pageFileTotal) * 100, 2)
    Write-Host "Page File % Used: $([math]::Round(100 - $pageFilePercent, 2))%" -ForegroundColor $(if ($pageFilePercent -lt 15) { "Red" } else { "Green" })
}
Write-Host ""

# ============================================================================
# DISK SPACE
# ============================================================================
Write-Host "DISK SPACE" -ForegroundColor Cyan
Write-Host $sep80dash

$disks = Get-WmiObject Win32_LogicalDisk -Filter "DriveType=3"
foreach ($disk in $disks) {
    $driveLetter = $disk.Name
    $totalGB = [math]::Round($disk.Size / 1GB, 2)
    $freeGB = [math]::Round($disk.FreeSpace / 1GB, 2)
    $usedGB = $totalGB - $freeGB
    $freePercent = [math]::Round(($freeGB / $totalGB) * 100, 2)
    
    $color = "Green"
    if ($freePercent -lt 5) { $color = "Red" }
    elseif ($freePercent -lt 10) { $color = "Yellow" }
    
    Write-Host "$driveLetter`: Total: $totalGB GB | Used: $usedGB GB | Free: $freeGB GB ($freePercent% free)" -ForegroundColor $color
}
Write-Host ""

# ============================================================================
# NETWORK INTERFACES
# ============================================================================
Write-Host "NETWORK INTERFACES" -ForegroundColor Cyan
Write-Host $sep80dash

$adapters = Get-WmiObject Win32_NetworkAdapterConfiguration -Filter "IPEnabled=TRUE"
foreach ($adapter in $adapters) {
    $mac = $adapter.MACAddress
    $ips = $adapter.IPAddress -join ", "
    $gateways = $adapter.DefaultIPGateway -join ", "
    
    Write-Host "Adapter: $($adapter.Description)"
    Write-Host "  MAC:        $mac"
    Write-Host "  IP Address: $ips"
    Write-Host "  Gateway:    $gateways"
}
Write-Host ""

# ============================================================================
# SYSTEM UPTIME
# ============================================================================
Write-Host "SYSTEM UPTIME" -ForegroundColor Cyan
Write-Host $sep80dash

$lastBootTime = Get-WmiObject Win32_OperatingSystem | Select-Object -ExpandProperty LastBootUpTime
$lastBootTime = [System.Management.ManagementDateTimeConverter]::ToDateTime($lastBootTime)
$uptime = (Get-Date) - $lastBootTime
$uptimeStr = "{0}d {1}h {2}m" -f $uptime.Days, $uptime.Hours, $uptime.Minutes

Write-Host "Last Boot:       $lastBootTime"
Write-Host "Uptime:          $uptimeStr"
Write-Host ""

# ============================================================================
# TOP PROCESSES BY MEMORY
# ============================================================================
Write-Host "TOP 10 PROCESSES BY MEMORY USAGE" -ForegroundColor Cyan
Write-Host $sep80dash

$topProcs = Get-Process | Sort-Object WorkingSet -Descending | Select-Object -First 10
$topProcs | ForEach-Object {
    $memMB = [math]::Round($_.WorkingSet / 1MB, 2)
    Write-Host "$($_.Name.PadRight(30)) $memMB MB"
}
Write-Host ""

# ============================================================================
# TOP PROCESSES BY CPU
# ============================================================================
Write-Host "TOP 10 PROCESSES BY CPU USAGE" -ForegroundColor Cyan
Write-Host $sep80dash

# Get CPU times from last sample
$procs = Get-Process | Where-Object { $_.UserProcessorTime -gt 0 } | Sort-Object UserProcessorTime -Descending | Select-Object -First 10
$procs | ForEach-Object {
    $cpuSeconds = [math]::Round($_.UserProcessorTime.TotalSeconds, 2)
    Write-Host "$($_.Name.PadRight(30)) $cpuSeconds sec"
}
Write-Host ""

# ============================================================================
# CRITICAL SERVICES STATUS
# ============================================================================
Write-Host "CRITICAL SERVICES STATUS" -ForegroundColor Cyan
Write-Host $sep80dash

$criticalServices = @("BITS", "Dhcp", "DnsClient", "Spooler", "W3SVC", "NTDS", "MSSQLSERVER", "WinRM")
foreach ($svc in $criticalServices) {
    $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($service) {
        $status = $service.Status
        $startType = $service.StartType
        $color = if ($status -eq "Running") { "Green" } else { "Yellow" }
        Write-Host "$($svc.PadRight(20)) Status: $($status.ToString().PadRight(10)) StartType: $startType" -ForegroundColor $color
    }
}
Write-Host ""

# ============================================================================
# EVENT LOG ERRORS (Last 24 Hours)
# ============================================================================
Write-Host "RECENT ERRORS & WARNINGS (Last 24 Hours)" -ForegroundColor Cyan
Write-Host $sep80dash

$yesterday = (Get-Date).AddDays(-1)
$errors = Get-EventLog -LogName System -After $yesterday -EntryType Error | Select-Object -First 10
$warnings = Get-EventLog -LogName System -After $yesterday -EntryType Warning | Select-Object -First 10

if ($errors) {
    Write-Host "System Errors:" -ForegroundColor Red
    $errors | ForEach-Object {
        Write-Host "  [$($_.TimeGenerated)] $($_.Source): $($_.Message.Substring(0, [math]::Min(70, $_.Message.Length)))" -ForegroundColor Red
    }
} else {
    Write-Host "No system errors in last 24 hours." -ForegroundColor Green
}

if ($warnings) {
    Write-Host ""
    Write-Host "System Warnings:" -ForegroundColor Yellow
    $warnings | ForEach-Object {
        Write-Host "  [$($_.TimeGenerated)] $($_.Source): $($_.Message.Substring(0, [math]::Min(70, $_.Message.Length)))" -ForegroundColor Yellow
    }
} else {
    Write-Host "No system warnings in last 24 hours." -ForegroundColor Green
}
Write-Host ""

# ============================================================================
# HEALTH SUMMARY
# ============================================================================
Write-Host $sep80
Write-Host "HEALTH SUMMARY" -ForegroundColor Cyan
Write-Host $sep80

$issues = @()

if ($cpuUsage -gt 80) { $issues += "⚠️  CPU usage is high ($cpuUsage%)" }
if ($memPercent -gt 85) { $issues += "⚠️  Memory usage is critically high ($memPercent%)" }
if ($memPercent -gt 70) { $issues += "⚠️  Memory usage is elevated ($memPercent%)" }

foreach ($disk in $disks) {
    $freePercent = [math]::Round(($disk.FreeSpace / $disk.Size) * 100, 2)
    if ($freePercent -lt 5) { $issues += "🔴 CRITICAL: $($disk.Name) disk nearly full ($freePercent% free)" }
    elseif ($freePercent -lt 10) { $issues += "⚠️  $($disk.Name) disk getting full ($freePercent% free)" }
}

if ($uptime.Days -gt 90) { $issues += "ℹ️  System hasn't been rebooted in $($uptime.Days) days (consider a reboot)" }

if ($issues.Count -eq 0) {
    Write-Host "✅ SYSTEM HEALTHY - No critical issues detected" -ForegroundColor Green
} else {
    Write-Host "❌ SYSTEM ISSUES DETECTED:" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host $issue
    }
}

Write-Host "=" * 80

