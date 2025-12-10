# Recovery Plan for `feature/add-local-env`

Date: 2025-12-09
Branch: feature/add-local-env (current)

Goal: Stabilize test strategy so full JS + .NET test suites run reliably.

Summary:
- We've implemented an IDbContextStrategy pattern and centralized seeding (SeedDefaults).
- Integration test seeding now uses a shared `InMemoryDatabaseRoot` for tests.
- A previously failing integration test passes in isolation, but full-suite runs are unstable: testhost lifecycle issues and intermittent attempts to run SQL migrations with invalid connection strings.

Recovery Steps (prioritized):

1. Create backup branch
   - Create a local backup branch to preserve current WIP before any further edits.

2. Reproduce full-suite failure and capture logs
   - Run the full .NET test suite and frontend tests, capture `dotnet test` logs and any `testhost` traces/artifacts.

3. Audit DbContext registrations across projects
   - Verify every project (app and tests) uses `IDbContextStrategy` and that tests consistently use `InMemoryDbContextStrategy` when `ASPNETCORE_ENVIRONMENT=Testing`.

4. Add defensive migration guards for Testing/InMemory
   - Ensure startup code does not call `Database.Migrate()` or attempt SQL migrations when using the InMemory provider or when `ASPNETCORE_ENVIRONMENT == "Testing"`.

5. Ensure tests use shared `InMemoryDatabaseRoot`
   - Confirm `CustomWebApplicationFactory` registers a single `InMemoryDatabaseRoot` instance so test code and test server share the same in-memory database.

6. Add testhost cleanup & disable parallel testhost runs
   - Add logic to gracefully shut down `testhost` and disable parallelization that creates competing testhosts; consider adding timeouts and retries for testhost shutdown.

7. Run targeted integration subsets to isolate failures
   - Execute smaller groups of tests and narrow down any project, test, or pattern that triggers SQL provider attempts.

8. Run full JS + .NET suite and collect artifacts
   - Repeat full suite with logging enabled and collect diagnostics (dotnet, node, jest, testhost process listings). Attach timestamps and machine state info.

9. Bisect or revert to last green commit if instability persists
   - If tests remain unstable, perform a git bisect or revert recent refactors until the suite is reliably green.

Notes:
- I saved this plan and will not push any commits without your explicit instruction.
- Next actionable items I can run now (pick one):
  - Create a local backup branch and commit workspace (I already committed changes locally as you requested).
  - Reproduce the full-suite failure and gather logs.
  - Audit `IDbContextStrategy` registrations across the repo.


