param(
    [Parameter(Mandatory = $true)][string]$PhysicalPath,
    [string]$AppPoolName = 'pto-track',
    [string]$AppName = 'pto-track',
    [string]$AppPoolUser = '',
    [SecureString]$AppPoolPassword = ''
)

function Write-Log($m) { Write-Host "[finish-deploy] $m" }
function Convert-SecureStringToPlain {
    param([SecureString]$Secure)
    if (-not $Secure) { return '' }
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringUni($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

Write-Log "Loading WebAdministration or fallback modules..."
try { Import-Module WebAdministration -ErrorAction Stop; Write-Log 'Imported WebAdministration' } catch {
    try { Import-Module WebAdministration -UseWindowsPowerShell -ErrorAction Stop; Write-Log 'Imported WebAdministration via Windows PowerShell compatibility' } catch {
        try { Import-Module IISAdministration -ErrorAction Stop; Write-Log 'Imported IISAdministration' } catch { Write-Error 'WebAdministration/IISAdministration module not available.'; exit 1 }
    }
}

# Create app pool if missing
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    Write-Log "Creating app pool $AppPoolName"
    New-WebAppPool -Name $AppPoolName
    $pool = Get-Item "IIS:\AppPools\$AppPoolName"
    if ($AppPoolUser) { $pool.processModel.identityType = 3; $pool.processModel.userName = $AppPoolUser; $pool.processModel.password = (Convert-SecureStringToPlain $AppPoolPassword) }
    Set-Item "IIS:\AppPools\$AppPoolName" $pool
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
}
else {
    Write-Log "App pool $AppPoolName already exists. Ensuring identity is set if provided."
    $pool = Get-Item "IIS:\AppPools\$AppPoolName"
    if ($AppPoolUser -and ($pool.processModel.userName -ne $AppPoolUser)) {
        Write-Log "Updating app pool identity to $AppPoolUser"
        $pool.processModel.userName = $AppPoolUser
        $pool.processModel.password = (Convert-SecureStringToPlain $AppPoolPassword)
        $pool.processModel.identityType = 3
        Set-Item "IIS:\AppPools\$AppPoolName" $pool
    }
}

# Create or update application under Default Web Site
if (-not (Test-Path "IIS:\Sites\Default Web Site\$AppName")) {
    Write-Log "Creating application Default Web Site/$AppName -> $PhysicalPath"
    New-WebApplication -Site 'Default Web Site' -Name $AppName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName
}
else {
    Write-Log "Application exists; ensuring application pool and physical path"
    $app = Get-WebApplication -Site 'Default Web Site' -Name $AppName
    if ($app.applicationPool -ne $AppPoolName) { Set-ItemProperty "IIS:\Sites\Default Web Site\$AppName" -Name applicationPool -Value $AppPoolName }
    if ($app.physicalPath -ne $PhysicalPath) { Set-ItemProperty "IIS:\Sites\Default Web Site\$AppName" -Name physicalPath -Value $PhysicalPath }
}

# Configure authentication: anonymous off, windows auth on
Write-Log "Configuring authentication for application"
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name enabled -Value false
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name enabled -Value true

# Ensure Negotiate provider present first (fallback to NTLM)
Write-Log "Configuring Windows auth providers (Negotiate,NTLM)"
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -AtElement @{value = 'Negotiate' } -ErrorAction SilentlyContinue
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -AtElement @{value = 'NTLM' } -ErrorAction SilentlyContinue
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -Value @{value = 'Negotiate' }
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "Default Web Site/$AppName" -Name "." -Value @{value = 'NTLM' }

Write-Log "Recycling app pool"
Restart-WebAppPool $AppPoolName

Write-Log "Done."
