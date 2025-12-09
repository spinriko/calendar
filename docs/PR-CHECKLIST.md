# PR Checklist

Use this checklist before opening a pull request. Small PRs should still follow these items where applicable.

- [ ] **Build:** Ensure the solution builds locally: `dotnet build pto.track.sln -c Release`.
- [ ] **Frontend Build:** If frontend files changed (TS/JS/CSS/Scss/asset pipeline), run the frontend build and confirm assets are generated into `pto.track/wwwroot/dist/` and `asset-manifest.json` is updated.
  - Command example: `npm install --prefix pto.track && npm run build --prefix pto.track`
- [ ] **Fixtures:** If asset fingerprints changed, run `pwsh ./pto.track/scripts/update-fixtures-from-manifest.js` and verify `responses/` fixtures are updated where needed.
- [ ] **Tests:** Run unit and integration tests locally:
  - .NET: `dotnet test` (tests default to analyzers disabled in local runs; see RUN-LOCAL for details)
  - JS: `npm test --prefix pto.track.tests.js` (if applicable)
- [ ] **Analyzers:** Run analyzers as a separate step (locally or in CI): `pwsh ./scripts/run-analyzers.ps1 -Execute`. Address warnings/errors as appropriate.
- [ ] **Manual Smoke Test:** Start the app (`pwsh ./scripts/dev.ps1 run`) and exercise the key pages affected by the change (Absences scheduler, Groups page, Resources, Events).
- [ ] **DevTools Check:** In browser DevTools confirm:
  - No 404s for `/dist/` assets
  - No `Unexpected token 'export'` or other module loading errors
  - No `DayPilot library not loaded` errors for scheduler pages
- [ ] **Docs / Runbook:** If the change affects developer workflows (build, run, test), update `docs/RUN-LOCAL.md` or `docs/ANALYZERS.md` accordingly.
- [ ] **Package updates:** If you updated npm packages or NuGet packages, include a short rationale and run `pwsh ./scripts/dev.ps1 all` to validate end-to-end.
- [ ] **Commit hygiene:** Keep commits small and focused; squash or rebase before merging if the branch contains fixup commits.
- [ ] **Reviewer notes:** Add a short note in the PR description listing anything reviewers should verify (e.g., asset fingerprints changed, vendor library updates, DB migration steps required in staging/prod).

If you follow this checklist most common causes of CI/test failures and local runtime breakages are prevented. If you're unsure about steps for a specific change, ask in the PR or ping a reviewer for a quick pairing session.
