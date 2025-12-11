````markdown
# CI Gates — Future Work

This page collects recommended patterns for gating builds based on code metrics and other quality signals. It's intended as a short, copy-pasteable reference for CI engineers who will implement gates in your pipeline (Azure DevOps, Jenkins, GitLab, etc.).

Goals
- Run fast, deterministic checks that fail the pipeline when clearly-defined quality thresholds are exceeded.
- Keep heavy analysis (Roslyn full-file scans) out of the fast PR validation pipeline — run them in nightly or dedicated `Analyzers` stages.
- Provide a small, auditable script that reads metrics produced by `tools/metrics-runner` and returns a non-zero exit code when thresholds are crossed.

Overview
- The repository provides a console metrics tool at `tools/metrics-runner` which writes `artifacts/metrics/metrics.json` when executed.
- CI should run `tools/metrics-runner` in a dedicated job (see `docs/run/RUN-CI.md`), publish `artifacts/metrics` as a pipeline artifact, and then consume that artifact in downstream gating jobs.

Example `evaluate-metrics.ps1` (suggested)
- Create `scripts/evaluate-metrics.ps1` that reads `artifacts/metrics/metrics.json`, applies configurable thresholds, and returns non-zero when a threshold is exceeded. Example minimal script:

```powershell
param(
  [string]$MetricsPath = "artifacts/metrics/metrics.json",
  [int]$MaxAggregatedCyclomatic = 1200,
  [int]$MaxFilesAboveCyclomatic = 10
)

if (-not (Test-Path $MetricsPath)) {
  Write-Error "Metrics file not found: $MetricsPath"
  exit 2
}

$metrics = Get-Content $MetricsPath | ConvertFrom-Json

# Example checks
if ($metrics.aggregatedCyclomatic -gt $MaxAggregatedCyclomatic) {
  Write-Error "Aggregated cyclomatic complexity $($metrics.aggregatedCyclomatic) exceeds threshold $MaxAggregatedCyclomatic"
  exit 1
}

# Count files with cyclomatic higher than 20
$badFiles = $metrics.files | Where-Object { $_.cyclomatic -gt 20 }
if ($badFiles.Count -gt $MaxFilesAboveCyclomatic) {
  Write-Error "Files with cyclomatic >20: $($badFiles.Count) (threshold: $MaxFilesAboveCyclomatic)"
  exit 1
}

Write-Host "Metrics OK: aggregatedCyclomatic=$($metrics.aggregatedCyclomatic); high-complex files=$($badFiles.Count)";
exit 0
```

Azure Pipelines gating job (example)
- This job runs after the `Analyzers` stage that published the `metrics` artifact. It downloads the artifact, runs `evaluate-metrics.ps1`, and will fail the job when thresholds are breached.

```yaml
- job: MetricsGate
  displayName: Evaluate Code Metrics
  pool: { vmImage: 'windows-latest' }
  dependsOn: Analyzers
  steps:
    - task: DownloadPipelineArtifact@2
      inputs:
        buildType: 'current'
        artifact: 'metrics'
        targetPath: '$(Pipeline.Workspace)/metrics'

    - powershell: |
        pwsh -NoProfile -Command {
          $metricsJson = "$(Pipeline.Workspace)/metrics/artifacts/metrics/metrics.json"
          pwsh ./scripts/evaluate-metrics.ps1 -MetricsPath $metricsJson -MaxAggregatedCyclomatic 1200 -MaxFilesAboveCyclomatic 10
        }
      displayName: 'Evaluate metrics and gate'
```

Guidance & best practices
- Keep thresholds conservative initially — prefer advisories/warnings before hard fails.
- Report useful context in the script output: list top 5 offenders (path + cyclomatic + lines) so reviewers can triage quickly.
- Avoid running heavy Roslyn analysis in the gate job — use the lightweight `metrics-runner` for fast checks; run full analyses in nightly jobs.
- Make thresholds configurable via pipeline variables or PR labels so teams can opt-in/out while ramping the gate.

Next steps (suggested)
- Add `scripts/evaluate-metrics.ps1` as an optional script (I can scaffold it for you when you're ready).
- Extend the script to emit a compact `code-metrics-summary.json` with top offenders and a human-readable markdown snippet to attach as a pipeline summary.

Notes
- This document is guidance for implementers. The exact thresholds and gating strategy should be chosen by the team owning the repository to match appetite for enforcement vs. developer friction.

````
