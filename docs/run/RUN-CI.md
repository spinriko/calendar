# CI Runbook — Azure Pipelines

This document describes a recommended Azure Pipelines layout (YAML) for this repository. The goals:

- Keep analyzers separate so they do not intermittently block or hang functional test jobs.
- Produce test artifacts (TRX / JUnit), code coverage reports and analyzer logs and upload them as build artifacts so Azure DevOps Server can display them.
- Publish build outputs for deployment/inspection.

High-level pipeline stages

1) restore/build — full rebuild of the solution
2) test — run unit/integration tests and publish test results + coverage
3) analyzers — run Roslyn analyzers in a separate job and publish analyzer logs (SARIF optional)
4) publish — `dotnet publish` and upload publish artifacts

Notes
- Keep the `analyzers` job independent and non-blocking for `test` so transient analyzer issues don't block PR validation.
- Use `PublishTestResults@2` and `PublishCodeCoverageResults@1` to show test/coverage in Azure DevOps. Upload analyzer logs as regular build artifacts so they can be downloaded and inspected.

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
              version: '7.x'

          - script: |
              dotnet test pto.track.services.tests/pto.track.services.tests.csproj --logger "trx;LogFileName=services_tests.trx" /p:CollectCoverage=true --results-directory $(Build.SourcesDirectory)/TestResults/services
            displayName: Test Services

          - script: |
              dotnet test pto.track.tests/pto.track.tests.csproj --logger "trx;LogFileName=app_tests.trx" /p:CollectCoverage=true --results-directory $(Build.SourcesDirectory)/TestResults/app
            displayName: Test App

          - script: |
              dotnet test pto.track.data.tests/pto.track.data.tests.csproj --logger "trx;LogFileName=data_tests.trx" --results-directory $(Build.SourcesDirectory)/TestResults/data
            displayName: Test Data

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

Tips and caveats

- If analyzers are noisy and cause flakes, keep them in the separate `Analyzers` stage and run them on PR merge or nightly schedules instead of every PR.
- Keep `RunAnalyzersDuringBuild=false` during the `Test` stage if previous experience shows analyzers can hang. Run analyzers explicitly in the `Analyzers` stage with the helper script that captures logs.
- Be explicit about .NET SDK versions using `UseDotNet@2` so agents are consistent.
- Consider splitting long test suites into parallel jobs to speed up CI; publish all test artifacts into a shared artifact container.

Known Azure DevOps Server details

- Azure DevOps Server (on-prem) may require installing extensions to display SARIF results; if you want SARIF integrated into the UI, install a SARIF publisher extension or use the `PublishCodeAnalysisResults@1` task if available in your instance.
- Ensure the build agent has Node.js available if your frontend build step runs `npm` on the hosted agent.

