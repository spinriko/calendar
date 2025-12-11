````markdown
# Runbook — Run & Debug Locally

This runbook helps developers run the app locally, run tests, run/inspect analyzers, and troubleshoot common JS manifest and bundling problems.

**Quick overview**
- App: ASP.NET Core app in `pto.track/` (solution `pto.track.sln`).
- Frontend assets: built into `pto.track/wwwroot/dist/` and fingerprinted. Manifest: `pto.track/wwwroot/dist/asset-manifest.json`.
- Vendor libs: `pto.track/wwwroot/lib/` (DayPilot lives at `lib/daypilot/daypilot-all.min.js`).

**Run the application (dev)**
- Restore and run the app with watch (recommended during development):

```powershell
pwsh -NoProfile -Command "dotnet restore; dotnet watch run --project pto.track/pto.track.csproj"
```

- Open browser at `https://localhost:5001` (or address printed by the run output).

**Build for production (no watch)**

```powershell
pwsh -NoProfile -Command "dotnet build pto.track.sln -c Release"
pwsh -NoProfile -Command "dotnet publish pto.track/pto.track.csproj -c Release -o ./publish --no-self-contained"
```

When to run `npm`/frontend build
- If you change any TypeScript, SCSS, or bundling configuration, run the frontend build to regenerate `wwwroot/dist/` and the `asset-manifest.json`.
- Typical local workflow when editing frontend code:
  - Run your frontend toolchain (project uses a simple build that outputs to `wwwroot/dist/`). If the repo has an npm script use it — otherwise run whatever build step exists in `package.json`.

Example (common patterns):
```powershell
pwsh -NoProfile -Command "npm install --prefix pto.track && npm run build --prefix pto.track"
```

If you are uncertain whether frontend assets are stale, rebuild them and restart the app.

**Running tests**

1) .NET tests

Run all .NET tests (optionally disable analyzers if they hang):

```powershell
pwsh -NoProfile -Command "dotnet test pto.track.services.tests/pto.track.services.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal"
pwsh -NoProfile -Command "dotnet test pto.track.tests/pto.track.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal"
pwsh -NoProfile -Command "dotnet test pto.track.data.tests/pto.track.data.tests.csproj /p:RunAnalyzersDuringBuild=false -v minimal"
```

If analyzers are stable in CI or you want to run them locally, see the analyzer section below.

2) JavaScript tests

Project includes a JS test runner under `pto.track.tests.js/`.

```powershell
pwsh -NoProfile -Command "npm install --prefix pto.track.tests.js && npm test --prefix pto.track.tests.js"
```

Node requirement: The JavaScript test tooling requires Node.js 20 or greater. If you have an older Node version installed, install Node 20+ (we recommend using `nvm` / `nvm-windows` or your OS package manager). CI is configured to use Node 20.

Headless capture (Puppeteer) and fixtures
- The repo keeps sample headless capture fixtures in `responses/`. If you regenerate assets fingerprints, run the fixture-update script to keep fixtures in sync:

```powershell
pwsh ./pto.track/scripts/update-fixtures-from-manifest.js
```

**Analyzers — run separately (non-blocking)**
- We intentionally keep analyzers separate so they do not block functional test runs.
- Local helper: `scripts/run-analyzers.ps1` (dry-run by default). To run:

```powershell
pwsh ./scripts/run-analyzers.ps1 -Execute
```

- The script builds the solution with analyzers enabled and writes logs into `artifacts/analyzers/`.
- CI recommendation: run analyzers as a separate job/stage. Upload the analyzer logs as artifacts and gate PR merges on analyzer job success if desired.

**Debugging & troubleshooting**

1) If the site fails to load in the browser
- Open browser devtools (Console + Network). Look for:
  - 404s for files under `/dist/` — fingerprint mismatch
  - JS runtime errors like `DayPilot library not loaded` or `Unexpected token 'export'`

2) JS manifest / fingerprint issues
- Check `pto.track/wwwroot/dist/asset-manifest.json`. It maps logical names (e.g., `absences-scheduler.js`) to hashed files.
- If a page tries to load `absences-scheduler.js` but the manifest maps it to `absences-scheduler.3ae5d079.js`, ensure your page uses the manifest-aware loader (we include manifest-aware loaders on pages like `Pages/Groups.cshtml`).
- To debug: fetch the manifest in the browser console:

```js
fetch('/dist/asset-manifest.json', {cache: 'no-store'}).then(r=>r.json()).then(console.log)
```

- If manifest is missing or stale:
  - Re-run your frontend build to regenerate `wwwroot/dist` and the manifest.
  - If using a dev server, ensure the dev server writes output into `wwwroot/dist/` or proxy is configured correctly.

3) `Unexpected token 'export'` errors
- This happens when an ES module bundle is included via a classic `<script src="...">` tag. Use `type="module"` or a manifest-aware dynamic import loader to import ESM bundles.

4) DayPilot / vendor missing at runtime
- Some legacy modules expect a global `DayPilot`. If the bundle uses ESM or wraps DayPilot differently, ensure either:
  - The vendor `daypilot-all.min.js` is included before the module, or
  - The bundle exports and the module imports DayPilot as an import.

We currently include a short vendor fallback in `Pages/AbsencesScheduler.cshtml` to ensure a global `DayPilot` when needed.

5) Database / migration issues during tests
- Test projects use in-memory providers for most unit tests. If a test triggers EF migrations or attempts to open a SQL connection, check the test startup for environment-specific logic and ensure environment variables are set appropriately.

Useful troubleshooting commands

```powershell
# Clear and rebuild everything
pwsh -NoProfile -Command "git clean -fdx; dotnet restore; npm install --prefix pto.track; npm run build --prefix pto.track; dotnet build -c Release"

# Recreate asset manifest and update fixtures
pwsh -NoProfile -Command "npm run build --prefix pto.track; pwsh ./pto.track/scripts/update-fixtures-from-manifest.js"

# Run analyzers only (writes logs to artifacts/analyzers)
pwsh ./scripts/run-analyzers.ps1 -Execute
```

Tips for when things go awry
- If a runtime JS error references a hashed file name that doesn't exist, re-run the frontend build and inspect `asset-manifest.json`.
- If tests hang during `dotnet test`, try rerunning with `/p:RunAnalyzersDuringBuild=false`. Then run analyzers separately to capture their output.
- Keep `responses/` fixtures in sync with the manifest. Use `pto.track/scripts/update-fixtures-from-manifest.js` after a frontend rebuild.
- Consider adding a short note in PRs when frontend asset changes include fingerprint changes — reviewers should ensure runtime pages use the manifest-aware loader (or the server-side helper that resolves assets).

Where to find more
- Bundling details and reasoning: `docs/arch/BUNDLING.md`.
- Vendor/library policies: `docs/VENDOR.md`.
- Analyzer run guidance: `docs/run/ANALYZERS.md`.

If you'd like, I can add a small CLI helper (`Makefile` or `ps1` entry point) that implements the common flows above (build+fixtures, test, analyzers, clean). Want that next?
A developer helper script is provided at `scripts/dev.ps1` that implements the most common developer flows.

Using `scripts/dev.ps1`

Examples (dry-run unless `-Execute` is provided for destructive/long-running commands):

```powershell
# Show help
pwsh ./scripts/dev.ps1 help

# Run frontend build only
pwsh ./scripts/dev.ps1 frontend

# Update fixtures from manifest
pwsh ./scripts/dev.ps1 fixtures

# Build solution (Debug)
pwsh ./scripts/dev.ps1 build

# Run the web app with dotnet watch
pwsh ./scripts/dev.ps1 run

# Run tests (safe default: analyzers disabled)
pwsh ./scripts/dev.ps1 test

# Run analyzers (requires -Execute to run analyzers build)
pwsh ./scripts/dev.ps1 analyzers -Execute

# Clean workspace (destructive; requires -Execute)
pwsh ./scripts/dev.ps1 clean -Execute

# Full flow: frontend -> fixtures -> build -> tests
pwsh ./scripts/dev.ps1 all
```

The helper wraps the same commands described above and is intended as a convenience for common developer tasks.

## VS Code — Testing & Debugging

If you use Visual Studio Code, the editor provides a convenient Test Explorer UI, debug integration, and keyboard shortcuts. The key items below collect the most useful VS Code-specific tips so you don't have to keep a separate file.

- **Recommended extensions**:
  - `ms-dotnettools.csharp` (C# Dev Kit / OmniSharp)
  - `formulahendry.dotnet-test-explorer` (Test Explorer integration)
  - `hbenl.vscode-test-explorer` (Test Explorer UI enhancements)

- **Test Explorer quickstart**:
  - Open the Test Explorer (beaker icon) to discover tests from the three test projects.
  - Run tests using the ▶️ controls beside a project, class, or individual test.
  - Debug tests using the 🐛 icon next to a test (sets breakpoints and attaches the debugger).

- **Useful keyboard shortcuts**:
  - `Ctrl+Shift+P` → `Test: Run All Tests`
  - `Ctrl+Shift+P` → `Test: Run Test at Cursor`
  - `Ctrl+Shift+P` → `Test: Debug Test at Cursor`
  - `F5` to start debugging the app; `Ctrl+F5` to start without debugger

- **Command Palette / Tasks**:
  - `Tasks: Run Task` → choose `build`, `test`, `watch`, or `publish` (these map to the tasks in the workspace).
  - Use `Test: Show Output` from the command palette to view the `.NET Test Log` channel.

- **.vscode config tips**:
  - `launch.json` should include a `preLaunchTask` that runs the `build` task so the app compiles before hitting breakpoints.
  - `tasks.json` can be used to expose the existing workspace `build`/`test` tasks to the Command Palette.
  - Settings snippet you may find useful:

```json
{
  "dotnet-test-explorer.testProjectPath": "**/*tests.csproj",
  "dotnet-test-explorer.enableTelemetry": false
}
```

- **VS Code troubleshooting**:
  - If tests don't appear: `Ctrl+Shift+P` → `Developer: Reload Window`, ensure the Test Explorer extension is enabled, and build the solution.
  - If the debugger doesn't stop at breakpoints: make sure you launched the test with the debug action (🐛), the code is built in `Debug` configuration, and `launch.json` has the correct `program`/`cwd`.
  - If OmniSharp is misbehaving: `Ctrl+Shift+P` → `OmniSharp: Restart OmniSharp`.

- **Terminal commands (from VS Code terminal)**
  - Run all tests:

```powershell
dotnet test
```

  - Run a single project:

```powershell
dotnet test pto.track.tests/pto.track.tests.csproj
```

The VS Code content previously lived in a separate `TESTING_VSCODE.md` file; it has been consolidated here to keep run/debug guidance in one place.

Artifacts produced by local runs
- **Analyzer logs**: `artifacts/analyzers/` — plaintext logs and SARIF (if diagnostics present). Use `pwsh ./scripts/run-analyzers.ps1 -Execute` to run analyzers locally; the script writes both a `.log` and a `.sarif` (or an empty SARIF skeleton) into that folder.
- **Code metrics**: `artifacts/metrics/` — the `CodeMetricsAnalyzer` tests write two files when run:
  - `code-metrics.json` — full machine-readable metrics (cyclomatic complexity, maintainability index, LOC, etc.)
  - `code-metrics-summary.json` — compact KPI summary useful for CI gating

To generate metrics locally (runs the analyzer-style tests that compute metrics):

```powershell
# Run the metrics tests (writes artifacts/metrics/*.json)
dotnet test pto.track.tests/pto.track.tests.csproj -c Release
```

These artifacts are suitable for uploading as CI build artifacts and for automated gating.

Per-project metrics
- The `code-metrics.json` output now includes a top-level `Projects` object containing per-project metrics keyed by project name (the csproj filename without extension).
- Each project entry contains the same metrics as the solution summary (Files, Lines, Classes, Methods, CyclomaticComplexity, Maintainability, Parameters). This makes it straightforward to implement per-project gates or track trends per package.
- Note: we intentionally do not enforce strict gates yet. Some projects' current averages (e.g. maintainability) are below conservative thresholds — raise the thresholds to appropriate values for your team before converting these metrics into blocking CI gates.

Quick PowerShell snippet — show per-project average maintainability:

```powershell
Get-Content artifacts/metrics/code-metrics.json -Raw |
  ConvertFrom-Json | Select-Object -ExpandProperty Projects | ConvertTo-Json -Depth 5

# Or a compact table view:
($p = (Get-Content artifacts/metrics/code-metrics.json -Raw | ConvertFrom-Json).Projects) |
  Get-Member -MemberType NoteProperty | ForEach-Object { $name = $_.Name; $avg = $p.$name.Maintainability.Average; '{0,-30} {1,6:N1}' -f $name, $avg }
```

Recommended next step before gating
- Review per-project `AvgMaintainability` and `AvgCyclomatic` values in `code-metrics-summary.json` and pick conservative thresholds that match your desired pace (for example, start with permissive thresholds and tighten gradually).
- Once thresholds are chosen, implement CI gating in the `Analyzers` stage (see `docs/run/RUN-CI.md`) and fail the job if any project exceeds the thresholds. I can help generate a small script to evaluate `code-metrics-summary.json` and return a non-zero exit code when thresholds are exceeded.

````
