# Run docs index

This folder contains the runbooks and developer guidance for running, testing, and troubleshooting the project.

- RUN-LOCAL.md — Local developer runbook (how to run, test, analyze, and debug locally).
- ANALYZERS.md — Guidance and script for running Roslyn analyzers and capturing logs.
- PR-CHECKLIST.md — Short checklist to follow before opening a pull request.

- Metrics artifact consumption
- ---------------------------
- When the CI `Analyzers` stage runs the `metrics-runner` console it publishes
- `artifacts/metrics/metrics.json` as a pipeline artifact named `metrics`.
- Below are examples for consuming that artifact in downstream Azure Pipelines
- stages and for inspecting it locally.

- Azure Pipelines (downstream job):

	- task: DownloadPipelineArtifact@2
		inputs:
			buildType: 'current'
			artifact: 'metrics'
			targetPath: '$(Pipeline.Workspace)/metrics'

	- powershell: |
			pwsh -NoProfile -Command {
				$metrics = Get-Content "$(Pipeline.Workspace)/metrics/artifacts/metrics/metrics.json" | ConvertFrom-Json
				Write-Host "Metrics generatedAt: $($metrics.generatedAt)"
				# Example: fail if aggregatedCyclomatic exceeds threshold
				if ($metrics.aggregatedCyclomatic -gt 1000) { throw "Aggregated cyclomatic complexity too high: $($metrics.aggregatedCyclomatic)" }
			}
		displayName: 'Consume metrics artifact and evaluate thresholds'

- Inspecting locally (developer machine):

	- Build & run the metrics console locally and inspect `artifacts/metrics/metrics.json`:

```pwsh
dotnet build tools/metrics-runner/metrics-runner.csproj -c Release
dotnet run --project tools/metrics-runner -- "C:\path\to\repo"
Get-Content .\artifacts\metrics\metrics.json | ConvertFrom-Json | Select-Object generatedAt, projectCount, totalFiles, aggregatedCyclomatic
```

	- Quick tip: use `ConvertFrom-Json` in PowerShell or `jq` to query the JSON and extract the top offenders.

Recommended flow:

1. Read `RUN-LOCAL.md` for local run & test steps.
2. Run `pwsh ./scripts/dev.ps1` helper for common flows.
3. Run analyzers with `pwsh ./scripts/run-analyzers.ps1 -Execute` (or via CI as described in `RUN-CI.md`).
4. Follow `PR-CHECKLIST.md` before opening a PR.
