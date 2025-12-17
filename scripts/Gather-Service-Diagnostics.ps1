<#
Gather-Service-Diagnostics.ps1
Run elevated on the server to collect diagnostic files for a Windows service that fails to start.
Usage:
  .\Gather-Service-Diagnostics.ps1 -ServiceName PTOTrack -PublishPath 'C:\standalone\pto-track'
#>
param(
    [string]$ServiceName = 'PTOTrack',
    [string]$PublishPath = 'C:\standalone\pto-track',
    [string]$OutputDir = "C:\temp\pto-diagnostics-$(Get-Date -Format yyyyMMdd-HHmmss)"
)

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Run this script as Administrator."
    exit 1
}

New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
Write-Output "Writing diagnostics to $OutputDir"

# Service info
Get-Service -Name $ServiceName -ErrorAction SilentlyContinue | Format-List * | Out-File -FilePath (Join-Path $OutputDir 'service-status.txt') -Width 200
sc.exe qc $ServiceName | Out-File (Join-Path $OutputDir 'service-config.txt')
sc.exe queryex $ServiceName | Out-File (Join-Path $OutputDir 'service-queryex.txt')

# Recent SCM errors and service-related events (last 24h)
$start = (Get-Date).AddDays(-1)
Get-WinEvent -FilterHashtable @{LogName = 'System'; ProviderName = 'Service Control Manager'; StartTime = $start } -MaxEvents 200 | Select-Object TimeCreated, Id, LevelDisplayName, Message | Format-List | Out-File (Join-Path $OutputDir 'scm-events.txt') -Width 200

# Application log for the service source and recent errors
Get-WinEvent -FilterHashtable @{LogName = 'Application'; StartTime = $start } -MaxEvents 500 |
Where-Object { $_.ProviderName -eq $ServiceName -or $_.Message -match $ServiceName } |
Select-Object TimeCreated, Id, LevelDisplayName, ProviderName, Message | Format-List | Out-File (Join-Path $OutputDir 'application-service-events.txt') -Width 400

# Also capture recent application errors (all providers)
Get-WinEvent -FilterHashtable @{LogName = 'Application'; Level = 2; StartTime = $start } -MaxEvents 200 | Select-Object TimeCreated, Id, LevelDisplayName, ProviderName, Message | Format-List | Out-File (Join-Path $OutputDir 'application-errors.txt') -Width 400

# Publish folder listing and file ACLs
if (Test-Path $PublishPath) {
    Get-ChildItem -Path $PublishPath -Recurse | Select-Object FullName, Length, LastWriteTime | Out-File (Join-Path $OutputDir 'publish-files.txt') -Width 400
    try { Get-Acl -Path $PublishPath | Format-List | Out-File (Join-Path $OutputDir 'publish-acl.txt') -Width 400 } catch { "Get-Acl failed: $_" | Out-File (Join-Path $OutputDir 'publish-acl.txt') }
    # exe ACL
    $exe = Join-Path $PublishPath 'pto.track.exe'
    if (Test-Path $exe) { Get-Acl -Path $exe | Format-List | Out-File (Join-Path $OutputDir 'exe-acl.txt') -Width 400 }
}
else {
    "Publish path not found: $PublishPath" | Out-File (Join-Path $OutputDir 'publish-files.txt')
}

# Registry: Event Log source key ACL
$regPath = 'HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\' + $ServiceName
try {
    if (Test-Path $regPath) {
        Get-ItemProperty -Path $regPath | Out-File (Join-Path $OutputDir 'eventlog-source-registry.txt') -Width 400
        try { Get-Acl -Path $regPath | Format-List | Out-File (Join-Path $OutputDir 'eventlog-source-registry-acl.txt') -Width 400 } catch { "Get-Acl failed: $_" | Out-File (Join-Path $OutputDir 'eventlog-source-registry-acl.txt') }
    }
    else {
        "Registry key not found: $regPath" | Out-File (Join-Path $OutputDir 'eventlog-source-registry.txt')
    }
}
catch { "Registry access failed: $_" | Out-File (Join-Path $OutputDir 'eventlog-source-registry.txt') }

# Local security: check Log on as a service (SeServiceLogonRight)
$secOut = Join-Path $OutputDir 'secpol-export.cfg'
secedit /export /cfg $secOut 2>$null
if (Test-Path $secOut) {
    Select-String -Path $secOut -Pattern 'SeServiceLogonRight' | Out-File (Join-Path $OutputDir 'logon-as-service-rights.txt')
}
else {
    "secedit export failed or not available" | Out-File (Join-Path $OutputDir 'logon-as-service-rights.txt')
}

# Attempt to start the service and capture immediate SCM events
Try {
    Start-Service -Name $ServiceName -ErrorAction Stop
    Start-Sleep -Seconds 3
    sc.exe queryex $ServiceName | Out-File (Join-Path $OutputDir 'service-query-after-start.txt')
    Get-WinEvent -FilterHashtable @{LogName = 'System'; ProviderName = 'Service Control Manager'; StartTime = (Get-Date).AddMinutes(-5) } -MaxEvents 200 | Select-Object TimeCreated, Id, LevelDisplayName, Message | Format-List | Out-File (Join-Path $OutputDir 'scm-events-after-start.txt') -Width 400
}
catch {
    "Start-Service failed: $_" | Out-File (Join-Path $OutputDir 'service-start-failed.txt')
    sc.exe queryex $ServiceName | Out-File (Join-Path $OutputDir 'service-query-after-start.txt')
    Get-WinEvent -FilterHashtable @{LogName = 'System'; ProviderName = 'Service Control Manager'; StartTime = (Get-Date).AddMinutes(-5) } -MaxEvents 200 | Select-Object TimeCreated, Id, LevelDisplayName, Message | Format-List | Out-File (Join-Path $OutputDir 'scm-events-after-start.txt') -Width 400
}

"Collected diagnostics to: $OutputDir"
Write-Output "Compressing results to ${OutputDir}.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($OutputDir, "$OutputDir.zip")
Write-Output "Done: $OutputDir.zip"