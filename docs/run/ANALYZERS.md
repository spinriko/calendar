````markdown
# Analyzer runner and CI guidance

This repository intentionally separates Roslyn/IDE analyzers from the functional test runs to avoid analyzers blocking or hanging the test pipeline.

Local script
- `scripts/run-analyzers.ps1` - PowerShell script that runs a build with analyzers enabled and writes a timestamped log to `artifacts/analyzers/`.

Usage (dry-run):

```powershell
pwsh ./scripts/run-analyzers.ps1
```

To actually run analyzers (executes `dotnet build` with analyzers enabled):

```powershell
pwsh ./scripts/run-analyzers.ps1 -Execute
```

Notes
- The script defaults to building the solution `pto.track.sln` in `Release` configuration.
- The script creates `artifacts/analyzers/` and saves a log file `analyzers-YYYYMMDD-HHMMSS.log`.

Recommended CI job (CI-agnostic)
- Run this as a separate job/stage that does not block the functional tests job.
- Example steps:
  - Restore NuGet packages
  - Checkout code
  - Run `pwsh ./scripts/run-analyzers.ps1 -Execute`
  - Upload the generated `artifacts/analyzers/*.log` as build artifacts

Running analyzers separately prevents intermittent analyzer hangs from slowing or failing the functional test jobs. Keep analyzer runs on PRs, nightly schedules, or as a gated quality gate depending on team preference.

````
