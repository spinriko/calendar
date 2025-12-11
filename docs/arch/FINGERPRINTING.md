# Fingerprinting & Asset Manifest — PTO Track

Purpose
- Describe the fingerprinting strategy used by PTO Track to produce cache-safe, content-addressed frontend assets and to wire those assets into the ASP.NET Core app at runtime and during publish.

Summary
- After bundling with `esbuild`, a post-build script computes a content hash for each output asset and renames the files to include an 8-character hash (e.g. `site.d6374af6.js`).
- A JSON manifest `wwwroot/dist/asset-manifest.json` maps logical names to the hashed paths (leading slash):

  ```json
  {
    "site.js": "/dist/site.d6374af6.js",
    "absences-scheduler.js": "/dist/absences-scheduler.15bcd525.js"
  }
  ```

Implementation details
- Hashing: SHA-256 of the file contents; manifest uses the first 8 hex characters of the hex digest.
- Renaming: script renames both the asset file and its source map, and updates the `sourceMappingURL` comment inside JS files to reference the new source map filename.
- Idempotency: the generator is careful to not re-hash already-hashed filenames (detects existing `name.[0-9a-f]{8}.ext`).

Key scripts
- `pto.track/scripts/generate-manifest.js`
  - Input: `wwwroot/dist/*.js`, `*.css`, `*.map` created by `esbuild`.
  - Output: renamed files and `wwwroot/dist/asset-manifest.json`.
- `pto.track/scripts/validate-manifest.js`
  - Ensures `asset-manifest.json` exists and that each mapped file exists on disk under `wwwroot/dist`.

Runtime integration
- `_Layout.cshtml` performs a runtime `fetch('/dist/asset-manifest.json')` and then appends a `<script type="module" src="...">` tag using the `site.js` mapping (or falls back to `/dist/site.js` when the manifest is missing).
- Because the manifest is fetched by the client and served with `no-cache`, pages will always obtain the latest mapping shortly after deploy.

Publish / CI integration
- `pto.track.csproj` contains a `NpmBuildForPublish` MSBuild target that runs before `PrepareForPublish`.
  - It conditionally runs the frontend build (`npm run build:js`) based on a stamp file, and always runs manifest validation when `wwwroot/dist` exists.
  - The target logs the absolute manifest path using an MSBuild Message with `High` importance to make it visible in CI logs.

Serving & caching
- `HostingExtensions.cs` configures static file serving for `wwwroot/dist` and sets the following cache behavior:
  - `asset-manifest.json`: `Cache-Control: no-cache, no-store` (so the browser always re-checks the mapping).
  - Hashed assets: `Cache-Control: public, max-age=31536000, immutable` (safe to cache indefinitely since filename changes when content changes).

Troubleshooting
- Missing manifest or files: run `npm run build:js` then `npm run validate:manifest` locally. Validate will exit non-zero and print missing paths on failure.
- Manifest points to missing files after deploy: ensure deploy is atomic (manifest + hashed assets uploaded together), or use an activation step that swaps the manifest when files are ready.
- Browser 415/empty responses for ESM modules: verify server serves the hashed `.js` file and that MIME type is text/javascript. Hosting changes ensure proper mapping.

Examples
- Build & validate locally:
  - `npm ci`
  - `npm run build:js`
  - `npm run validate:manifest`

Notes
- Consider integrating source-map upload (Sentry/Datadog) in CI after build to assist debugging of production errors.
- Consider Subresource Integrity (SRI) for third-party CDN-hosted assets if relevant.
