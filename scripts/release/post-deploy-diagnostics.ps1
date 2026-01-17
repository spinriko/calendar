param(
    [Parameter(Mandatory = $true)][string]$WebsiteName,
    [Parameter(Mandatory = $true)][string]$DeploymentFolder
)

function Write-Log($m) { Write-Host "[diagnostics] $m" }

Write-Log "Starting post-deployment diagnostics..."

# 1. Export ANCM/.NET errors from Event Log
Write-Log "=== Exporting ANCM/.NET Runtime Events ==="
$providers = @("ASP.NET Core Module V2", "AspNetCoreModuleV2", ".NET Runtime")
$startTime = (Get-Date).AddHours(-2)
foreach ($p in $providers) {
    Write-Host "=== Events for provider: ${p} (since $startTime) ==="
    try {
        Get-WinEvent -FilterHashtable @{ ProviderName = $p; StartTime = $startTime } -ErrorAction Stop |
            Select-Object TimeCreated, Id, LevelDisplayName, ProviderName, Message |
            Sort-Object TimeCreated |
            ForEach-Object {
                "{0:yyyy-MM-dd HH:mm:ss} [{1}] ({2}) {3}`n{4}" -f $_.TimeCreated, $_.LevelDisplayName, $_.Id, $_.ProviderName, $_.Message
            }
    } catch {
        Write-Host "No events or access denied for ${p}: $($_.Exception.Message)"
    }
}

# 2. Dump IIS Authentication Settings
Write-Log "=== IIS Authentication Settings for site: $WebsiteName ==="
try {
    Import-Module WebAdministration -ErrorAction Stop
    $winAuth = Get-WebConfigurationSection -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $WebsiteName -Filter 'system.webServer/security/authentication/windowsAuthentication'
    $anonAuth = Get-WebConfigurationSection -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $WebsiteName -Filter 'system.webServer/security/authentication/anonymousAuthentication'
    $winEnabled = $winAuth.Attributes['enabled'].Value
    $anonEnabled = $anonAuth.Attributes['enabled'].Value
    Write-Host "WindowsAuthentication enabled: $winEnabled"
    Write-Host "AnonymousAuthentication enabled: $anonEnabled"
} catch {
    Write-Host "Failed to read IIS auth settings: $($_.Exception.Message)"
}

# 3. Tail IIS Access Logs
Write-Log "=== Tailing IIS Access Logs ==="
try {
    $site = Get-Website -Name $WebsiteName
    $siteId = $site.Id
    $logDir = Join-Path "C:\inetpub\logs\LogFiles" ("W3SVC{0}" -f $siteId)
    Write-Host "IIS logs dir: $logDir"
    if (Test-Path $logDir) {
        $latest = Get-ChildItem $logDir -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latest) {
            Write-Host "Latest IIS log: $($latest.FullName)"
            Get-Content $latest.FullName -Tail 200
        } else {
            Write-Host "No log files found in $logDir"
        }
    } else {
        Write-Host "IIS logs folder not found for site '$WebsiteName' at $logDir"
    }
} catch {
    Write-Host "Failed to tail IIS logs for site '$WebsiteName': $($_.Exception.Message)"
}

# 4. Dump Application stdout Logs
Write-Log "=== Inspecting Application stdout Logs ==="
$logDir = Join-Path $DeploymentFolder "logs"
Write-Host "stdout logs directory: $logDir"
if (Test-Path $logDir) {
    Get-ChildItem -Path $logDir -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object {
        Write-Host "Latest log: $($_.FullName)"
        Get-Content -Path $_.FullName -Tail 200
    }
} else {
    Write-Host "No logs directory found at $logDir"
}

Write-Log "Diagnostics complete"
exit 0
