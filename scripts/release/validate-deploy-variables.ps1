param(
    [Parameter(Mandatory = $true)][string]$AutomationAcctPass,
    [Parameter(Mandatory = $true)][string]$AppServiceAccountPassword,
    [Parameter(Mandatory = $true)][string]$AutomationAcctName,
    [Parameter(Mandatory = $true)][string]$AppServiceAccount,
    [Parameter(Mandatory = $true)][string]$WebServer,
    [Parameter(Mandatory = $true)][string]$DeploymentFolder
)

function Write-Log($m) { Write-Host "[validate-vars] $m" }

function CheckVar($name, $value) {
    if (-not $value -or $value -eq "") {
        Write-Host "${name}: MISSING"
        return $false
    } else {
        Write-Host "${name}: SET"
        return $true
    }
}

Write-Log "Validating deployment variables..."

$ok = $true
if (-not (CheckVar 'automationAcctPass' $AutomationAcctPass)) { $ok = $false }
if (-not (CheckVar 'appServiceAccountPassword' $AppServiceAccountPassword)) { $ok = $false }
if (-not (CheckVar 'automationAcctName' $AutomationAcctName)) { $ok = $false }
if (-not (CheckVar 'appServiceAccount' $AppServiceAccount)) { $ok = $false }
if (-not (CheckVar 'webServer' $WebServer)) { $ok = $false }
if (-not (CheckVar 'deploymentFolder' $DeploymentFolder)) { $ok = $false }

if (-not $ok) {
    Write-Error "One or more required deploy variables are missing or empty"
    exit 1
}

Write-Log "All required variables validated successfully"
exit 0
