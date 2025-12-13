# Deploying self-contained app and running as a Windows Service behind IIS (reverse proxy)

This document outlines the manual steps your infra team typically performs to deploy a published, self-contained ASP.NET Core app to `C:\standalone\pto-track`, configure IIS as a reverse-proxy for `http://localhost/pto-track` → `http://localhost:5139/pto-track`, and install the app executable as a Windows Service such that Service Control Manager (SCM) gets a proper "Started" status.

Important: these are manual, explanatory steps. Do not run them as a black-box automation; review each step and adapt to your environment (service account, ports, firewall, ARR settings).

High-level checklist
- Publish a self-contained Windows build of the app to `C:\standalone\pto-track`
- Ensure the app is configured to run as a Windows service (the host needs to report to SCM)
- Install the service (create service registration)
- Configure IIS as a reverse-proxy using ARR + URL Rewrite to forward `/pto-track` to `http://localhost:5139/pto-track`
- Start the service and validate end-to-end

1) Ensure the app will report to Service Control Manager
-----------------------------------------------

To have the process correctly report "Started" to the SCM when running as a Windows service, the host must be configured to run as a Windows Service. In an ASP.NET Core app using the Generic Host, add the Windows service integration in `Program.cs`:

```csharp
// Example snippet (Program.cs)
var builder = WebApplication.CreateBuilder(args);

// Required when running as a Windows Service so the host integrates with SCM
builder.Host.UseWindowsService();

// ... add services, configure web host
var app = builder.Build();
app.Run();
```

Notes:
- If you need to support both interactive development and service mode, detect the environment or command-line flag and only call `UseWindowsService()` on Windows when appropriate.
- The app's lifetime will be controlled by SCM when installed as a service; logs will go to whatever logging sinks you configure (EventLog, files, etc.). Consider adding `builder.Services.Configure<EventLogSettings>(...)` and enabling the EventLog provider for easy troubleshooting.
- The app's lifetime will be controlled by SCM when installed as a service; logs will go to whatever logging sinks you configure (EventLog, files, etc.). Consider adding `builder.Services.Configure<EventLogSettings>(...)` and enabling the EventLog provider for easy troubleshooting.

Event Log source
----------------
If you configure logging to write to the Windows Event Log it's helpful to register a stable event source so administrators can find entries easily (the app's `Program.cs` uses `SourceName = "PTO Track"`). Creating the event source requires administrative privileges and should be done once during provisioning.

Example PowerShell (run elevated):

```powershell
if (-not [System.Diagnostics.EventLog]::SourceExists('PTO Track')) {
  New-EventLog -LogName Application -Source 'PTO Track'
}

# To remove (if needed):
# if ([System.Diagnostics.EventLog]::SourceExists('PTO Track')) { Remove-EventLog -Source 'PTO Track' }
```

Note: attempts to create the event source from the application process itself will fail unless the process has the required privileges; perform the registration in deployment scripts or during host provisioning.

2) Publish a self-contained Windows build
---------------------------------------

From the repository root, run the publish command (example for `win-x64`):

```powershell
dotnet publish .\pto.track\pto.track.csproj -c Release -r win-x64 --self-contained -o C:\standalone\pto-track
```

Notes:
- The published folder should contain `pto.track.exe` and all runtime dependencies.
- Ensure file ACLs for the service account (LocalService or a specific domain account) allow read/execute in `C:\standalone\pto-track`.

3) Configure the app's URL and PathBase
---------------------------------------

Decide how the app should be bound. In your scenario the app listens on `http://localhost:5139` and uses a PathBase of `/pto-track`.

- Option A: configure `appsettings.json` (or environment variables) to include `PathBase` = `/pto-track`.
- Make sure the Kestrel listen URL includes the port and host: e.g., pass `--urls "http://localhost:5139"` or set `ASPNETCORE_URLS`.

4) Install the executable as a Windows Service (manual, reproducible commands)
----------------------------------------------------------------------------

You can register the published executable as a Windows Service. There are two common ways:

- Using `sc.exe` (built-in):

  ```powershell
  sc create "PTOTrack" binPath= "C:\standalone\pto-track\pto.track.exe" DisplayName= "PTO Track" start= auto
  ```

  Then set the service account and other options in the Services MMC or via `sc config`.

- Using PowerShell `New-Service` (simpler syntax):

  ```powershell
  New-Service -Name "PTOTrack" -BinaryPathName 'C:\standalone\pto-track\pto.track.exe' -DisplayName 'PTO Track' -StartupType Automatic
  ```

Important:
- The process must be started by SCM in service mode for `UseWindowsService()` to work and report start success. Running the EXE directly does not make it a service and won't be controlled by SCM.
- If your app uses `UseWindowsService()` it will block/start under SCM correctly; if you need the app to log or write telemetry during startup, instrument it so failures surface in Event Viewer or logs.

5) Configure IIS as a reverse-proxy for `/pto-track` -> `http://localhost:5139/pto-track`
-------------------------------------------------------------------------------

Prerequisites on the IIS host:
- Install Application Request Routing (ARR) and URL Rewrite modules.
- In ARR settings (server-level), enable "Proxy" -> "Enable proxy".

Create a site or a virtual directory and add a URL Rewrite inbound rule that rewrites requests for `/pto-track` to the backend:

Example rule (web.config or UI):

```xml
<rule name="ReverseProxy_PTOTrack" stopProcessing="true">
  <match url="^pto-track/(.*)" />
  <conditions>
  </conditions>
  <action type="Rewrite" url="http://localhost:5139/pto-track/{R:1}" logRewrittenUrl="true" />
</rule>
```

Notes:
- The `match` above assumes the incoming URL path contains `pto-track` as the first segment. Adjust if your site is configured differently.
- Ensure ARR is allowed to forward request headers needed by the app (X-Forwarded-For, X-Forwarded-Proto) if you rely on them.

6) Start the service and validate
---------------------------------

Start the service using Services MMC or SCM:

```powershell
# Start service
Start-Service -Name "PTOTrack"

# Check status
Get-Service -Name "PTOTrack" | Format-List -Property Status, DisplayName, ServiceType

# Tail logs or check Event Viewer for errors
```

If the service start succeeds (SCM reports `Running`), open a browser to:

  http://localhost/pto-track/

If you get 502 / Gateway errors at IIS, examine:
- Is the backend listening on `localhost:5139`? (Test with `Invoke-WebRequest http://localhost:5139/pto-track/` on the host.)
- Are there firewall rules preventing access to the port?
- Check the app logs and Windows Event Log for errors during startup.

7) Troubleshooting: common reasons SCM shows NOT STARTED
--------------------------------------------------------
- The process is not running under SCM (you launched the EXE manually) — only running as a registered service will integrate with SCM.
- The host did not call `UseWindowsService()` so it doesn't report state correctly.
- The service user lacks permissions to read files or bind to the configured port.
- The app crashes during startup before reporting `Started` — check Event Viewer and stdout logs.

8) Security note
----------------
Running a public-facing site on the host requires review of service account privileges and ACLs. Prefer a low-privilege service account and avoid running as Administrator.
