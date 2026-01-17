param(
    [string]$FeatureName = 'Web-Server'
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    Write-Log "IIS prerequisite check started"
    
    Write-Host "Checking for WebAdministration module"
    
    # Try to import WebAdministration module
    Import-Module WebAdministration -ErrorAction Stop
    
    Write-Log "WebAdministration module available"
    Write-Host "IIS prerequisites verified successfully"
    
    exit 0
}
catch {
    Write-Log "ERROR: Failed to import WebAdministration module: $_"
    Write-Host "##vso[task.logissue type=error]WebAdministration module not available. Ensure IIS is installed."
    exit 1
}
