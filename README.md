# PTO Track - Resource Scheduling Calendar

![Version](docs/badges/version.png)
[![Build Status](https://quantum:8080/tfs/DefaultCollection/ASB/_build/badge/1?style=flat)](https://quantum:8080/tfs/DefaultCollection/ASB/_build)

**PTO Track** is an ASP.NET Core 8.0 web application for resource scheduling and paid-time-off (PTO) tracking. Built with clean architecture principles, it features an interactive drag-and-drop scheduler interface powered by DayPilot for managing employee absences and time-off requests.

## Quick Start

### Prerequisites
- **.NET SDK 8.0** or later (see `global.json`)
- **Node.js 20** LTS (for frontend build)
- **SQL Server** (Express or full edition)

### Clone & Run

```powershell
git clone <repo-url>
cd pto-track

# Restore + build
dotnet restore
dotnet build

# Run locally (development)
dotnet watch run --project pto.track/pto.track.csproj

# Open: https://localhost:5001
```

See [Developer Setup Guide](docs/run/DEVELOPER-SETUP.md) for detailed instructions (database config, user secrets, Node setup).

## Key Features

- 🗓️ **Absence Scheduler**: Drag-and-drop interface for requesting and approving PTO
- 🔐 **Role-Based Access**: Employee, Manager, and Admin views with proper authorization
- 🔄 **Approval Workflow**: Pending → Approved/Rejected → Executed/Cancelled states
- 🪟 **Windows Authentication**: Integrated with Active Directory
- 🏗️ **Clean Architecture**: Clear separation (Web → Services → Data)

## Project Layout

```
pto.track/              ← Web layer (Razor Pages + API controllers)
pto.track.services/     ← Business logic (services, DTOs, unit tests)
pto.track.data/         ← Data access (EF Core, entities, migrations)
pto.track.tests/        ← Integration tests (TestHost, in-memory DB)
pto.track.tests.js/     ← Frontend tests (Jest)
docs/                   ← Architecture & runbooks
├── run/                ← Developer guides (RUN-LOCAL.md, DEVELOPER-SETUP.md)
├── deploy/             ← Deployment docs (ARCHITECTURE.md, SERVICE-ACCOUNTS.md)
├── ci/                 ← Pipeline overview (PIPELINE-OVERVIEW.md)
└── test/               ← Testing strategy (TESTING-ARCHITECTURE.md)
```

## Development

### Run Tests

```powershell
# All tests
dotnet test pto.track.sln

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Frontend tests
cd pto.track.tests.js && npm ci && npm test
```

### Run Analyzers

```powershell
pwsh ./scripts/run-analyzers.ps1 -Execute
```

### Build Frontend

```powershell
cd pto.track
npm ci
npm run build:js
```

## Deployment

PTO Track is deployed to **IIS in-process** on Windows Server:

```powershell
# Publish as self-contained (win-x64)
dotnet publish pto.track/pto.track.csproj -c Release -o ./publish `
  --runtime win-x64 --self-contained

# Copy to: C:\inetpub\wwwroot\pto-track\
```

See [Deployment Architecture](docs/deploy/ARCHITECTURE.md) for full IIS setup and [Service Accounts](docs/deploy/SERVICE-ACCOUNTS.md) for identity/permission details.

## CI/CD Pipeline

Azure Pipelines stages:

1. **Build**: Restore → npm build:js → dotnet build
2. **Test**: Run .NET + frontend tests, collect coverage
3. **Analyze**: Roslyn analyzers, code metrics (non-blocking)
4. **Publish**: Self-contained app + deployment scripts
5. **Deploy**: Copy to IIS, set configuration, health check

See [Pipeline Overview](docs/ci/PIPELINE-OVERVIEW.md) for detailed stage breakdown and variable groups.

## Documentation

| Guide | Purpose |
|-------|---------|
| [Developer Setup](docs/run/DEVELOPER-SETUP.md) | Local dev environment checklist |
| [Run Local](docs/run/RUN-LOCAL.md) | Detailed developer runbook |
| [Testing Architecture](docs/test/TESTING-ARCHITECTURE.md) | Unit + integration tests, mocking, in-memory DB |
| [Deployment Architecture](docs/deploy/ARCHITECTURE.md) | IIS in-process model, config, logs |
| [Service Accounts](docs/deploy/SERVICE-ACCOUNTS.md) | Identity matrix, permissions, SQL setup |
| [Pipeline Overview](docs/ci/PIPELINE-OVERVIEW.md) | Build stages, artifact flow, variables |

## Useful Scripts

| Script | Purpose |
|--------|---------|
| `scripts/run-analyzers.ps1` | Run Roslyn analyzers, produce SARIF |
| `scripts/admin/Start-ADOServices.ps1` | Start SQL, WAS, W3SVC, agent service |
| `scripts/admin/Stop-ADOServices.ps1` | Gracefully stop services |
| `scripts/admin/Cleanup-AgentCaches.ps1` | Clean old agent workspaces |
| `pto.track/scripts/update-fixtures-from-manifest.js` | Sync test fixtures to asset manifest |

## Contributing

1. Create feature branch from `main`
2. Make changes, commit
3. Run tests locally: `dotnet test`
4. Run analyzers: `pwsh ./scripts/run-analyzers.ps1 -Execute`
5. Open PR against `main`

See each documentation link above for deeper details on architecture, testing, and deployment.

## License & Third-Party

- See root `LICENSE` file
- Third-party notices: `LicensesThirdParty/nuget.txt`

