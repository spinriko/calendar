param(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentFolder,
    
    [Parameter(Mandatory = $true)]
    [string]$BackupFolder,
    
    [Parameter(Mandatory = $true)]
    [string]$TempFolder,
    
    [Parameter(Mandatory = $false)]
    [string]$ServiceAccount = "LocalSystem"
)

Write-Host "=== Pre-Deployment Target Preparation ==="

# Create deployment folders
Write-Host "Creating deployment folders..."
New-Item -ItemType Directory -Force $DeploymentFolder | Out-Null
New-Item -ItemType Directory -Force $BackupFolder | Out-Null
New-Item -ItemType Directory -Force $TempFolder | Out-Null
Write-Host "[OK] Folders created"

# Validate service account (skip permissions if LocalSystem)
if ($ServiceAccount -ne "LocalSystem") {
    Write-Host "Validating service account: $ServiceAccount"
    
    $accountName = $ServiceAccount.Split('\')[-1]
    $accountExists = $false
    
    try {
        $accountExists = [ADSI]::Exists("WinNT://./$(accountName)")
    }
    catch {
        Write-Host "[WARNING] Could not validate account existence: $_"
    }
    
    if (-not $accountExists) {
        Write-Host "[ERROR] Service account '$ServiceAccount' does not exist on this target"
        Write-Host "ACTION REQUIRED: Create the service account manually before deployment"
        Write-Host "Example: New-LocalUser -Name '$accountName' -Password (ConvertTo-SecureString 'password' -AsPlainText -Force)"
        throw "Service account validation failed: $ServiceAccount not found"
    }
    
    Write-Host "[OK] Service account '$ServiceAccount' verified"
    Write-Host "Setting NTFS permissions for $ServiceAccount on $DeploymentFolder"
    
    try {
        $acl = Get-Acl -Path $DeploymentFolder
        $accountName = $ServiceAccount.Split('\')[-1]
        
        # Remove any existing rule for this account
        $acl.Access | Where-Object { $_.IdentityReference -match $accountName } | ForEach-Object {
            $acl.RemoveAccessRule($_) | Out-Null
        }
        
        # Add ReadAndExecute rule
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $ServiceAccount,
            "ReadAndExecute",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl -Path $DeploymentFolder -AclObject $acl
        Write-Host "[OK] Permissions set for $ServiceAccount"
    }
    catch {
        Write-Host "[WARNING] Could not set permissions: $_"
        Write-Host "Ensure service account has Read and Execute rights on $DeploymentFolder"
    }
}
else {
    Write-Host "[OK] Using LocalSystem (built-in, no permissions needed)"
}

Write-Host "=== Pre-Deployment Preparation Complete ==="
