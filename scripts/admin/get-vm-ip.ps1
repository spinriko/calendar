param(
    [string]$VMName = "DeployTestVM"
)

$vm = Get-VM -Name $VMName -ErrorAction Stop

$ip = $vm.NetworkAdapters |
    Select-Object -ExpandProperty IPAddresses |
    Where-Object { $_ -match '^\d{1,3}(\.\d{1,3}){3}$' -and $_ -notlike '169.*' } |
    Select-Object -First 1

if (-not $ip) {
    Write-Host "No valid IPv4 address found for VM '$VMName'."
    exit 1
}

Write-Host "VM '$VMName' IP Address: $ip"
