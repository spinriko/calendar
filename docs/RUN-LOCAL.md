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
- Analyzer run guidance: `docs/ANALYZERS.md`.

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
