param(
    [Parameter(Mandatory = $true)]
    [string]$Hostname,

    [Parameter(Mandatory = $true)]
    [string]$IPAddress
)

$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"

Write-Host "Updating hosts file at: $hostsPath"

# Ensure file is writable
if ((Get-Item $hostsPath).Attributes -band [System.IO.FileAttributes]::ReadOnly) {
    Write-Host "Removing read-only attribute"
    attrib -r $hostsPath
}

# Read all lines
$lines = Get-Content -Path $hostsPath -Encoding ASCII

# Remove any existing entry for this hostname
$filtered = $lines | Where-Object { $_ -notmatch "\b$Hostname\b" }

# Add the new entry
$newEntry = "$IPAddress`t$Hostname"

# Write back using ASCII (required for hosts file)
$filtered + $newEntry | Set-Content -Path $hostsPath -Encoding ASCII -Force

Write-Host "Added/updated entry: $newEntry"
