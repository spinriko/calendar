# ADO Server Service Management Scripts

Quick utility scripts to start/stop all Azure DevOps Server services without having to remember them individually.

## Scripts

### Start-ADOServices.ps1
Starts all ADO Server services in correct dependency order:
1. SQL Server (MSSQLSERVER)
2. IIS (W3SVC)
3. Azure Pipelines Agent (vstsagent.localhost.DVO.QUANTUM-DVO)

**Usage:**
```powershell
.\Start-ADOServices.ps1
```

### Stop-ADOServices.ps1
Stops all ADO Server services in reverse dependency order (dependents first).

**Usage:**
```powershell
# Normal graceful shutdown
.\Stop-ADOServices.ps1

# Force shutdown if services hang
.\Stop-ADOServices.ps1 -Force
```

### Cleanup-AgentCaches.ps1
Removes old agent run folders and clears NuGet/npm package caches to free disk space.

**Usage:**
```powershell
# Preview what would be deleted (dry-run)
.\Cleanup-AgentCaches.ps1 -WhatIf

# Delete run folders older than 7 days (default)
.\Cleanup-AgentCaches.ps1 -Days 7 -ClearNuGet -ClearNpm

# Delete run folders older than 3 days and clear NuGet only
.\Cleanup-AgentCaches.ps1 -Days 3 -ClearNuGet
```

**Parameters:**
- `-WorkRoot` : Agent _work directory (default: `C:\azdo-agent\_work`)
- `-Days` : Delete run folders older than this many days (default: 7)
- `-ClearNuGet` : Clear dotnet NuGet global cache
- `-ClearNpm` : Clear npm package cache
- `-WhatIf` : Preview what would be deleted without actually deleting

**Notes:**
- Safe to run when no pipeline job is executing on the agent
- Use `-WhatIf` first to preview before deleting
- Typical agent folder cleanup: 500 MB - 1 GB per run removed

## Services Managed

| Service | Display Name | Purpose |
|---------|--------------|---------|
| MSSQLSERVER | SQL Server | Database for ADO Server |
| SQLSERVERAGENT | SQL Server Agent | SQL job scheduling |
| WAS | Windows Process Activation Service | IIS dependency |
| W3SVC | IIS World Wide Web Publishing | Hosts ADO Server web application |
| WMSVC | IIS Web Management Service | IIS management |
| vstsagent.localhost.DVO.QUANTUM-DVO | Azure Pipelines Agent | Build/deploy agent for DVO pool |

## Notes

- Startup is done in dependency order (SQL → SQL Agent → WAS → IIS → WMSVC → Agent)
- Shutdown is done in reverse order (Agent → IIS → WMSVC → WAS → SQL Agent → SQL) for clean shutdown
- If a service is already running/stopped, it's skipped with an OK status
- If a service is not found (e.g., not installed), it's skipped with a warning
- Exit code 0 = success, exit code 1 = one or more services failed

## Example Output

```
========================================
Starting ADO Server Services
========================================

→ Starting: SQL Server (MSSQLSERVER)...
✓ SUCCESS: SQL Server (MSSQLSERVER) started
→ Starting: IIS World Wide Web Publishing Service...
✓ SUCCESS: IIS World Wide Web Publishing Service started
✓ OK: Azure Pipelines Agent (QUANTUM-DVO) (already running)

========================================
Summary
========================================
Started:  2
Skipped:  1
```
