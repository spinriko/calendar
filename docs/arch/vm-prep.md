## Local Mimicry Setup (Non-Domain WinRM)

### Hosts File Entries (on ADO Server / Pipeline Machine)

`C:\Windows\System32\drivers\etc\hosts`

Add:

`<vm-ip>   WIN-4N5G0JEC76Q`  
`<vm-ip>   workmimicry.corp.local`

### Configure TrustedHosts (on ADO Server / Pipeline Machine)

```powershell
# Allow WinRM to connect to non-domain VM
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "WIN-4N5G0JEC76Q"

or
 
$existing = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value
$new = "$existing,WIN-4N5G0JEC76Q,workmimicry.corp.local"
Set-Item WSMan:\localhost\Client\TrustedHosts -Value $new
