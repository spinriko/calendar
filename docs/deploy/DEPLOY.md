# PTO Track — Deploy & Service Troubleshooting

This document lists the recommended deploy and troubleshooting steps to get `PTO_Track_Service` running as a Windows Service and how to collect diagnostics if it fails to start.

Important: run the commands below elevated (Administrator).

1) Stop the service and publish a fresh build

```powershell
Stop-Service -Name PTO_Track_Service -ErrorAction SilentlyContinue
dotnet publish .\pto.track\pto.track.csproj -c Release -o C:\self-contained_apps\pto.track --self-contained -r win-x64
```

2) Verify service configuration

```powershell
sc.exe qc PTO_Track_Service
Get-Service -Name PTO_Track_Service | Format-List *
```

Confirm `BINARY_PATH_NAME` points to `C:\self-contained_apps\pto.track\pto.track.exe` and `SERVICE_START_NAME` is the intended service account (e.g. `ASB\s-web-ptotrack-dev`).

3) Check file and executable ACLs

Ensure the service account has Read & Execute on the publish folder and exe:

```powershell
icacls 'C:\self-contained_apps\pto.track' /save C:\temp\acl-backup.txt /t
icacls 'C:\self-contained_apps\pto.track\pto.track.exe'
# If missing, grant RX (replace domain\user as needed):
icacls 'C:\self-contained_apps\pto.track' /grant "ASB\s-web-ptotrack-dev:(RX)" /t
```

4) Confirm "Log on as a service" right

```powershell
secedit /export /cfg C:\temp\secpol.cfg
Select-String -Path C:\temp\secpol.cfg -Pattern 'SeServiceLogonRight'
```

If the account is not listed, add it via Group Policy or ask your Windows admins to add it to the "Log on as a service" policy.

5) Try run interactively to capture startup errors

Stop the service, then run the exe from an elevated prompt (this reveals startup exceptions immediately):

```powershell
Stop-Service -Name PTO_Track_Service -ErrorAction SilentlyContinue
Push-Location 'C:\self-contained_apps\pto.track'
.\pto.track.exe > C:\temp\interactive-startup.log 2>&1
Pop-Location
# After it exits, open the log:
Get-Content C:\temp\interactive-startup.log -Raw
```

If you need to reproduce the service account environment, run with `PsExec` or a scheduled task under that account.

6) If the service times out (Event IDs 7000/7009)

- Check the System (SCM) events around the start time for 7000/7009 messages.
- Consider temporary increase of SCM timeout while debugging (reboot required):

```powershell
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control' -Name ServicesPipeTimeout -Value 60000 -Type DWord
# Reboot to apply
Restart-Computer -Force
```

7) Capture .NET host tracing and corehost trace

If the interactive run doesn't show the issue, enable corehost trace and run:

```powershell
$env:COREHOST_TRACE=1
$env:COREHOST_TRACEFILE='C:\temp\corehost.log'
.\pto.track.exe
Get-Content C:\temp\corehost.log -Tail 200
```

8) Check Event Log and perf/registry permissions

- Look in the Application log for the service source or .NET Runtime errors.
- If you see "Access to the registry key 'Global' is denied" from performance counters (Elastic APM or System counters), consider disabling metrics or granting registry/perf read permission to the account. The perf counters and certain agents require registry access to `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\Global`.

9) If EventLog source is missing

The app may write raw EventLog entries if the source exists. Check and create (run once as admin):

```powershell
if (-not (Test-Path 'HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\PTO_Track_Service')) {
  New-Item -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\PTO_Track_Service' -Force | Out-Null
}
# Grant read to service account if necessary:
# (Use advanced registry ACL tools or request AD/Windows admin help.)
```

10) Collect diagnostics (run the provided `scripts/Gather-Service-Diagnostics.ps1`)

```powershell
.\scripts\Gather-Service-Diagnostics.ps1 -ServiceName PTO_Track_Service -PublishPath 'C:\self-contained_apps\pto.track'
# This will produce a zip in C:\temp; upload or paste key files (service-start-failed, scm-events, publish-acl, exe-acl, event-source-registry)
```

11) Common causes & quick fixes

- Missing Read/Execute on publish folder for the service account — add using `icacls`.
- Missing "Log on as a service" right — add via policy.
- Startup blocked by network calls or waiting for unavailable resources — run interactively to see stack traces.
- Performance counter/agent permissions (Elastic APM) throwing exceptions — disable metrics or grant registry read permissions under Perflib.

12) If you want, I can prepare a short script to apply RX ACL to the publish folder and attempt an interactive start and capture logs; ask me to prepare it and I'll add it to `scripts/`.

---

If you try these tomorrow, paste the `icacls` output and the `C:\temp\interactive-startup.log` (or the zip from the diagnostics script) and I will analyze further.