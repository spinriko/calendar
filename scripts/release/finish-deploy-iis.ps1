param(
    [Parameter(Mandatory = $true)][string]$PhysicalPath,
    [string]$WebSiteName = 'Default Web Site',
    [string]$AppPoolName = 'pto-track',
    [string]$AppName = 'pto-track',
    [string]$AppPoolUser = '',
    [string]$AppPoolPassword = '',
    [string]$Environment = '',
    [switch]$CreateSiteIfMissing = $false
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
    if ($AppPoolUser) { $pool.processModel.identityType = 3; $pool.processModel.userName = $AppPoolUser; $pool.processModel.password = $AppPoolPassword }
    Set-Item "IIS:\AppPools\$AppPoolName" $pool
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
}
else {
    Write-Log "App pool $AppPoolName already exists. Ensuring identity is set if provided."
    $pool = Get-Item "IIS:\AppPools\$AppPoolName"
    if ($AppPoolUser -and ($pool.processModel.userName -ne $AppPoolUser)) {
        Write-Log "Updating app pool identity to $AppPoolUser"
        $pool.processModel.userName = $AppPoolUser
        $pool.processModel.password = $AppPoolPassword
        $pool.processModel.identityType = 3
        Set-Item "IIS:\AppPools\$AppPoolName" $pool
    }
}

# Set ASPNETCORE_ENVIRONMENT at app pool level (avoid web.config env block for inprocess self-contained)
if ($Environment) {
    try {
        $envVars = $pool.environmentVariables
        if ($envVars["ASPNETCORE_ENVIRONMENT"]) {
            Write-Log "Updating ASPNETCORE_ENVIRONMENT on app pool to '$Environment'"
            $envVars["ASPNETCORE_ENVIRONMENT"].Value = $Environment
        }
        else {
            Write-Log "Adding ASPNETCORE_ENVIRONMENT on app pool: '$Environment'"
            $envVars.Add("ASPNETCORE_ENVIRONMENT", $Environment) | Out-Null
        }
        Set-Item "IIS:\AppPools\$AppPoolName" $pool
    }
    catch {
        Write-Log "[WARNING] Failed to set ASPNETCORE_ENVIRONMENT on app pool: $_"
    }
}

# Ensure web site exists
if (-not (Test-Path "IIS:\Sites\$WebSiteName")) {
    if ($CreateSiteIfMissing) {
        Write-Log "Creating web site $WebSiteName"
        New-WebSite -Name $WebSiteName -PhysicalPath 'C:\inetpub\wwwroot' -Force | Out-Null
    }
    else {
        Write-Error "Web site '$WebSiteName' not found. Pass -CreateSiteIfMissing to create it or set the correct name."
        exit 1
    }
}

# Create or update application under web site
if (-not (Test-Path "IIS:\Sites\$WebSiteName\$AppName")) {
    Write-Log "Creating application $WebSiteName/$AppName -> $PhysicalPath"
    New-WebApplication -Site $WebSiteName -Name $AppName -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName
}
else {
    Write-Log "Application exists; ensuring application pool and physical path"
    $app = Get-WebApplication -Site $WebSiteName -Name $AppName
    if ($app.applicationPool -ne $AppPoolName) { Set-ItemProperty "IIS:\Sites\$WebSiteName\$AppName" -Name applicationPool -Value $AppPoolName }
    if ($app.physicalPath -ne $PhysicalPath) { Set-ItemProperty "IIS:\Sites\$WebSiteName\$AppName" -Name physicalPath -Value $PhysicalPath }
}

# Configure authentication: anonymous off, windows auth on
Write-Log "Configuring authentication for application"
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name enabled -Value false
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name enabled -Value true

# Ensure Negotiate provider present first (fallback to NTLM)
Write-Log "Configuring Windows auth providers (Negotiate,NTLM)"
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name "." -AtElement @{value = 'Negotiate' } -ErrorAction SilentlyContinue
Remove-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name "." -AtElement @{value = 'NTLM' } -ErrorAction SilentlyContinue
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name "." -Value @{value = 'Negotiate' }
Add-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication/providers" -PSPath 'IIS:\' -Location "$WebSiteName/$AppName" -Name "." -Value @{value = 'NTLM' }

Write-Log "Starting app pool (if stopped) and verifying"
$pool = Get-Item "IIS:\AppPools\$AppPoolName"
if ($pool.State -ne 'Started') {
    Write-Log "App pool is $($pool.State). Starting..."
    Start-WebAppPool $AppPoolName
    Start-Sleep -Seconds 2
    $pool = Get-Item "IIS:\AppPools\$AppPoolName"
    if ($pool.State -ne 'Started') {
        Write-Error "App pool failed to start. State: $($pool.State). Check identity credentials and 'Log on as a service' rights for $AppPoolUser"
        exit 1
    }
    Write-Log "App pool started successfully"
}
else {
    Write-Log "App pool already running. Recycling..."
    Restart-WebAppPool $AppPoolName
}

Write-Log "Done."
