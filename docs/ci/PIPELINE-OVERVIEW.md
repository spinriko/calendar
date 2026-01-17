# Azure Pipelines Pipeline Overview

This document describes the PTO Track Azure Pipelines CI/CD structure, stage dependencies, artifact flow, and key decisions.

## Pipeline Architecture

PTO Track uses a **multi-stage pipeline** with clear separation of concerns:

```
Build → Test → Analyze → Publish → DeployDevToIIS
```

### Design Principles

1. **Fail-Fast**: Quick feedback on breaking changes (Build → Test stages run first)
2. **Non-Blocking Analysis**: Code analyzers run independently (Analyze stage), warnings don't block deployment
3. **Artifact-Based Deployment**: Deploy stage receives pre-built artifacts, no new compilation
4. **Cross-Machine Deployment**: Deploy agent may differ from build agent; scripts packaged in artifact

## Pipeline Stages

### Stage 1: Build

**Runs On**: `DVO` agent pool (Windows Server with .NET 8 SDK, npm, git)

**Steps**:

1. **Checkout**
   - Source: Azure Repos Git
   - Credentials: PAT from Library (DevOps Automation Accounts group)
   - Submodules: Disabled (not used)
   - Fetch depth: 0 (full history for versioning)

2. **Npm Restore & Frontend Build**
   ```powershell
   npm ci                    # Install exact versions from package-lock.json
   npm run build:js          # TypeScript → JavaScript (wwwroot/dist/)
   ```
   - **Why separate from .NET build**: Frontend build is independent toolchain
   - **npmPath variable**: Resolves npm location for service account context
   - **Output**: Fingerprinted bundles in `wwwroot/dist/asset-manifest.json`

3. **.NET Build**
   ```powershell
   dotnet restore            # NuGet package restore
   dotnet build -c Release   # Compile all projects (Release config)
   ```
   - **Configuration**: Release (optimized, no debug symbols)
   - **Platforms**: AnyCPU (no platform-specific targeting)

4. **Publish Artifact**
   - Name: `build_artifact`
   - Path: `$(Build.ArtifactStagingDirectory)/`
   - Retention: 30 days (configurable)
   - Usage: Consumed by Test, Analyze, Publish, Deploy stages

### Stage 2: Test

**Runs On**: `DVO` agent pool (reuses agent, minimal overhead)

**Depends On**: Build (artifact available)

**Projects Tested**:

1. `pto.track.services.tests` — Service layer unit tests
2. `pto.track.tests` — Integration tests (TestHost, in-memory DB)
3. `pto.track.data.tests` — Data access layer tests
4. Frontend tests (Jest) — JavaScript/TypeScript tests

**Steps**:

1. **Download Build Artifact** (contains pre-built binaries)
2. **Run .NET Tests**
   ```powershell
   dotnet test pto.track.services.tests /p:RunAnalyzersDuringBuild=false
   dotnet test pto.track.tests /p:RunAnalyzersDuringBuild=false
   dotnet test pto.track.data.tests /p:RunAnalyzersDuringBuild=false
   ```
   - **Coverage**: Collects code coverage (OpenCover format)
   - **Output**: TRX reports (XML), coverage reports

3. **Merge Coverage Reports**
   - Tool: ReportGenerator
   - Generates: HTML coverage report, coverage summary

4. **Publish Test Results**
   - TRX files → Azure Pipelines test view (failures, pass/fail timeline)
   - Coverage reports → Artifacts folder

5. **Run JS Tests** (if included)
   ```powershell
   npm ci --prefix pto.track.tests.js
   npm test --prefix pto.track.tests.js
   ```
   - Tool: Jest
   - Coverage: ES6 modules, JSX/TSX

**Failure Handling**:
- If any test fails, stage fails
- Pipeline stops (Deploy stage blocked)
- User must fix code, trigger new build

### Stage 3: Analyze

**Runs On**: `DVO` agent pool

**Depends On**: Build (uses built binaries)

**Separate from Test**: Analyzers run in their own stage to avoid blocking Test stage on warning-only issues

**Steps**:

1. **Download Build Artifact**

2. **Run Roslyn Analyzers**
   ```powershell
   dotnet build -c Release /p:RunAnalyzers=true
   ```
   - Generates SARIF report (Semantic Analysis Results Format)
   - Output: `artifacts/analyzers/` folder

3. **Code Metrics**
   - Tool: Metrics Runner (custom code analysis)
   - Collects: Lines of code, cyclomatic complexity, maintainability index
   - Output: `artifacts/metrics/code-metrics.json`

4. **Publish Analyzer Artifacts**
   - SARIF report (viewable in Azure Pipelines UI)
   - Raw logs
   - Metrics JSON

**Failure Handling**:
- Non-blocking (does not prevent Publish/Deploy)
- Useful for tracking code health trends
- Can configure branch policy to block on analyzer warnings (optional)

### Stage 4: Publish

**Runs On**: `DVO` agent pool (must have .NET 8 SDK + npm)

**Depends On**: Build (uses sources + built artifacts)

**Purpose**: Generate self-contained application package for deployment

**Checkout**: `self` (needs project file for dotnet publish)

**Steps**:

1. **Generate Self-Contained App**
   ```powershell
   # Resolve glob pattern to actual project path
   $projPath = @(Get-ChildItem -Path "$(Build.SourcesDirectory)" -Filter "pto.track.csproj" -Recurse)[0].FullName
   
   # Publish with npm in PATH (needed for build targets)
   $npmDir = Split-Path "$(npmPath)"
   $env:PATH = "$npmDir;$env:PATH"
   
   dotnet publish $projPath `
     --configuration Release `
     --runtime win-x64 `
     --self-contained true `
     --output $(Build.ArtifactStagingDirectory)/publish
   ```

   **Why npm in PATH?**
   - Build target runs `npm run build:js` during publish
   - Service account context doesn't have npm in PATH
   - Variable provides explicit path

   **Output**:
   - All .NET binaries
   - All runtime files (.NET 8 for win-x64)
   - All frontend assets (wwwroot/)
   - ~150MB total

2. **Package Release Scripts**
   ```powershell
   Copy-Item -Path scripts/release/* -Destination $(Build.ArtifactStagingDirectory)/scripts/release/ -Recurse
   ```

   **Scripts included**:
   - `iis-prereq-check.ps1` — Validate IIS features installed
   - `before-deploy-iis.ps1` — Create/validate AppPool
   - `set-connection-string.ps1` — Update web.config with connection string
   - `finish-deploy-iis.ps1` — Set AppPool identity
   - `iis-health-check.ps1` — Verify app started successfully

   **Why package scripts?**
   - Deploy agent may not have source checkout
   - Scripts needed for IIS automation
   - Single artifact is self-contained

3. **Publish Artifact**
   - Name: `build_artifact` (same name, overwrites earlier builds)
   - Contains:
     - `publish/` — self-contained app
     - `scripts/release/` — deployment scripts

**Output**:
- `build_artifact` (complete deployment package)
- Ready to deploy to any Windows IIS server

### Stage 5: DeployDevToIIS

**Runs On**: **Different agent pool** (may be different machine)

**Depends On**: Publish (artifact available)

**Environment**: `Development` (or Staging/Production as needed)

**Deployment Target**: IIS AppPool on `C:\inetpub\wwwroot\pto-track\`

**No Checkout**: Deploy agent does not need source code (everything in artifact)

**Steps**:

1. **Pre-Deployment Checks**
   ```powershell
   & "$(Pipeline.Workspace)/build_artifact/scripts/release/iis-prereq-check.ps1"
   ```
   - Verify IIS Web Server role installed
   - Verify URL Rewrite module (if needed)
   - Fail fast if IIS not available

2. **Create/Validate AppPool**
   ```powershell
   & "$(Pipeline.Workspace)/build_artifact/scripts/release/before-deploy-iis.ps1"
   ```
   - Check if AppPool exists
   - Create if missing, using service account identity from variable group
   - Verify AppPool is stopped (for clean deployment)

3. **Copy Application Files**
   ```powershell
   Copy-Item -Path "$(Pipeline.Workspace)/build_artifact/publish/*" `
     -Destination "C:\inetpub\wwwroot\pto-track\" -Recurse -Force
   ```
   - Replace all files in AppPool folder
   - Includes exe, dlls, wwwroot/, config files

4. **Inject Configuration**
   ```powershell
   & "$(Pipeline.Workspace)/build_artifact/scripts/release/set-connection-string.ps1"
   ```
   - Update `web.config` with:
     - `ASPNETCORE_ENVIRONMENT` (Dev/Production)
     - Connection string (from DevOps variable group: `ASBDotNetWebApps-DEV`)

5. **Set AppPool Identity**
   ```powershell
   & "$(Pipeline.Workspace)/build_artifact/scripts/release/finish-deploy-iis.ps1"
   ```
   - Set AppPool identity to service account (`QUANTUM\ptoappsvc`)
   - Verify permissions on app folder
   - Start AppPool

6. **Health Check**
   ```powershell
   & "$(Pipeline.Workspace)/build_artifact/scripts/release/iis-health-check.ps1"
   ```
   - `GET https://localhost/health`
   - Retry for 30 seconds (AppPool might be warming up)
   - Fail if endpoint not responding

**Failure Handling**:
- If any step fails, pipeline stops
- Deployment is **not rolled back** (manual intervention required)
- Check logs in `C:\inetpub\wwwroot\pto-track\logs\`

## Variable Groups

### DevOps Automation Accounts

**Scope**: All projects in collection

**Variables**:

| Name | Purpose | Example Value |
|------|---------|----------------|
| `ServiceAccountName` | AppPool identity domain\user | `QUANTUM\ptoappsvc` |
| `ServiceAccountPassword` | AppPool account password | `***` (secret) |
| `sqlConnectionString` | Database connection string | `Server=SQL_SERVER,1433;Database=pto_track_dev;...` |
| `npmPath` | Path to npm executable | `C:\Users\...\nvm4w\v20.x.x\npm.cmd` |

**Secrets**: `ServiceAccountPassword` marked as secret (encrypted in DevOps)

**Access**: Limited to authorized users; used in pipeline via `${{ variables.groupName }}`

### ASBDotNetWebApps-DEV

**Scope**: Dev environment deployments

**Variables**:

| Name | Purpose | Example Value |
|------|---------|----------------|
| `deploymentFolder` | Target IIS folder | `C:\inetpub\wwwroot\pto-track` |
| `appServiceAccount` | Same as `ServiceAccountName` above; local test VM default is `.\ptoappsvc` | `QUANTUM\ptoappsvc` |
| `appPoolName` | IIS AppPool name | `PtoTrackAppPool` |
| `pto-track-env` | ASPNETCORE_ENVIRONMENT | `Development` |
| `healthUrl` | Health check endpoint | `https://localhost/health` |
| `forceDeploy` | Skip safety checks | `false` (set to `true` to force) |

**Test VM account creation:** When creating local test VMs with `setup-deploy-vm.ps1`, the script will ensure two local accounts exist: `s-webdev-agent` (automation agent — added to local **Administrators** by default) and `ptoappsvc` (AppPool account — simple user granted **Log on as a service**). Passwords are prompted when creating the VM and should be stored in secure variable groups if you intend to use them in pipelines. These accounts are for local test VMs only and should not be used in production or corporate domain environments.

## Artifact Flow

```
Source Code (Git)
    ↓
[Build Stage]
    ├─ npm ci + build:js
    ├─ dotnet restore + build
    ├─ Publish to ArtifactStagingDirectory
    └─ Create build_artifact
    ↓
[Test Stage] ←─ Download build_artifact (no compilation needed)
    ├─ Run .NET tests
    ├─ Merge coverage
    └─ Publish test results
    ↓
[Analyze Stage] ←─ Download build_artifact
    ├─ Run analyzers (on built binaries)
    ├─ Collect metrics
    └─ Publish artifacts
    ↓
[Publish Stage] ← Checkout sources (need .csproj for publish targets)
    ├─ dotnet publish (self-contained win-x64)
    ├─ Copy scripts/release/
    ├─ Create build_artifact (overwrites)
    └─ Publish artifact
    ↓
[DeployDevToIIS Stage] ← Download build_artifact
    ├─ Pre-deploy checks
    ├─ Copy publish/ to C:\inetpub\
    ├─ Update web.config
    ├─ Start AppPool
    └─ Health check
    ↓
[IIS Running]
```

## Key Decisions & Trade-Offs

### Why Npm in Publish, Not Build?

**Option A (Current)**: Build stage: npm ci + build:js. Publish stage: dotnet publish (build target runs npm again).

**Pro**: Frontend bundles cached if no changes, publish is deterministic.
**Con**: npm runs twice if TypeScript changed.

**Option B**: Only build stage runs npm. Publish stage just copies artifacts.

**Con**: dotnet publish build targets would fail (can't run npm).

**Chosen**: Option A (current) — allows flexible npm changes during publish, acceptable build time.

### Why Scripts Packaged, Not Checked Out on Deploy Agent?

**Option A (Current)**: Package scripts into artifact.

**Pro**: Deploy agent doesn't need source checkout, reduced complexity.
**Con**: Scripts updated only if artifact regenerated.

**Option B**: Deploy agent checks out sources, reads scripts from checkout.

**Pro**: Latest scripts always used.
**Con**: Deploy agent needs .gitignore, credential handling, disk space.

**Chosen**: Option A (current) — simpler cross-machine deployment, scripts rarely change.

### Why Separate Analyze Stage?

**Option A (Current)**: Run analyzers in separate, non-blocking stage.

**Pro**: Warnings don't block deployment, trend tracking easier, can set warnings/errors independently.
**Con**: More complex pipeline, may hide issues.

**Option B**: Run analyzers in Test stage, fail on any warning.

**Pro**: Simpler, forced remediation.
**Con**: Can block valid deploys on style warnings.

**Chosen**: Option A (current) — pragmatic: allows iteration while tracking code quality trends.

## Troubleshooting

### Build Stage Fails

**Problem**: "npm not found"

```
Error: npm: The term 'npm' is not recognized
```

**Cause**: npmPath variable not set or incorrect

**Fix**:
1. Open [DevOps Automation Accounts variable group](https://quantum:8080/tfs/DefaultCollection/_library?itemType=VariableGroups)
2. Verify `npmPath` is set (e.g., `C:\Users\...\AppData\Local\nvm4w\v20.x.x\npm.cmd`)
3. Test on agent: `npm --version`
4. If not found, install nvm4w on build agent machine

### Test Stage Fails

**Problem**: "Connection string 'PtoTrackDbContext' is missing"

**Cause**: Integration tests need connection string in appsettings.json

**Fix**: Tests should use in-memory database (TestHost configuration). See [Testing Architecture](../test/TESTING-ARCHITECTURE.md).

### Publish Stage Fails

**Problem**: "Project file does not exist: \*/pto.track.csproj"

**Cause**: Glob pattern `**/pto.track.csproj` not resolved before passing to dotnet

**Fix**: Stage now resolves glob with `Get-ChildItem` before passing to `dotnet publish`.

### Deploy Stage Fails

**Problem**: "Access Denied" copying files to `C:\inetpub\wwwroot\pto-track\`

**Cause**: Deploy agent identity lacks write permissions on AppPool folder

**Fix**:
```powershell
icacls C:\inetpub\wwwroot\pto-track /grant "QUANTUM\s-webdev-agent:(RW)" /t
```

**Problem**: Health check times out after deploy

**Cause**: AppPool still starting, or endpoint not responding

**Fix**:
1. Check `C:\inetpub\wwwroot\pto-track\logs\stdout_*.log` for exceptions
2. Verify connection string injected correctly in `web.config`
3. Increase health check timeout (default 30s)

---

**See also**:
- [Service Accounts](SERVICE-ACCOUNTS.md) — Agent, AppPool, and build account details
- [Deployment Architecture](ARCHITECTURE.md) — IIS model and configuration
- [Testing Architecture](../test/TESTING-ARCHITECTURE.md) — Test strategy and in-memory DB
