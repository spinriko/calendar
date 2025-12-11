```markdown
# Tech Debt — Top Offenders (snapshot)

Generated: 2025-12-11T01:30:59Z

This report is an automated snapshot of code-metrics collected by `tools/metrics-runner`.  Use it as a running backlog of files to prioritize for complexity reduction and maintainability improvements. Regenerate by running:

```pwsh
dotnet run --project tools/metrics-runner -- "C:\code\dotnet\pto"
```

## Top 10 by Cyclomatic Complexity

| Rank | Path | Cyclomatic | Lines | Maintainability Index |
|---:|---|---:|---:|---:|
| 1 | `pto.track.tests/CodeMetricsAnalyzer.cs` | 133 | 824 | 18.50 |
| 2 | `pto.track/Controllers/AbsencesController.cs` | 50 | 356 | 37.62 |
| 3 | `pto.track.data.tests/UnitTest1.cs` | 36 | 519 | 35.93 |
| 4 | `pto.track.tests/IntegrationTests.cs` | 35 | 336 | 40.18 |
| 5 | `pto.track.services/ServiceCollectionExtensions.cs` | 33 | 187 | 46.00 |
| 6 | `pto.track.services.tests/AbsenceServiceTests.cs` | 29 | 904 | 31.61 |
| 7 | `pto.track/HostingExtensions.cs` | 27 | 216 | 45.44 |
| 8 | `pto.track.services/AbsenceService.cs` | 23 | 190 | 47.20 |
| 9 | `pto.track.tests/ImpersonationTests.cs` | 22 | 540 | 37.44 |
| 10 | `pto.track.services.tests/ResourceServiceTests.cs` | 21 | 422 | 39.91 |

## Top 10 by Lowest Maintainability Index

| Rank | Path | Maintainability Index | Cyclomatic | Lines |
|---:|---|---:|---:|---:|
| 1 | `pto.track.tests/CodeMetricsAnalyzer.cs` | 18.50 | 133 | 824 |
| 2 | `pto.track.services.tests/AbsenceServiceTests.cs` | 31.61 | 29 | 904 |
| 3 | `pto.track.data.tests/UnitTest1.cs` | 35.93 | 36 | 519 |
| 4 | `pto.track.tests/ImpersonationTests.cs` | 37.44 | 22 | 540 |
| 5 | `pto.track/Controllers/AbsencesController.cs` | 37.62 | 50 | 356 |
| 6 | `pto.track.services.tests/ResourceServiceTests.cs` | 39.91 | 21 | 422 |
| 7 | `pto.track.tests/IntegrationTests.cs` | 40.18 | 35 | 336 |
| 8 | `pto.track.tests/AbsencesAuthorizationTests.cs` | 40.57 | 19 | 405 |
| 9 | `pto.track.services.tests/EventServiceTests.cs` | 43.19 | 15 | 325 |
| 10 | `pto.track/HostingExtensions.cs` | 45.44 | 27 | 216 |

## Recommended next actions

- Triage: Decide whether to prioritize production code first (controllers, services, hosting extensions) or include tests in the initial cleanup.
- Start small: pick 1–2 files (for example `pto.track/Controllers/AbsencesController.cs` and `pto.track.services/ServiceCollectionExtensions.cs`) and extract obvious helper methods or services.
- Re-measure: After each change re-run `tools/metrics-runner` and update this file (commit a new snapshot) so the PR shows measurable improvement.

## Notes

- This snapshot was generated from `artifacts/metrics/metrics.json`. The metrics are approximate (Halstead/MI approximations) but sufficient for prioritization.
- Consider converting this snapshot into a tracked backlog (issue per file) as you pay down tech debt.

```