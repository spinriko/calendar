DayPilot (vendor file bundled into app)

- Source file: `pto.track/wwwroot/lib/daypilot/daypilot-all.min.js`
- License: DayPilot Lite — Apache License 2.0
- Version: recorded from distributed file header; update here when you replace the vendor file.

Why we bundle
- We import this vendor file into the app entry (`wwwroot/js/absences-scheduler.ts`) so esbuild will include DayPilot in the generated bundle.
- Bundling ensures deterministic load order and avoids runtime race conditions between vendor script and ESM entry imports.

How to update DayPilot in the repo
1. Download the new `daypilot-all.min.js` from the DayPilot distribution you trust (official site or internal mirror).
2. Replace `pto.track/wwwroot/lib/daypilot/daypilot-all.min.js` in the repo and commit the change.
3. Update the `Version:` line above to record the new version and the date.
4. Rebuild frontend artifacts:

```pwsh
cd pto.track
npm run build:js
```

5. Verify `wwwroot/dist/asset-manifest.json` was regenerated and that hashed assets are present.

Notes & best practices
- Keep the vendor file checked into source control so builds are reproducible.
- Respect the DayPilot license (Apache 2.0). Keep the copyright/license header present in the vendor file.
- If you prefer to avoid bundling in future, switch to loading the vendor as a separate script tag and ensure it is served with a short cache TTL or fingerprinted filename.
