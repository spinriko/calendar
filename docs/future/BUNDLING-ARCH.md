# Bundling — PTO Track (Implemented)

Status: Implemented (see notes)

Overview
- PTO Track moved from SDK-managed Static Web Assets to an explicit frontend bundling pipeline.
- Frontend artifacts are built into `wwwroot/dist/` and served by ASP.NET Core with explicit cache rules.

Why
- Eliminated intermittent Static Web Assets manifest collisions and browser module MIME errors.
- Established a deterministic build + deploy pipeline for JS/CSS with cache-friendly fingerprints.

What was implemented
- Bundler: `esbuild` (fast, minimal config). Entry sources live under `pto.track/wwwroot/js/` (TypeScript).
- Output: `pto.track/wwwroot/dist/` (JS, hashed filenames and source maps).
- Fingerprinting: post-build Node script generates content-hash filenames and `asset-manifest.json` mapping logical names to hashed files.
  - Script: `pto.track/scripts/generate-manifest.js` — computes SHA256(…) and renames outputs to include an 8-char hash (example: `site.d6374af6.js`).
- Validation: `pto.track/scripts/validate-manifest.js` ensures `asset-manifest.json` exists and that mapped files are present.
  - NPM script: `npm run validate:manifest`.
- Hosting changes: `HostingExtensions.cs` updated to serve `/dist` with proper MIME and cache headers:
  - `asset-manifest.json` served with `Cache-Control: no-cache, no-store`.
  - Hashed assets served with `Cache-Control: public, max-age=31536000, immutable`.
- Layout changes: `Pages/Shared/_Layout.cshtml` loads `asset-manifest.json` at runtime and appends the correct `<script type="module" src=...>` referencing the hashed `site.js` (fallback to `/dist/site.js` if manifest missing).
- MSBuild integration: `pto.track.csproj` target `NpmBuildForPublish` runs during publish (BeforeTargets=PrepareForPublish):
  - Installs deps (skips `npm ci` when `node_modules` present), conditionally runs `npm run build:js` (stamp-based gating), and runs manifest validation.
  - Exposes an MSBuild message with the manifest absolute path for CI visibility.

How to build locally
1. Install dev deps (if not present):
   ```powershell
   Set-Location 'c:\code\dotnet\pto\pto.track'
   npm ci
   ```
2. Build frontend (bundles + fingerprinting + manifest):
   ```powershell
   npm run build:js
   ```
3. Validate manifest:
   ```powershell
   npm run validate:manifest
   ```
4. Build and run app:
   ```powershell
   dotnet build .\pto.track\pto.track.csproj
   Start-Process -FilePath dotnet -ArgumentList 'run','--project','c:\code\dotnet\pto\pto.track\pto.track.csproj' -WorkingDirectory 'c:\code\dotnet\pto\pto.track' -NoNewWindow
   ```

CI / Publish notes
- `NpmBuildForPublish` target in `pto.track.csproj` will run `npm run build:js` (conditioned) and then validate the manifest during `dotnet publish`.
- Ensure CI has Node/npm installed and available on PATH. The target will fail fast if manifest is missing or incorrect.

Rollout considerations
- Deploy `wwwroot/dist` and the published site together; manifest is no-cache so pages will query the latest mapping quickly.
- Because assets are hashed and immutable, you can safely set very long CDN/browser cache lifetimes for assets.

Files changed (key)
- `pto.track/package.json` — `build:js`, `watch:js`, `validate:manifest` scripts
- `pto.track/scripts/generate-manifest.js` — fingerprinting + manifest writer
- `pto.track/scripts/validate-manifest.js` — manifest validator
- `pto.track/Pages/Shared/_Layout.cshtml` — loads manifest at runtime and appends module script
- `pto.track/HostingExtensions.cs` — static file mapping + cache headers for `/dist`
- `pto.track/pto.track.csproj` — `NpmBuildForPublish` target updated to run frontend build + validation and log manifest path

Troubleshooting
- If manifest missing: run `npm run build:js` then `npm run validate:manifest` locally to surface errors.
- If browser loads an older asset: verify `asset-manifest.json` is the latest (no-cache) and that your page loaded the new manifest mapping.

Next steps (optional)
- Add SRI checks for immutable assets.
- Upload source maps to your error tracker (Sentry/Datadog) during CI publish.

---
Document created from implemented changes; edit as needed.
