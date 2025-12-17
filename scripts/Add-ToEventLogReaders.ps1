param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$User
)

# Require elevation
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator."
    exit 1
}

# Determine domain/machine and username
if ($User -match '^(?<domain>[^\\]+)\\(?<name>.+)$') {
    $domain = $matches.domain
    $name = $matches.name
}
else {
    $domain = $env:COMPUTERNAME
    $name = $User
}

$groupPath = "WinNT://$env:COMPUTERNAME/Event Log Readers,group"
$userPath = "WinNT://$domain/$name,user"

try {
    $group = [ADSI]$groupPath
}
catch {
    Write-Error "Failed to bind to local group 'Event Log Readers'. $_"
    exit 2
}

try {
    $userObj = [ADSI]$userPath
}
catch {
    Write-Error "User not found: $User"
    exit 3
}

try {
    if ($group.IsMember($userObj.Path)) {
        Write-Output "'$User' is already a member of 'Event Log Readers'."
        exit 0
    }

    $group.Add($userObj.Path)
    Write-Output "Added '$User' to 'Event Log Readers'."
    exit 0
}
catch {
    Write-Error "Failed to add user to group: $_"
    exit 4
}