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

## Services Managed

| Service | Display Name | Purpose |
|---------|--------------|---------|
| MSSQLSERVER | SQL Server | Database for ADO Server |
| W3SVC | IIS World Wide Web Publishing | Hosts ADO Server web application |
| vstsagent.localhost.DVO.QUANTUM-DVO | Azure Pipelines Agent | Build/deploy agent for DVO pool |

## Notes

- Startup is done in dependency order (SQL → IIS → Agent)
- Shutdown is done in reverse order (Agent → IIS → SQL) for clean shutdown
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
