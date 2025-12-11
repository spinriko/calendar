
# CI Runbook — Azure Pipelines

This document describes a recommended Azure Pipelines layout (YAML) for this repository. The goals:

- Keep analyzers separate so they do not intermittently block or hang functional test jobs.
- Produce test artifacts (TRX), code coverage reports, analyzer logs, and frontend bundle artifacts for Azure DevOps Server display and deployment.
- Publish build outputs (including wwwroot/dist) for deployment/inspection.

## Frontend Bundling and Artifacts

- Frontend JS/TS assets are bundled using esbuild and output to `pto.track/wwwroot/dist/` with cache-busting hashes and an `asset-manifest.json`.
- The pipeline should run the frontend build (esbuild) before tests and publish the entire `wwwroot/dist` directory as a build artifact for deployment.
- Example build step (if using npm scripts):
  ```yaml
  - script: npm install --prefix pto.track && npm run build --prefix pto.track
    displayName: 'Build Frontend Bundles'
  ```

## High-level pipeline stages

High-level pipeline stages

1) restore/build — full rebuild of the solution

2) test — run unit/integration tests, JS tests, and publish test results + coverage
3) analyzers — run Roslyn analyzers in a separate job and publish analyzer logs (SARIF optional)
4) publish — `dotnet publish`, upload publish artifacts, and publish frontend bundles (wwwroot/dist)


Notes
- Keep the `analyzers` job independent and non-blocking for `test` so transient analyzer issues don't block PR validation.
- Use `PublishTestResults@2` and `PublishCodeCoverageResults@1` to show test/coverage in Azure DevOps. Upload analyzer logs as regular build artifacts so they can be downloaded and inspected.
- Run the frontend build (esbuild) before tests to ensure up-to-date bundles. Publish `wwwroot/dist` as a build artifact for deployment.


Example pipeline (azure-pipelines.yml)

```yaml
trigger:
  - main

stages:
  - stage: Build
    displayName: Restore + Build
    jobs:
      - job: Build
        pool: { vmImage: 'windows-latest' }
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '7.x' # update as appropriate

          - script: dotnet restore pto.track.sln
            displayName: Restore

          - script: dotnet build pto.track.sln -c Release --no-restore
            displayName: Build


  - stage: Test
    displayName: Run Tests
    dependsOn: Build
    jobs:
      - job: Test
        pool: { vmImage: 'windows-latest' }
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '9.x'

          - script: npm install --prefix pto.track && npm run build --prefix pto.track
            displayName: Build Frontend Bundles

          - script: dotnet test pto.track.services.tests/pto.track.services.tests.csproj --logger "trx;LogFileName=services_tests.trx" /p:CollectCoverage=true --results-directory $(Build.SourcesDirectory)/TestResults/services
            displayName: Test Services

          - script: dotnet test pto.track.tests/pto.track.tests.csproj --logger "trx;LogFileName=app_tests.trx" /p:CollectCoverage=true --results-directory $(Build.SourcesDirectory)/TestResults/app
            displayName: Test App

          - script: dotnet test pto.track.data.tests/pto.track.data.tests.csproj --logger "trx;LogFileName=data_tests.trx" --results-directory $(Build.SourcesDirectory)/TestResults/data
            displayName: Test Data

          - task: NodeTool@0
            inputs:
              versionSpec: '20.x'
            displayName: Use Node.js 20.x

          - script: pwsh ./pto.track.tests.js/run-headless.ps1
            displayName: Run JavaScript Tests (QUnit/Puppeteer)

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/TestResults/**/*.trx'
              mergeTestResults: true
              testRunTitle: 'Dotnet Tests'

          - task: PublishCodeCoverageResults@1
            inputs:
              codeCoverageTool: 'Cobertura'
              summaryFileLocation: '$(Build.SourcesDirectory)/**/coverage.cobertura.xml'
              reportDirectory: '$(Build.SourcesDirectory)/coverage-report'

          - task: PublishBuildArtifacts@1
            inputs:
              pathToPublish: '$(Build.SourcesDirectory)/pto.track/wwwroot/dist'
              artifactName: 'frontend-dist'
              publishLocation: 'Container'

  - stage: Analyzers
    displayName: Run Analyzers (separate job)
    dependsOn: Build
    condition: succeededOrFailed() # run even if tests fail so we can collect analyzer results
    jobs:
      - job: Analyzers
        pool: { vmImage: 'windows-latest' }
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '7.x'

          - powershell: pwsh ./scripts/run-analyzers.ps1 -Execute
            displayName: Run analyzers script

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: 'artifacts/analyzers'
              ArtifactName: 'analyzers'
              publishLocation: 'Container'

          # Optional: run the lightweight console metrics runner
          # The metrics runner is a small .NET console tool located at `tools/metrics-runner`.
          # Running it in CI produces `artifacts/metrics/metrics.json` which you can publish
          # as a build artifact and consume in downstream gates or dashboards.

          - powershell: |
              pwsh -NoProfile -Command {
                dotnet build tools/metrics-runner/metrics-runner.csproj -c Release
                dotnet run --project tools/metrics-runner -- "$(Build.SourcesDirectory)"
              }
            displayName: Run metrics-runner (console)

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: 'artifacts/metrics'
              ArtifactName: 'metrics'
              publishLocation: 'Container'

          # Optional: if analyzers produce SARIF, publish it using an extension that reads SARIF
          # - task: PublishCodeAnalysisResults@1
          #   inputs:
          #     sarifFile: 'artifacts/analyzers/**/*.sarif'

  - stage: Publish
    displayName: Publish Artifacts
    dependsOn: [Build, Test, Analyzers]
    jobs:
      - job: Publish
        pool: { vmImage: 'windows-latest' }
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '7.x'

          - script: |
              dotnet publish pto.track/pto.track.csproj -c Release -o $(Build.ArtifactStagingDirectory)/publish --no-restore
            displayName: dotnet publish

          - task: PublishBuildArtifacts@1
            inputs:
              PathtoPublish: '$(Build.ArtifactStagingDirectory)/publish'
              ArtifactName: 'app-publish'
              publishLocation: 'Container'

```


Recommended artifact handling

- Test results: publish TRX files via `PublishTestResults@2` so Azure DevOps shows test results per test run.
- Coverage: generate Cobertura (or other supported format) during `dotnet test` (use Coverlet / XPlat Code Coverage) and publish via `PublishCodeCoverageResults@1`.
- Analyzer logs: write analyzer output to `artifacts/analyzers/` (our `scripts/run-analyzers.ps1` does this) and publish that directory with `PublishBuildArtifacts@1` so reviewers can download logs. Optionally produce SARIF from analyzers and publish with a SARIF-aware extension.
- Frontend bundles: publish `pto.track/wwwroot/dist` as a build artifact for deployment and inspection.

Run tests per project (recommended)

It's best to run each test project separately in CI so you can capture per-project test results, timeouts, and logs. This avoids a single monolithic test run that can obscure which project hung or failed and makes it easy to parallelize later.

Example (run each project separately and publish TRX results):

```yaml
# run services/unit tests
script: |
  dotnet test pto.track.services.tests/pto.track.services.tests.csproj \
    --logger "trx;LogFileName=services_tests.trx" \
    --results-directory $(Build.SourcesDirectory)/TestResults/services

# run integration/app tests
script: |
  dotnet test pto.track.tests/pto.track.tests.csproj \
    --logger "trx;LogFileName=app_tests.trx" \
    --results-directory $(Build.SourcesDirectory)/TestResults/app

# run data tests
script: |
  dotnet test pto.track.data.tests/pto.track.data.tests.csproj \
    --logger "trx;LogFileName=data_tests.trx" \
    --results-directory $(Build.SourcesDirectory)/TestResults/data

# publish all TRX files together
- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/TestResults/**/*.trx'
    mergeTestResults: true
    testRunTitle: 'Dotnet Tests'
```

Notes:
- Use `--results-directory` (or `--logger`) to control where TRX files are written so the `PublishTestResults` task can find them reliably.
- Add job-level or script-level timeouts so a single hanging test cannot stall the whole pipeline. When a project hangs, you'll get a failed job quickly instead of waiting for a single aggregated test run to timeout.
- Keep analyzers in a separate stage so their behavior doesn't affect the test job.
- If your build agent runs Node/npm tests (frontend), run them in their own job and publish the HTML/console output as a build artifact (see `save-results.html` in the pipeline). Frontend failures are reported independently.
  - Node version: frontend/test jobs require Node.js 20 or greater. Use the `NodeTool@0` task to ensure a consistent Node runtime in CI agents.

Tips and caveats

- If analyzers are noisy and cause flakes, keep them in the separate `Analyzers` stage and run them on PR merge or nightly schedules instead of every PR.
- Keep `RunAnalyzersDuringBuild=false` during the `Test` stage. Run analyzers explicitly in the `Analyzers` stage with the helper script (`scripts/run-analyzers.ps1`) that captures logs.
- Be explicit about .NET SDK versions using `UseDotNet@2` so agents are consistent.
- Consider splitting long test suites into parallel jobs to speed up CI; publish all test artifacts into a shared artifact container.

Known Azure DevOps Server details

- Azure DevOps Server (on-prem) may require installing extensions to display SARIF results; if you want SARIF integrated into the UI, install a SARIF publisher extension or use the `PublishCodeAnalysisResults@1` task if available in your instance.
- Ensure the build agent has Node.js available if your frontend build step runs `npm` on the hosted agent.
- Ensure the build agent runs the frontend build and publishes `wwwroot/dist` for deployment.

