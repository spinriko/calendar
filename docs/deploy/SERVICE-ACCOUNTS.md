# Service Accounts & Security Model

This document defines all service accounts used in the PTO Track system, their roles, permissions, and management procedures.

## Service Account Inventory

| Account | Type | System | Purpose | Managed By |
|---------|------|--------|---------|-----------|
| `QUANTUM\s-webdev-agent` | Service Account | Windows | Azure Pipelines build agent | ADO Server |
| `QUANTUM\ptoappsvc` | Service Account | Windows | IIS AppPool identity (dev) | AD / Windows |
| `QUANTUM\ptoappsvc-prod` | Service Account | Windows | IIS AppPool identity (prod) | AD / Windows |
| SQL Agent Login | SQL Server | SQL Server | Database access (app) | SQL DBA |
| Pipeline PAT | Token | Azure DevOps | Agent pool registration, source checkout | ADO Server |

## Build Agent Service Account

### Account: `QUANTUM\s-webdev-agent`

**Role**: Azure Pipelines agent service

**System**: Windows Server (ADO Server machine)

**Service**: `vstsagent.localhost.DVO.QUANTUM-DVO` (Windows Service)

**Responsibilities**:
- Checkout source code from Azure Repos
- Restore NuGet packages
- Run npm build (TypeScript compilation)
- Execute dotnet build/test/publish
- Upload artifacts to pipeline
- Trigger deployment jobs

### Required Permissions

#### Local Machine
```powershell
# Must be able to:
- Read C:\code\dotnet\pto (source folder)
- Read/Write C:\azdo-agent\_work (agent workspace)
- Access node_modules, npm cache, NuGet cache
- Run dotnet, npm, git executables

# Windows Firewall: Allow outbound HTTPS to artifact storage
```

#### Network
```
- Outbound: Azure DevOps Server (http://quantum:8080)
- Outbound: Git repos (HTTP/HTTPS)
- Outbound: NuGet.org (HTTPS) for package restore
- Outbound: npm registry (HTTPS) for npm ci
```

#### Service Rights
```powershell
# "Log on as a service" right required
secedit /export /cfg C:\temp\secpol.cfg
Select-String -Path C:\temp\secpol.cfg -Pattern 'SeServiceLogonRight'
# Should list: QUANTUM\s-webdev-agent
```

### PATH Configuration

**Issue**: `nvm4w` (Node Version Manager for Windows) is user-installed; not available in service account PATH.

**Solution**: `npmPath` variable in DevOps Library group provides explicit path to npm:

```
Variable Group: DevOps Automation Accounts
Name: npmPath
Value: C:\Users\<agent-user>\AppData\Local\nvm4w\nodejs\<version>\npm.cmd
       (or) C:\Program Files\nodejs\npm.cmd
```

**Pipeline usage** (in `azure-pipelines.yml`):

```powershell
$npmDir = Split-Path "$(npmPath)"
$env:PATH = "$npmDir;$env:PATH"
npm ci
npm run build:js
```

### Environment Variables

Agent service inherits Windows environment, but may not have user-specific variables:

- `ASPNETCORE_ENVIRONMENT`: Set explicitly in pipeline tasks (not from service account)
- `DOTNET_CLI_TELEMETRY_OPTOUT`: Can be set at service account level if needed
- `HOME` / `USERPROFILE`: May differ; can affect .NET SDK telemetry and cache paths

## IIS AppPool Service Account (Dev)

### Account: `QUANTUM\ptoappsvc`

**Role**: AppPool identity for PTO Track application (dev environment)

**System**: Windows Server (IIS host machine, may be same as ADO Server)

**IIS Configuration**:
```
AppPool Name: PtoTrackAppPool (or similar)
Identity: ApplicationPoolIdentity (QUANTUM\ptoappsvc)
Load User Profile: true (allows access to user-specific cache folders)
```

### Required Permissions

#### File System
```powershell
icacls C:\inetpub\wwwroot\pto-track /grant "QUANTUM\ptoappsvc:(RX)" /t
icacls C:\inetpub\wwwroot\pto-track\logs /grant "QUANTUM\ptoappsvc:(RX)" /t
```

**Permissions needed**:
- `(RX)` — Read + Execute on app folder (access exe, dlls, static assets)
- `(RW)` — Read + Write on logs folder (write stdout logs)

#### Database Access

**Connection String**:
```
Server=SQL_SERVER,1433;Database=pto_track_dev;Integrated Security=True;TrustServerCertificate=True;
```

**SQL Server Configuration**:
1. Create SQL login for the AppPool account:
   ```sql
   CREATE LOGIN [QUANTUM\ptoappsvc] FROM WINDOWS;
   ```

2. Add login to database:
   ```sql
   USE pto_track_dev;
   CREATE USER [ptoappsvc] FOR LOGIN [QUANTUM\ptoappsvc];
   ALTER ROLE db_owner ADD MEMBER [ptoappsvc];  -- for dev; use minimal role in prod
   ```

3. For **Integrated Windows Auth** to work:
   - SQL Server must have Windows Authentication enabled
   - AppPool account must be a **domain account** (not local `BUILTIN\` account)
   - SQL Server SPN must be registered (usually done automatically by SQL Server)

#### Windows Auth & Kerberos

If the app uses **Negotiate/Kerberos** (e.g., Active Directory single sign-on):

- AppPool identity must be **trusted for delegation** in Active Directory (if using Kerberos constrained delegation)
- Or use **Simple Windows Auth** without delegation (less secure but simpler)

**Configuration** (in `appsettings.json`):
```json
"Authentication": {
  "Mode": "Windows"  // or "Mock" for testing
}
```

### Log Access

```powershell
# Create logs folder if not present
New-Item -Path C:\inetpub\wwwroot\pto-track\logs -ItemType Directory -ErrorAction SilentlyContinue

# Grant write access
icacls C:\inetpub\wwwroot\pto-track\logs /grant "QUANTUM\ptoappsvc:(RW)" /t
```

## IIS AppPool Service Account (Production)

### Account: `QUANTUM\ptoappsvc-prod` (or corp equivalent)

Same structure as dev account, but with:

**Permissions**:
- Minimal database role (e.g., `db_reader`, `db_writer` only, not `db_owner`)
- Network restrictions (firewall rules limiting which servers can be accessed)
- Audit logging enabled for all database operations

**Difference from Dev**:
- Separate account per environment (security isolation)
- Tighter RBAC (role-based access control)
- Network-level restrictions (corp firewall policies)

## SQL Server Login & Database User

### Account: `QUANTUM\ptoappsvc` (from AppPool identity)

**Role**: Application database access (read/write operations)

**Scope**: Database `pto_track_dev` (dev) or `pto_track_prod` (prod)

**Setup**:
```sql
-- On SQL Server instance
CREATE LOGIN [QUANTUM\ptoappsvc] FROM WINDOWS;

-- In application database
USE pto_track_dev;
CREATE USER [ptoappsvc] FOR LOGIN [QUANTUM\ptoappsvc];

-- Assign role (dev = owner; prod = minimal)
ALTER ROLE db_owner ADD MEMBER [ptoappsvc];
```

**Permissions Assigned**:
- `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all application tables
- `EXECUTE` on all stored procedures (if used)
- `CREATE TABLE` (for migrations only; can be restricted in prod)

**Database Migrations**:
- Account must have schema change rights during deployment
- Post-migration, rights can be restricted to data-only operations

## Pipeline Service Principal (PAT)

### Token: Personal Access Token (PAT)

**Purpose**: Agent registration + source checkout

**Scope** (DevOps Server):
- `Agent Pools` (read/manage)
- `Code` (read — source checkout)
- `Build` (read/write — queue builds, upload artifacts)

**Management**:
```
Azure DevOps Server → User Settings → Personal Access Tokens
Name: pto-track-agent-registration-PAT
Expiration: 90 days (recommend renewal cycle)
Scopes: Agent Pools, Code (Read), Build (Read & Write)
```

**Storage**: Stored securely during agent registration; not stored in source control

**Rotation**: Schedule quarterly renewals to comply with security policy

## Access Control Matrix

| Account | Build | Test | Publish | Deploy | Logs | Database |
|---------|-------|------|---------|--------|------|----------|
| `s-webdev-agent` | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |
| `ptoappsvc` | ✗ | ✗ | ✗ | ✓ | ✓ | ✓ |
| PAT | ✓ (agent mgmt) | ✗ | ✗ | ✗ | ✗ | ✗ |

## Troubleshooting

### Build Agent Issues

**Problem**: Agent shows "Offline" or "Idle" but won't pick up jobs

```powershell
# Check service status
Get-Service vstsagent.localhost.DVO.QUANTUM-DVO | Format-List Status, StartType

# View agent logs
Get-Content C:\azdo-agent\_diag\Agent_*.log -Tail 50
```

**Problem**: npm not found during build

```powershell
# Verify npmPath variable in DevOps group
# https://quantum:8080/tfs/DefaultCollection/_library?itemType=VariableGroups

# Test npm in agent's context
ssh s-webdev-agent@QUANTUM-DVO
npm --version  # Should work; if not, update npmPath
```

### AppPool Issues

**Problem**: AppPool won't start; Event Viewer shows startup exception

```powershell
# Check AppPool credentials in IIS
Get-IISAppPool -Name PtoTrackAppPool | Format-List Identity

# Verify file permissions
icacls C:\inetpub\wwwroot\pto-track

# Check stdout logs for exception
Get-Content C:\inetpub\wwwroot\pto-track\logs\stdout*.log -Tail 50
```

**Problem**: "Access Denied" connecting to SQL Server

```sql
-- Verify login exists
SELECT * FROM sys.server_principals WHERE name = 'QUANTUM\ptoappsvc';

-- Verify database user exists
USE pto_track_dev;
SELECT * FROM sys.database_principals WHERE name = 'ptoappsvc';

-- Check role membership
SELECT * FROM sys.database_role_members WHERE member_principal_id = (
  SELECT principal_id FROM sys.database_principals WHERE name = 'ptoappsvc'
);
```

---

**See also**:
- [Deployment Architecture](ARCHITECTURE.md) — IIS model and configuration
- [Pipeline Overview](../ci/PIPELINE-OVERVIEW.md) — CI/CD stages and agent pool
