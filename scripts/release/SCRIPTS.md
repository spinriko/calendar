# PowerShell Deployment Scripts

This document summarizes the PowerShell scripts extracted from the Azure Pipeline YAML for improved maintainability and reusability.

## Scripts Summary

All scripts are located in `scripts/release/` and follow a consistent pattern with parameter validation, logging, and error handling.

### Core Deployment Scripts

| Script | Purpose | Parameters |
|--------|---------|-----------|
| **before-deploy-iis.ps1** | Pre-deployment IIS validation & cleanup | `-PhysicalPath`, `-AppPoolUser`, `-StopIISIfNeeded` |
| **finish-deploy-iis.ps1** | Post-deployment IIS configuration | `-PhysicalPath`, `-AppPoolUser` |
| **check-iis-installed.ps1** | Validate IIS Web-Server feature | `-FeatureName` (e.g., "Web-Server") |
| **extract-artifact.ps1** | Extract ZIP artifact to destination | `-SourceFolder`, `-DestinationFolder` |
| **set-connection-string.ps1** | Set ConnectionStrings__PtoTrackDbContext at machine scope | `-ConnectionString` |
| **swap-deployment-folders.ps1** | Rotate deployment folders (backup/current/temp) | `-DeploymentPath`, `-BackupPath`, `-TempPath` |
| **update-rewrite-rules.ps1** | Add HTTP/HTTPS rewrite rules to web.config | `-WebConfigPath`, `-ForwardedHttpUrl`, `-ForwardedHttpsUrl` |
| **iis-health-check.ps1** | Post-deployment health check via HTTP endpoint | `-HealthUrl`, `-TimeoutSeconds` |
| **iis-prereq-check.ps1** | Pre-deployment validation of IIS configuration | (ad-hoc) |

## Azure Pipeline Integration

All scripts are invoked via `PowerShellOnTargetMachines@3` task with:
- `ScriptPath`: Points to the external `.ps1` file
- `ScriptArguments`: Passes parameters from pipeline variables
- `Machines`: Target web server (from `$(webServerDev)`)
- `UserName` / `UserPassword`: Service account credentials

### Example Task Structure

```yaml
- task: PowerShellOnTargetMachines@3
  displayName: "Check IIS Installation"
  inputs:
    Machines: "$(webServerDev)"
    UserName: "$(automationAcctName)"
    UserPassword: "$(automationAcctPass)"
    ScriptPath: '$(Build.SourcesDirectory)\scripts\release\check-iis-installed.ps1'
    ScriptArguments: '-FeatureName "Web-Server"'
    CommunicationProtocol: "Http"
```

## Script Features

All scripts include:
- ✅ **Parameter validation**: Ensures all required parameters are provided and non-empty
- ✅ **Consistent logging**: `Write-Log` function with timestamps
- ✅ **Error handling**: try-catch blocks with detailed error messages
- ✅ **Exit codes**: 0 for success, 1 for failure
- ✅ **Idempotency**: Safe to run multiple times without side effects

## Deployment Order (Pipeline Sequence)

1. **check-iis-installed.ps1** - Verify IIS Web-Server feature
2. **stop-and-remove-service.ps1** - Stop existing Windows service
3. **extract-artifact.ps1** - Extract published application ZIP
4. Set ASPNETCORE_ENVIRONMENT on the IIS App Pool (done by `finish-deploy-iis.ps1` using the `-Environment` parameter)
5. **set-connection-string.ps1** - Configure database connection
6. **swap-deployment-folders.ps1** - Rotate folders (backup current, deploy new)
7. **update-rewrite-rules.ps1** (optional, disabled) - Configure rewrite rules
8. **create-windows-service.ps1** - Create/start Windows service

## Notes

- All scripts are designed to be **runnable both from the pipeline and manually on servers**
- ASPNETCORE_ENVIRONMENT is now managed by setting an environment variable on the IIS app pool during DeployDev (see `finish-deploy-iis.ps1 -Environment`).
- The `swap-deployment-folders.ps1` provides rollback capability via backup folder
- `update-rewrite-rules.ps1` is currently disabled (condition: false) pending further testing

## Manual Execution Example

```powershell
# Run a script directly from the command line
.\scripts\release\check-iis-installed.ps1 -FeatureName "Web-Server"

# Or with multiple parameters
.\scripts\release\set-connection-string.ps1 -ConnectionString "Server=localhost;Database=pto-track;Trusted_Connection=true;"
```
