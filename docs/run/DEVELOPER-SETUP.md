# Developer Environment Setup

This guide helps you set up PTO Track for local development, including .NET, Node.js, database, and debugging.

## System Requirements

### Minimum

- **OS**: Windows 10/11 or Windows Server 2019+
- **.NET SDK**: 8.0.100 or later (locked in `global.json`)
- **Node.js**: 20.x LTS (for frontend build)
- **Database**: SQL Server 2019+ or SQL Server Express
- **RAM**: 8 GB (recommended 16 GB)
- **Disk**: 20 GB free (includes .NET SDK, node_modules, database)

### Recommended Tools

- **IDE**: Visual Studio 2022 or VS Code with C# extension
- **PowerShell**: pwsh (PowerShell 7+) or Windows PowerShell 5.1
- **Git**: Git for Windows
- **Node Version Manager**: `nvm-windows` or `nvm4w` (for Node 20 if needed)
- **SQL Client**: SQL Server Management Studio (SSMS) or SQL Operations Studio

## Installation Steps

### 1. Clone the Repository

```powershell
git clone https://dev.azure.com/quantum/DefaultCollection/ASB/_git/pto-track
cd pto-track
```

### 2. Install .NET SDK 8.0

Verify global.json requires version 8.0.100:

```powershell
cat global.json

# Output should show:
# {
#   "sdk": {
#     "version": "8.0.100",
#     "rollForward": "latestFeature"
#   }
# }
```

**Windows**:
- Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- Or use Chocolatey: `choco install dotnet-sdk`
- Or use Windows Package Manager: `winget install Microsoft.DotNet.SDK.8`

**Verify**:
```powershell
dotnet --version       # Should be 8.0.100 or later
dotnet --info          # List all SDKs installed
```

### 3. Install Node.js 20

**Option A: Direct Download**
- Visit [https://nodejs.org](https://nodejs.org)
- Download Node 20 LTS
- Run installer (adds to PATH)

**Option B: nvm-windows (Recommended for Multiple Versions)**
- Download nvm-windows installer: [https://github.com/coreybutler/nvm-windows](https://github.com/coreybutler/nvm-windows)
- Install nvm, then:
  ```powershell
  nvm install 20.0.0
  nvm use 20.0.0
  ```

**Option C: Package Manager**
```powershell
# Chocolatey
choco install nodejs

# Windows Package Manager
winget install OpenJS.NodeJS
```

**Verify**:
```powershell
node --version         # Should be v20.x.x
npm --version          # Should be 10.x.x or later
```

### 4. Install SQL Server (Local Development)

**Option A: SQL Server Express** (free, lightweight)
- Download from [https://www.microsoft.com/en-us/sql-server/sql-server-express](https://www.microsoft.com/en-us/sql-server/sql-server-express)
- Install with default settings
- Includes LocalDB (file-based database for dev)
- Instance name: `SQLEXPRESS` or `(LocalDB)\MSSQLLocalDB`

**Option B: Docker Container** (isolated, clean setup)
```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123!" `
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

**Option C: Corp SQL Server** (shared dev instance)
- Contact your DBA for dev database server details
- Get connection string: `Server=CORP_SQL_SERVER,1433;Database=pto_track_dev;...`

### 5. Create Local Database

**Using SQL Server Management Studio (SSMS)**:

1. Connect to SQL Server instance (e.g., `localhost\SQLEXPRESS`)
2. Right-click **Databases** → **New Database**
3. Name: `pto_track_dev`
4. Click **OK**

**Or PowerShell**:
```powershell
$connectionString = "Server=(LocalDB)\MSSQLLocalDB;Integrated Security=True;"
$query = "CREATE DATABASE pto_track_dev;"

Invoke-Sqlcmd -ConnectionString $connectionString -Query $query
```

### 6. Configure User Secrets

User secrets store sensitive configuration locally (not in repo):

```powershell
cd pto.track

# Create user secrets for this project
dotnet user-secrets init --project .

# Add connection string
dotnet user-secrets set "ConnectionStrings:PtoTrackDbContext" `
  "Server=(LocalDB)\MSSQLLocalDB;Database=pto_track_dev;Integrated Security=True;TrustServerCertificate=True;"

# View secrets (to verify)
dotnet user-secrets list

# Output:
# ConnectionStrings:PtoTrackDbContext = Server=...
```

**Location**: Secrets stored in:
```
Windows: %APPDATA%\Microsoft\UserSecrets\<project-id>\secrets.json
macOS/Linux: ~/.microsoft/usersecrets/<project-id>/secrets.json
```

### 7. Create Local Settings File (Optional)

For non-secret configuration overrides, create `appsettings.local.json`:

```powershell
cd pto.track

# Copy example
Copy-Item appsettings.local.json.example appsettings.local.json

# Edit to match your local environment
notepad appsettings.local.json
```

**Example appsettings.local.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

### 8. Run Database Migrations

EF Core migrations run automatically on app startup (dev environment):

```powershell
# Just run the app; migrations happen automatically
dotnet run --project pto.track/pto.track.csproj
```

**Manual migration** (if needed):
```powershell
dotnet ef database update --project pto.track.data/pto.track.data.csproj --startup-project pto.track/pto.track.csproj
```

## Running the Application Locally

### Development Mode (with Watch)

**Recommended for active development** — auto-restarts on file changes:

```powershell
cd pto-track

# Restore packages
dotnet restore

# Run with watch (rebuilds on save)
dotnet watch run --project pto.track/pto.track.csproj

# Output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:5001
#       Now listening on: http://localhost:5000
```

Open browser: `https://localhost:5001`

**Stop**: Press `Ctrl+C`

### Production-Like Mode (No Watch)

```powershell
# Build for Release
dotnet build pto.track.sln -c Release

# Run without watch (single invocation)
dotnet run --project pto.track/pto.track.csproj --no-build -c Release

# Open: https://localhost:5001
```

## Frontend Development

### Rebuild JS/CSS Bundles

If you edit TypeScript, SCSS, or bundling config, regenerate frontend assets:

```powershell
cd pto.track

# Install dependencies (one-time)
npm ci

# Build bundles (TypeScript → JavaScript)
npm run build:js

# Output: wwwroot/dist/ + updated asset-manifest.json
```

### Webpack Watch Mode (if using Webpack)

```powershell
npm run watch:js        # Auto-rebuild on .ts/.scss changes
```

### Update Test Fixtures

If you regenerate asset manifest (hashed filenames), update test fixtures:

```powershell
pwsh ./pto.track/scripts/update-fixtures-from-manifest.js
```

## Running Tests

### .NET Tests

**All tests**:
```powershell
dotnet test pto.track.sln
```

**Single project**:
```powershell
dotnet test pto.track.services.tests/pto.track.services.tests.csproj
dotnet test pto.track.tests/pto.track.tests.csproj
dotnet test pto.track.data.tests/pto.track.data.tests.csproj
```

**With coverage**:
```powershell
dotnet test pto.track.sln /p:CollectCoverage=true /p:CoverageFormat=opencover
```

**Disable analyzers** (faster iteration):
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet test pto.track.sln /p:RunAnalyzersDuringBuild=false
```

### JavaScript Tests

```powershell
cd pto.track.tests.js

npm ci       # Install dependencies
npm test     # Run Jest tests + ESLint
```

### Run All Tests (CI-Like)

```powershell
pwsh ./run-all-tests.ps1
```

Runs:
1. .NET tests (Services, Tests, Data)
2. JS tests (Jest)
3. Analyzer report
4. Metrics report

## Debugging

### Visual Studio 2022

1. **Open Solution**:
   ```powershell
   start pto.track.sln
   ```

2. **Set Breakpoint**: Click left margin in code editor

3. **Run Debugger**: Press `F5` or **Debug → Start Debugging**

4. **Controls**:
   - `F5` — Continue
   - `F10` — Step over
   - `F11` — Step into
   - `Shift+F11` — Step out
   - `Shift+F5` — Stop

### VS Code

**Setup** (one-time):

1. Install C# extension: [ms-dotnettools.csharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
2. Open workspace:
   ```powershell
   code pto.track.code-workspace
   ```

**Debug**:

1. Create `.vscode/launch.json`:
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": ".NET Core Launch (web)",
         "type": "coreclr",
         "request": "launch",
         "preLaunchTask": "build",
         "program": "${workspaceFolder}/pto.track/bin/Debug/net8.0/pto.track.dll",
         "args": [],
         "cwd": "${workspaceFolder}/pto.track",
         "stopAtEntry": false,
         "env": {
           "ASPNETCORE_ENVIRONMENT": "Development"
         }
       }
     ]
   }
   ```

2. Set breakpoint (click left margin)

3. Press `F5` to debug

### IIS Express (Local IIS)

For debugging with IIS Express (closer to prod):

```powershell
# Install IIS Express (usually with Visual Studio)
# Then run:
dotnet run --launch-profile https
```

## Troubleshooting

### Problem: "Connection string 'PtoTrackDbContext' is missing"

**Cause**: User secrets not initialized

**Fix**:
```powershell
cd pto.track
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:PtoTrackDbContext" "Server=...;Database=pto_track_dev;..."
dotnet user-secrets list
```

### Problem: Database migrations fail on startup

**Cause**: Either DB doesn't exist or connection string is wrong

**Fix**:
```powershell
# Verify database exists
sqlcmd -S (LocalDB)\MSSQLLocalDB -Q "SELECT name FROM sys.databases WHERE name = 'pto_track_dev';"

# If not found, create it:
sqlcmd -S (LocalDB)\MSSQLLocalDB -Q "CREATE DATABASE pto_track_dev;"

# Manually run migration:
dotnet ef database update --project pto.track.data/
```

### Problem: npm not found; TypeScript build fails

**Cause**: Node.js not in PATH

**Fix**:
```powershell
# Verify Node is installed
node --version
npm --version

# If not found, add to PATH or reinstall Node
```

### Problem: Port 5001 already in use

**Cause**: Another process listening on the port

**Fix**:
```powershell
# Find process using port 5001
netstat -ano | Select-String ":5001"

# Kill process (replace PID):
Stop-Process -Id <PID> -Force

# Or use different port:
dotnet run --project pto.track/pto.track.csproj -- --urls "https://localhost:5002"
```

### Problem: Slow test runs; TestHost lifecycle issues

**Cause**: Analyzer runs in test project, or parallel test isolation issue

**Fix**:
```powershell
# Skip analyzers
dotnet test pto.track.sln /p:RunAnalyzersDuringBuild=false

# Run tests serially (slower but safer)
dotnet test pto.track.sln -p:RunAnalyzersDuringBuild=false --no-parallel
```

## Performance Tips

1. **Use SSD** for repo and `node_modules` (faster npm restore)
2. **Exclude from antivirus**: `bin/`, `obj/`, `node_modules/` folders
3. **Reduce logging**: Set `LogLevel` to `Warning` in development
4. **Build once, test many**: Run tests without rebuilding:
   ```powershell
   dotnet test --no-build
   ```
5. **Use dotnet watch**: Auto-recompile on save (faster iteration)

## Environment Variables

### Development

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"    # Enables hot reload, dev pages
$env:ASPNETCORE_URLS = "https://localhost:5001" # Custom URL
```

### Testing

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"        # Uses in-memory DB, mock auth
```

### Production

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"     # No dev features, no hot reload
$env:ASPNETCORE_URLS = "https://0.0.0.0:443"   # Listen on all interfaces
```

## Next Steps

1. ✅ Clone repo
2. ✅ Install .NET 8 + Node 20
3. ✅ Create local database
4. ✅ Set user secrets (connection string)
5. ✅ Run `dotnet run` and open browser
6. ✅ Run tests: `dotnet test`
7. ✅ Build frontend: `npm ci && npm run build:js`

**Common workflows**:

- **Make feature**: Edit code → `dotnet watch run` (auto-reload) → test in browser
- **Fix bug**: Edit code → Run relevant test → `dotnet watch run` to verify
- **Update frontend**: Edit TypeScript → `npm run build:js` → `dotnet run`
- **Deploy locally**: `dotnet publish -c Release -o ./publish` → test exe

---

**See also**:
- [Run Local Runbook](RUN-LOCAL.md) — Detailed development guide
- [Testing Architecture](../test/TESTING-ARCHITECTURE.md) — Test strategy and mocking
- [Service Accounts](../deploy/SERVICE-ACCOUNTS.md) — Dev database access setup
