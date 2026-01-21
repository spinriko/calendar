# BDD Adoption Strategy (SpecFlow-first)

## Why BDD here
- Keep executable specifications close to code, reusing our .NET 8 test infrastructure (xUnit, WebApplicationFactory, in-memory DB).
- Express business flows (PTO requests, approvals, group access) in Gherkin for readability, while running them as automated acceptance tests.
- Complement existing unit/integration tests; not a replacement. Use BDD for high-value end-to-end behaviors.

## Tool choice
- **SpecFlow (.NET 8, xUnit runner)**: best fit for our C# stack and existing integration harness. Mature, good VS/ADO integration, supports parallel runs, living doc plugins if needed.
- **Alternatives**: 
  - Playwright/Cypress (JS) for browser E2E; useful later for UI journeys, but heavier to maintain.
  - LightBDD (C#) if we want code-first BDD without Gherkin; less business-friendly.
- Recommendation: start with **SpecFlow + xUnit**; optionally add Playwright later for UI smoke flows.

## Scope to target first
- Happy-path PTO request/approve flow (mock auth and with Windows auth).
- Resource visibility by group (authorization rules).
- Absence overlaps/validation rules (business logic acceptance).
- Migration guardrails (ensure seeded defaults/Groups/Resources are present after migration).

## Project layout
- Add a new test project: `pto.track.bdd.tests` (net8.0, xUnit).
- References: `pto.track`, `pto.track.services`, `pto.track.data` as needed.
- NuGet: `SpecFlow.xUnit`, `SpecFlow.Tools.MsBuild.Generation`, `SpecFlow.Assist.Dynamic` (optional), `FluentAssertions` (optional).
- Reuse test server: `WebApplicationFactory<Program>` with in-memory DB and mock auth (similar to `pto.track.tests`).

## Implementation steps
1) **Create project** `pto.track.bdd.tests` with xUnit as test runner.
2) **Install SpecFlow packages** and enable code-behind generation (MSBuild integration).
3) **Bootstrap host**: reuse `CustomWebApplicationFactory` pattern to host the API with in-memory DB and mock auth; share fixtures for performance.
4) **Author features** (Gherkin `.feature` files) for the prioritized flows. Keep Given/When/Then lean; push logic into step definitions/services.
5) **Step definitions**: use HttpClient from the factory for API calls; or call services directly for faster acceptance tests when UI is not required.
6) **Data setup**: create small builders/fixtures to seed in-memory DB per scenario; avoid global shared state to keep scenarios independent.
7) **CI integration**: add a `dotnet test` step for `pto.track.bdd.tests` gated behind a variable (run in main and release branches; optional on PRs to save time).
8) **Living docs (optional)**: enable SpecFlow+ LivingDoc report publication as build artifact if stakeholders want readable outputs.

## Non-goals (for now)
- Full browser automation; add Playwright later if UI journeys need coverage.
- Replacing unit/integration suites; BDD sits above them for acceptance coverage.

## Guardrails
- Keep scenarios high-level; avoid duplication of logic in step definitions (push down into helpers/services).
- Limit scenario runtime; fail fast on setup errors. Target < 30-60s for the entire BDD suite initially.
- Use tags to control execution sets (e.g., @happy-path, @auth, @slow) and map them to pipeline filters.

## Pipeline sketch (later)
- Add a `DotNetCoreCLI@2 test` step for `pto.track.bdd.tests/pto.track.bdd.tests.csproj`, conditioned on branch or variable (e.g., `runBdd=true`).
- Publish LivingDoc (optional) as an artifact for main/release runs.

## Next steps
- Approve tool choice (SpecFlow + xUnit).
- Scaffold `pto.track.bdd.tests` project with one feature: PTO request/approve (mock auth).
- Wire minimal pipeline step (conditional) and measure runtime.
