param(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentFolder,
    
    [Parameter(Mandatory = $true)]
    [string]$BackupFolder,
    
    [Parameter(Mandatory = $true)]
    [string]$TempFolder,

    [Parameter(Mandatory = $true)]
    [string]$ServiceAccount,

    [Parameter(Mandatory = $true)]
    [SecureString]$ServiceAccountPassword
)

Write-Host "=== Pre-Deployment Target Preparation ==="
Write-Host "Creating deployment folders on target machine..."
New-Item -ItemType Directory -Force $DeploymentFolder | Out-Null
New-Item -ItemType Directory -Force $BackupFolder | Out-Null
New-Item -ItemType Directory -Force $TempFolder | Out-Null
Write-Host "[OK] Folders created"

if ($ServiceAccount -ne "LocalSystem") {
    Write-Host "Setting NTFS permissions for $ServiceAccount on $DeploymentFolder"

    try {
        $acl = Get-Acl -Path $DeploymentFolder

        $acl.Access | Where-Object { $_.IdentityReference.Value -ieq $ServiceAccount } | ForEach-Object {
            $acl.RemoveAccessRule($_) | Out-Null
        }

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
    }

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
    }
}
else {
    Write-Host "[OK] Using LocalSystem (built-in, no permissions needed)"
}

Write-Host "=== Pre-Deployment Preparation Complete ==="
