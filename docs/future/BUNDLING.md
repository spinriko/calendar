```markdown
# Bundling Plan — PTO Track

Status: Draft

Purpose
- Capture a concise, actionable plan for moving the project from SDK-managed Static Web Assets to a Single-Bundle strategy for frontend JS/CSS assets.
- Provide concrete commands, watch workflows, and roll-back guidance so the team can adopt bundling with minimal disruption.

Goals
- Eliminate fragile static web asset collisions and manifest errors observed during development.
- Produce deterministic, cache-friendly frontend bundles (JS/CSS) placed under `wwwroot/dist/`.
- Ensure correct MIME types and stable module loading (avoid browser module MIME failures).
- Keep DayPilot and other third-party libs loadable (either bundled or loaded separately in correct order).
- Keep local dev experience fast with watch-mode rebuilds and compatible `dotnet watch run` usage.

Recommended tooling
- esbuild (recommended): fast, easy to configure; supports bundling TS -> JS, produces source maps, has watch mode.
- Alternatives: Rollup, Vite, Webpack (heavier; use if you need advanced plugin ecosystem).

High-level approach
1. Add a small frontend build folder (we already have `pto.track.tests.js` with npm). Use `esbuild` there.
2. Build outputs go to `pto.track/wwwroot/dist/` (JS and CSS). Use distinct paths to avoid SDK fingerprint detection collisions.
3. Update Razor layout/pages to reference `/dist/*` assets (DayPilot first if kept separate). Prefer explicit filenames or hashed filenames for caching.
4. Run `npm run watch` in parallel with `dotnet watch run` during development.
5. Optionally disable SDK Static Web Assets for this project once bundling is stable (or ensure bundles live under `wwwroot/dist` so they don't collide with generated scoped assets).

Minimal file layout example
- pto.track/
  - wwwroot/
    - dist/
      - daypilot-all.min.js (or loaded from CDN)
      - absences-scheduler.bundle.js
      - impersonation-panel.bundle.js
      - site.bundle.css

esbuild example (quick commands)
1) Install (in `pto.track.tests.js` or repo root):
   npm install --save-dev esbuild

2) Single-shot build commands (PowerShell):
   npx esbuild ./pto.track/wwwroot/js/absences-scheduler.ts --bundle --outfile=./pto.track/wwwroot/dist/absences-scheduler.bundle.js --sourcemap --minify --target=es2020 --platform=browser
   npx esbuild ./pto.track/wwwroot/js/impersonation-panel.ts --bundle --outfile=./pto.track/wwwroot/dist/impersonation-panel.bundle.js --sourcemap --minify --target=es2020 --platform=browser

3) Recommended `package.json` scripts (inside `pto.track.tests.js`):
   {
     "scripts": {
       "build:js": "esbuild ../pto.track/wwwroot/js/*.ts --bundle --outdir=../pto.track/wwwroot/dist --minify --sourcemap --target=es2020 --platform=browser",
       "watch:js": "esbuild ../pto.track/wwwroot/js/*.ts --bundle --outdir=../pto.track/wwwroot/dist --sourcemap --watch"
     }
   }

Integration details
- Layout changes:
  - Replace SDK-generated fingerprint references with explicit `/dist/*` links in `Pages/_Layout.cshtml` or per-page head.
  - Example:
    - <script src="/dist/daypilot-all.min.js"></script>
    - <script type="module" src="/dist/absences-scheduler.bundle.js"></script>

- DayPilot handling:
  - If DayPilot is loaded as a global, include its script before your bundled module.
  - Alternatively, bundle DayPilot into the bundle (beware size and license), or keep it as a CDN script.

- Content-Type and MIME issues:
  - Serving files from `wwwroot/dist` using ASP.NET Core Static Files will set `Content-Type` correctly by extension, avoiding the module MIME error.
  - If you precompress (`.gz`) assets, ensure the server provides proper Content-Encoding headers (or avoid precompressing for local dev).

Dev workflow
- Start the frontend watcher:
  - In a terminal: `cd pto.track.tests.js` then `npm run watch:js` (or use `npx esbuild ... --watch`).
- Start the app with hot-reload:
  - `dotnet watch run --project pto.track/pto.track.csproj`
- Open `http://localhost:<port>/AbsencesScheduler` and verify DayPilot loads.

CI / Production
- Build step should run `npm run build:js` before `dotnet publish` so `wwwroot/dist` is populated.
- Optionally add hashed filenames (esbuild can write to hashed names via plugins or a small wrapper script) and update layout with generated names, or keep stable names and rely on cache control headers.

Rollout & rollback
- Rollout:
  1. Add bundling scripts and build outputs to `wwwroot/dist`.
  2. Update `_Layout.cshtml` to reference `dist` files.
  3. Run full local test and headless capture (we already have `headless-capture.mjs`).
  4. Merge and run CI builds that include `npm run build:js`.
- Rollback:
  - Revert `Pages/_Layout.cshtml` changes and remove `dist` references. Re-enable static web assets setup if it was disabled.

Risks & mitigations
- Risk: duplicate routes with SDK-managed static assets. Mitigation: use a unique folder (`/dist/`) for bundles or temporarily set `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>` until all pages are migrated.
- Risk: large bundle size hurting cold load. Mitigation: keep DayPilot separate (CDN or separate script), and split bundles by feature if needed.

Next steps (suggested)
1. Decide on bundler (esbuild recommended) — you are already leaning towards full bundling.
2. Add `esbuild` scripts to `pto.track.tests.js/package.json` and commit.
3. Update `_Layout.cshtml` to point to `/dist/*` assets (dev-only branch).
4. Run `npm run watch:js` + `dotnet watch run` and validate with `headless-capture.mjs`.

References
- esbuild: https://esbuild.github.io/
- ASP.NET Core Static Files: https://docs.microsoft.com/aspnet/core/fundamentals/static-files

Document author: planning note created by automation for the team — edit as needed.

```
