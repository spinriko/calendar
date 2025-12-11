# Bundling Plan — PTO Track

Status: **Complete** (as of December 11, 2025 — implemented in `feature/add-local-env` and merged to `feature/fixes`)

## Implementation Summary

- **Bundler:** esbuild is used for all frontend JS/TS assets.
- **Source:** All TypeScript sources are in `pto.track/wwwroot/js/`.
- **Output:** Bundled/minified JS is emitted to `pto.track/wwwroot/dist/` with cache-busting hashes.
- **Integration:** Razor pages reference `/dist/*.js` assets directly (see `Groups.cshtml`, `AbsencesScheduler.cshtml`).
- **Manifest:** An asset manifest is generated for dynamic module loading.
- **Watch/Build:** Bundling is run in parallel with .NET dev server; see npm scripts in `pto.track.tests.js` (historical, now handled by build scripts).
- **DayPilot:** Vendor JS is loaded separately as needed.

All goals in this plan have been implemented and are in active use.

---

*This document was moved from `docs/future/BUNDLING.md` after completion. For historical context, see the project changelog and PRs for `feature/add-local-env` and `feature/fixes`.*
