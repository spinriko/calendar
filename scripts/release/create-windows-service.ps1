param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath,

    [Parameter(Mandatory = $true)]
    [string]$ServiceAccountName,

    [Parameter(Mandatory = $true)]
    [SecureString]$ServiceAccountPassword,

    [Parameter(Mandatory = $true)]
    [string]$ServiceDescription
)

function Write-Log {
    param([string]$Message)
    Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    Write-Log "Creating Windows Service '$ServiceName'..."
    
    if (-not $ServiceName) {
        throw "ServiceName parameter is required"
    }
    
    if (-not $ExecutablePath) {
        throw "ExecutablePath parameter is required"
    }
    
    if (-not $ServiceAccountName) {
        throw "ServiceAccountName parameter is required"
    }
    
    if (-not $ServiceAccountPassword) {
        throw "ServiceAccountPassword parameter is required"
    }
    
    if (-not $ServiceDescription) {
        throw "ServiceDescription parameter is required"
    }
    
    # Verify the executable exists
    if (-not (Test-Path $ExecutablePath)) {
        throw "Executable not found at: $ExecutablePath"
    }
    
    Write-Log "Creating service with binPath: $ExecutablePath"
    Write-Log "Service account: $ServiceAccountName"
    
    # Create service
    sc.exe create $ServiceName binPath= "$ExecutablePath" start= auto obj= "$ServiceAccountName" password= "$ServiceAccountPassword"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Log "Service created successfully"
    }
    elseif ($LASTEXITCODE -eq 1073) {
        Write-Log "Service already exists. Updating service..."
        sc.exe config $ServiceName binPath= "$ExecutablePath" start= auto obj= "$ServiceAccountName" password= "$ServiceAccountPassword"
    }
    else {
        throw "Service creation failed with exit code: $LASTEXITCODE"
    }
    
    # Set service description
    Write-Log "Setting service description: $ServiceDescription"
    sc.exe description $ServiceName "$ServiceDescription"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set service description. Exit code: $LASTEXITCODE"
    }
    
    # Start service
    Write-Log "Starting service..."
    Start-Service -Name $ServiceName -ErrorAction Stop
    
    Write-Log "Service '$ServiceName' started successfully"
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    exit 1
}
