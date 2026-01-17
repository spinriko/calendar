# PTO Track — Deployment Architecture

This document describes the PTO Track deployment model, infrastructure dependencies, and operational characteristics.

## Deployment Model: IIS In-Process

PTO Track is deployed as a **self-contained .NET 8 application** running in **IIS in-process** (AspNetCoreModuleV2 with `hostingModel="inprocess"`).

### Why In-Process?

- **Performance**: No inter-process communication overhead vs. out-of-process
- **Simplicity**: Single process lifecycle tied to AppPool recycling
- **Windows Auth**: Native support for Negotiate/Kerberos in-process
- **Monitoring**: Integrated with IIS request logging and performance counters

### Deployment Structure

```
C:\inetpub\wwwroot\pto-track\
├── pto.track.exe                (self-contained runtime)
├── pto.track.dll
├── appsettings.json             (base config)
├── appsettings.Production.json   (prod overrides)
├── web.config                    (IIS bootstrap config)
├── wwwroot/                      (static assets)
│   ├── dist/                     (fingerprinted JS/CSS bundles)
│   └── lib/                      (vendor libraries: DayPilot, etc.)
└── logs/                         (stdout logging from hostingModel)
```

### Self-Contained Deployment

The app is published with `-r win-x64 --self-contained`, meaning:

- **All runtime files included**: No .NET SDK required on target server
- **Single folder copy**: Deploy entire folder to replace previous version
- **Artifact size**: ~150MB+ (includes .NET 8 runtime for Windows)
- **Pipeline**: Build stage produces self-contained artifact → Publish stage packages into CI artifact → Deploy stage copies to IIS folder

## Windows Authentication & Service Accounts

### AppPool Identity

The IIS AppPool runs under a **Windows service account**:

```
Default identity: QUANTUM\ptoappsvc (or corp equivalent)
Required permissions:
  - Read + Execute on C:\inetpub\wwwroot\pto-track\
  - Read on application folder (logs, configs, assets)
  - Database permissions (see below)
```

### Database Access

Connection string uses **Windows Integrated Authentication**:

```csharp
"PtoTrackDbContext": "Server=SQL_SERVER,1433;Database=pto_track_dev;Integrated Security=True;TrustServerCertificate=True;"
```

The AppPool identity (`QUANTUM\ptoappsvc`) must be a **SQL Server login** with appropriate database role (e.g., `db_owner` for dev, minimal for prod).

### npm Build Integration

**Critical**: Frontend build (TypeScript → JavaScript) runs during **publish phase**, not deployment.

- `.NET SDK` must be available on build agent
- **npm and Node.js** must be in PATH on build agent
- **Local dev**: Use `nvm4w` or package manager for Node 20+
- **Azure DevOps pipeline**: Uses `npmPath` variable from DevOps Automation Accounts group

## IIS Configuration (web.config)

The [web.config](../../pto.track/web.config) file bootstraps the app:

```xml
<aspNetCore processPath=".\pto.track.exe" hostingModel="inprocess" 
            stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
  <environmentVariables>
    <add name="ASPNETCORE_ENVIRONMENT" value="__ASPNETCORE_ENVIRONMENT__" />
  </environmentVariables>
</aspNetCore>
```

### Key Settings

| Setting | Purpose | Value |
|---------|---------|-------|
| `processPath` | Executable to run | `.\pto.track.exe` (relative) |
| `hostingModel` | In-process vs. out-of-process | `inprocess` |
| `stdoutLogEnabled` | Capture console output | `true` |
| `stdoutLogFile` | Redirect stdout to file | `.\logs\stdout` (IIS logs in real-time) |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Dev/Staging/Production |

### stdout Logging

ASP.NET Core logs to console; IIS captures stdout when enabled:

```
Location: C:\inetpub\wwwroot\pto-track\logs\stdout_*.log
Rotation: IIS handles log rolling (timestamp-based filenames)
Useful for: Exception traces, startup diagnostics, request flow
```

## CI/CD Artifact Flow

### Build Stage
- Restores NuGet packages
- Runs npm ci + npm run build:js (TypeScript bundling)
- Builds C# (Release config)
- **Output**: Artifact named `build_artifact`

### Publish Stage
- Runs `dotnet publish` with:
  - `--configuration Release`
  - `--runtime win-x64`
  - `--self-contained true`
- **Packages** `scripts/release/` folder into artifact (deploy scripts)
- **Output**: Single `build_artifact` with:
  - `publish/` — self-contained app
  - `scripts/release/` — deployment automation scripts

### Deploy Stage (DeployDevToIIS)
1. Downloads `build_artifact`
2. Runs pre-deploy checks (IIS feature validation)
3. Copies `publish/` folder to `C:\inetpub\wwwroot\pto-track\`
4. Validates AppPool + sets connection string
5. Restarts AppPool + health-checks endpoint

Scripts source: `$(Pipeline.Workspace)/build_artifact/scripts/release/`

## Database Migrations

**Current approach**: Migrations run **on-demand** at application startup (EF Core).

```csharp
// In Program.cs
if (!env.IsProduction)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PtoTrackDbContext>();
    db.Database.Migrate();
}
```

**For production**: Either:
- (Option A) Pre-migrate in deploy script before copying app
- (Option B) Use bare SQL scripts + maintenance window
- (Option C) Disable auto-migrate, run explicit `dotnet ef database update` before deploy

**Status**: Currently configured for **dev/staging only**. Production migration strategy TBD.

## Configuration Precedence

ASP.NET Core loads configuration in order (later overrides earlier):

1. `appsettings.json` (base defaults)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (Production/Staging/Development)
3. Environment variables
4. User secrets (dev only)
5. Key-Value pairs in Azure Key Vault (if enabled)

For **IIS deployment**:

- `ASPNETCORE_ENVIRONMENT` set in `web.config`
- Connection string injected by deploy script (see `scripts/release/set-connection-string.ps1`)
- Other secrets sourced from secured variable groups (Pipeline Secrets)

## Monitoring & Diagnostics

### Logs
- **Application logs**: `C:\inetpub\wwwroot\pto-track\logs\stdout_*.log` (ASP.NET Core structured logs)
- **IIS logs**: `C:\inetpub\logs\LogFiles\W3SVC1\` (HTTP request/response logs)
- **Event Viewer**: Windows Event Log (AppPool crashes, service account issues)

### Health Check
- Endpoint: `GET /health` (or configured health path)
- Pipeline validation: `iis-health-check.ps1` pings endpoint post-deploy
- Success criteria: HTTP 200 within X seconds

### Performance
- **CPU**: IIS AppPool Performance object
- **Memory**: dotnet.exe private working set
- **Requests**: IIS HTTP requests/sec counters

## Rollback & Recovery

### Rollback Steps
1. Keep previous app folder as `pto-track.backup`
2. On deploy failure, copy backup back to `pto-track`
3. Restart AppPool
4. Run health check

### Database Rollback
- If migration broke data, EF Core provides `Remove-Migration` for pre-deployed versions
- For prod: ensure backup + test restore procedure before deploying

## Appendix: Common Issues

| Issue | Cause | Fix |
|-------|-------|-----|
| 500 errors immediately after deploy | Startup exception (DB, config) | Check `stdout_*.log` in logs/ folder |
| "Access Denied" on `pto-track.exe` | AppPool identity lacks R+X permissions | `icacls` grant RX to service account |
| "Connection string missing" | Config not injected by deploy script | Run `set-connection-string.ps1` manually |
| AppPool won't start | Windows Auth handler registration fails | Check `ASPNETCORE_ENVIRONMENT` matches config |
| npm not found during publish | Agent build PATH incomplete | Verify `npmPath` variable in DevOps group |

---

**See also**:
- [Deployment Runbook](DEPLOY.md) — step-by-step deployment procedures
- [Service Accounts](SERVICE-ACCOUNTS.md) — identity matrix and permissions
- [Pipeline Overview](../ci/PIPELINE-OVERVIEW.md) — CI/CD stages and variables
