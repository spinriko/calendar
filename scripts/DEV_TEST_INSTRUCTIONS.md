Dev Test Runner
================

This runner provides a safe way to execute the full test surface for local development without touching real databases.

Behavior
- Runs C# unit and integration tests with `ASPNETCORE_ENVIRONMENT=Testing` (in-memory DB path taken by test factories).
- Runs JavaScript tests in `pto.track.tests.js`.
- Default mode is dry-run. Pass `-Execute` to actually run tests.

Usage
```
pwsh ./scripts/dev-test.ps1            # dry-run (prints commands)
pwsh ./scripts/dev-test.ps1 -Execute  # run tests
pwsh ./scripts/dev-test.ps1 -Execute -DisableAnalyzers -FailFast -TimeoutSeconds 300
```

Notes
- This keeps your normal developer environment `local` unchanged; the script injects `ASPNETCORE_ENVIRONMENT=Testing` into the test processes only.
- If you prefer the tests to run with `local`, consider adding a guarded `LOCAL_TEST_MODE` flag in startup so tests can run safely under `local` (requires code changes).
