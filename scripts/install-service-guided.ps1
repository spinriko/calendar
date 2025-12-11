<#
  Guided, non-automated helper to show the commands required to install the published
  self-contained app as a Windows Service. The script prints the commands you should
  run and does NOT execute them by default. Pass `-Execute` to actually run them.

  WARNING: This is intentionally conservative. By default it only prints recommendations.
#>

param(
    [string]$PublishPath = 'C:\inetpub\pto-track',
    [string]$ServiceName = 'PTOTrack',
    [switch]$Execute
)

function Show-Commands {
    param($PublishPath, $ServiceName)

    Write-Host "Recommended commands to run (please inspect each line):`n"

    Write-Host "# 1) (Optional) confirm files in publish folder"
    Write-Host "Get-ChildItem -Path '$PublishPath' -File"
    Write-Host "`n# 2) Create service using New-Service"
    Write-Host "New-Service -Name '$ServiceName' -BinaryPathName '$PublishPath\pto.track.exe' -DisplayName 'PTO Track' -StartupType Automatic"
    Write-Host "`n# OR using sc.exe (note sc spacing is significant):"
    Write-Host "sc create \"$ServiceName\" binPath= \"$PublishPath\\pto.track.exe\" DisplayName= \"PTO Track\" start= auto"
    Write-Host "`n# 3) Start the service"
    Write-Host "Start-Service -Name '$ServiceName'"
    Write-Host "`n# 4) Check status"
    Write-Host "Get-Service -Name '$ServiceName' | Format-List Status, DisplayName, ServiceType"
    Write-Host "`n# 5) Troubleshoot: view EventLog entries from the app or check stdout logs (as configured)"
}

if ($Execute) {
    Write-Host "Executing the recommended commands (intentional action requested)" -ForegroundColor Yellow

    # Validate the publish folder exists and contains the exe
    if (-not (Test-Path -Path $PublishPath)) {
        Write-Error "Publish path not found: $PublishPath"; exit 2
    }

    $exe = Join-Path $PublishPath 'pto.track.exe'
    if (-not (Test-Path -Path $exe)) {
        Write-Error "Executable not found at expected location: $exe"; exit 2
    }

    # Create the service (New-Service is clearer than sc syntax and avoids spacing pitfalls)
    try {
        if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
            Write-Host "Service '$ServiceName' already exists. Skipping creation."
        }
        else {
            New-Service -Name $ServiceName -BinaryPathName "`"$exe`"" -DisplayName 'PTO Track' -StartupType Automatic
            Write-Host "Service created: $ServiceName"
        }

        Start-Service -Name $ServiceName -ErrorAction Stop
        Write-Host "Service started. Run Get-Service -Name $ServiceName to check status." -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to create/start service: $_"
        exit 1
    }
}
else {
    Show-Commands -PublishPath $PublishPath -ServiceName $ServiceName
    Write-Host "`nNote: To actually run these commands, re-run with -Execute." -ForegroundColor Cyan
}
