param(
    [Parameter(Mandatory = $true)][string]$PhysicalPath,
    [string]$AppPoolName = 'pto-track',
    [string]$AppName = 'pto-track',
    [string]$AppPoolUser = '',
    [switch]$StopIISIfNeeded
)

function Write-Log($m) { Write-Host "[before-deploy] $m" }

Write-Log "Loading WebAdministration or fallback modules..."
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Log 'Imported WebAdministration'
}
catch {
    Write-Log 'WebAdministration not available in this session. Attempting Windows PowerShell compatibility or IISAdministration.'
    try {
        Import-Module WebAdministration -UseWindowsPowerShell -ErrorAction Stop
        Write-Log 'Imported WebAdministration via Windows PowerShell compatibility'
    }
    catch {
        Write-Log 'Attempting to import IISAdministration module from PSGallery'
        try {
            if (-not (Get-Module -ListAvailable -Name IISAdministration)) {
                Install-Module -Name IISAdministration -Force -Scope AllUsers -ErrorAction Stop
            }
            Import-Module IISAdministration -ErrorAction Stop
            Write-Log 'Imported IISAdministration'
        }
        catch {
            Write-Error 'Neither WebAdministration nor IISAdministration modules are available. Install IIS Management Scripts & Tools feature.'
            exit 1
        }
    }
}

if ($StopIISIfNeeded) {
    Write-Log "Stopping IIS (w3svc)"
    Stop-Service W3SVC -ErrorAction Stop
}

Write-Log "Checking existing application at Default Web Site/$AppName"
$appPath = "IIS:\Sites\Default Web Site\$AppName"
if (Test-Path $appPath) {
    try {
        $existingPhysical = (Get-WebApplication -Site 'Default Web Site' -Name $AppName).physicalPath
    }
    catch { $existingPhysical = $null }
    if ($existingPhysical -and ($existingPhysical -ne $PhysicalPath)) {
        Write-Log "Existing app found with different physical path ('$existingPhysical'). Removing application $AppName"
        Remove-WebApplication -Site 'Default Web Site' -Name $AppName -Confirm:$false
    }
    else {
        Write-Log "Application exists and points to same path; leaving in place."
    }
}
else { Write-Log "No existing application at Default Web Site/$AppName" }

# App pool handling: only remove if exists and identity mismatch
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    $pool = Get-Item "IIS:\AppPools\$AppPoolName"
    $currentUser = $pool.processModel.userName
    if ($AppPoolUser -and ($currentUser -ne $AppPoolUser)) {
        Write-Log "AppPool exists but identity mismatch (current='$currentUser', desired='$AppPoolUser'). Removing pool."
        Remove-WebAppPool $AppPoolName
    }
    else { Write-Log "AppPool exists and identity matches (or no desired user provided)." }
}

if ($StopIISIfNeeded) { Start-Service W3SVC }

Write-Log "Pre-deploy checks complete."
