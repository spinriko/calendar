# Metrics Runner

Small console tool to produce basic code metrics (lines-of-code, file counts) per project.

Usage:

PowerShell:
```powershell
# from repo root
dotnet run --project tools/metrics-runner -- "..\.."
```

Outputs to `artifacts/metrics/metrics.json`.

This is intentionally lightweight — use it for fast, non-xunit metrics runs. For Roslyn-based
analysis (deeper metrics), extend this tool to invoke Microsoft.CodeAnalysis APIs or call
the existing analyzer code.
