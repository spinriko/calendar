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

    $accountResolved = $true
    try {
        # Resolve domain or local accounts to SID; throws if not resolvable
        $null = (New-Object System.Security.Principal.NTAccount($ServiceAccount)).Translate([System.Security.Principal.SecurityIdentifier])
        Write-Host "[OK] Account resolved via NTAccount"
    }
    catch {
        Write-Host "[WARNING] Could not resolve account '$ServiceAccount': $_"
        $accountResolved = $false
    }

    if (-not $accountResolved) {
        Write-Host "[WARNING] Proceeding to set permissions for '$ServiceAccount' anyway; Windows may resolve at apply time."
    }

    Write-Host "Setting NTFS permissions for $ServiceAccount on $DeploymentFolder"

    try {
        $acl = Get-Acl -Path $DeploymentFolder

        # Remove any existing rule for this account
        $acl.Access | Where-Object { $_.IdentityReference.Value -ieq $ServiceAccount } | ForEach-Object {
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

    # Ensure logs directory exists and grant Modify permission for app to write stdout logs
    try {
        $logsDir = Join-Path $DeploymentFolder 'logs'
        New-Item -ItemType Directory -Force $logsDir | Out-Null
        $aclLogs = Get-Acl -Path $logsDir
        $ruleLogs = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $ServiceAccount,
            "Modify",
            "ContainerInherit,ObjectInherit",
            "None",
            "Allow"
        )
        $aclLogs.SetAccessRule($ruleLogs)
        Set-Acl -Path $logsDir -AclObject $aclLogs
        Write-Host "[OK] Modify permission granted on logs directory for $ServiceAccount"
    }
    catch {
        Write-Host "[WARNING] Could not set logs directory permissions: $_"
        Write-Host "Ensure $ServiceAccount has write access to $logsDir"
    }
}
else {
    Write-Host "[OK] Using LocalSystem (built-in, no permissions needed)"
}

Write-Host "=== Pre-Deployment Preparation Complete ==="
