# Azure DevOps Environments: Adoption Guide for Corp Servers

This document outlines how to use Azure DevOps (ADO) Environments to harden and streamline deployments to corp servers, replacing ad‑hoc remote PowerShell tasks with environment‑bound deployment jobs.

## Why Use Environments
- **Approvals & Checks:** Built-in manual approvals, business hours, required reviewers, and policy checks per environment.
- **Resource Binding:** Register corp servers as Virtual Machine resources. Deployment jobs run on those machines (agent-based), no embedded creds in YAML.
- **Secrets & Variables:** Store environment-scoped variables and secrets; reference them consistently from pipeline steps.
- **Observability:** View deployment history, approvals, and health per stage/environment.

## Prerequisites
- Azure DevOps project access (Project Administrator recommended for setup).
- Corp servers reachable from ADO and able to run an ADO agent (Windows service).
- Service account permissions on servers for IIS/service operations and file system.

## Set Up Environments in ADO
1. **Create Environments:**
   - In ADO → Pipelines → Environments → New environment.
   - Create `Corp-Dev`, `Corp-Test`, and `Corp-Prod` (or similar).
2. **Add VM Resources:**
   - Open an environment → Add resource → Virtual machines → Follow wizard.
   - Install the ADO agent on each corp server (Windows service). Tag resources (e.g., `web`, `iis`).
   - Verify resource shows online/healthy.
3. **Configure Approvals & Checks (Prod/Test):**
   - In each environment → Approvals and checks → Add required reviewers, business hours, etc.
4. **Define Environment Variables/Secrets:**
   - Store `sqlConnectionString` (or use Variable Groups / Key Vault). Scope to the environment.

## Pipeline Structure (Multi-Stage)
- **Build stage**: compile and publish artifact.
- **AnalyzeBuild stage**: run unit/integration tests, coverage, lint/static analysis, and dependency/security checks.
- **Deploy stages**: `deployment` jobs, each targeting an ADO environment (`Corp-Dev`, `Corp-Test`, `Corp-Prod`).
- **EnvTests/EnvAnalytics stages**: post-deploy environment-level smoke/functional tests and analytics checks (Dev/Test) gating promotion.
- **Approvals**: configured in environment UI; pipeline pauses until approved.

### Sample YAML (illustrative)
```yaml
stages:
- stage: Build
  displayName: Build
  jobs:
  - job: Build
    pool: { vmImage: 'windows-latest' }
    steps:
    - task: DotNetCoreCLI@2
      displayName: Restore
      inputs:
        command: 'restore'
        projects: 'pto.track/pto.track.csproj'
    - task: DotNetCoreCLI@2
      displayName: Publish
      inputs:
        command: 'publish'
        projects: 'pto.track/pto.track.csproj'
        arguments: '-c Release -o $(Build.ArtifactStagingDirectory)'
    - task: PublishBuildArtifacts@1
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'drop'

- stage: AnalyzeBuild
  displayName: Analyze Build (Tests, Coverage, Lint)
  dependsOn: Build
  jobs:
  - job: Analyze
    pool: { vmImage: 'windows-latest' }
    steps:
    # Run tests with coverage for each test project in the stack
    - task: DotNetCoreCLI@2
      displayName: Run .NET Tests (web) with Coverage
      inputs:
        command: 'test'
        projects: 'pto.track.tests/pto.track.tests.csproj'
        arguments: '--collect:"XPlat Code Coverage"'
    - task: DotNetCoreCLI@2
      displayName: Run .NET Tests (services) with Coverage
      inputs:
        command: 'test'
        projects: 'pto.track.services.tests/pto.track.services.tests.csproj'
        arguments: '--collect:"XPlat Code Coverage"'
    - task: DotNetCoreCLI@2
      displayName: Run .NET Tests (data) with Coverage
      inputs:
        command: 'test'
        projects: 'pto.track.data.tests/pto.track.data.tests.csproj'
        arguments: '--collect:"XPlat Code Coverage"'
    - task: PublishTestResults@2
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/TestResults/*.trx'
        failTaskOnFailedTests: true
    - script: npm test --prefix pto.track.tests.js
      displayName: Run JS tests (npm)
    - task: PublishTestResults@2
      inputs:
        testResultsFormat: 'JUnit'
        testResultsFiles: 'pto.track.tests.js/results/*.xml'
        failTaskOnFailedTests: true
    - task: PowerShell@2
      displayName: Dependency Vulnerability Scan (.NET)
      inputs:
        targetType: 'inline'
        script: |
          dotnet list pto.track/pto.track.csproj package --vulnerable || exit 0

- stage: Deploy_Dev
  displayName: Deploy Dev
  dependsOn: AnalyzeBuild
  jobs:
  - deployment: DeployDev
    displayName: Deploy to Corp-Dev
    environment: 'Corp-Dev'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: drop
          - task: PowerShell@2
            displayName: Extract Artifact
            inputs:
              targetType: 'inline'
              script: |
                $src = "$(Pipeline.Workspace)\drop"
                $dst = "C:\deploy\pto-track\dev\temp"
                if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
                New-Item -ItemType Directory -Force -Path $dst | Out-Null
                Copy-Item -Path $src\* -Destination $dst -Recurse
          - task: PowerShell@2
            displayName: Set Environment Variables (Machine)
            inputs:
              targetType: 'inline'
              script: |
                [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT','Development','Machine')
                $conn = '$(sqlConnectionString)'
                [Environment]::SetEnvironmentVariable('ConnectionStrings__PtoTrackDbContext',$conn,'Machine')
          - task: PowerShell@2
            displayName: Swap Folders & Restart IIS
            inputs:
              targetType: 'inline'
              script: |
                $deploy = "C:\inetpub\wwwroot\pto-track"
                $backup = "C:\inetpub\wwwroot\pto-track_backup"
                if (Test-Path $backup) { Remove-Item $backup -Recurse -Force }
                if (Test-Path $deploy) { Move-Item $deploy $backup }
                Move-Item "C:\deploy\pto-track\dev\temp" $deploy
                iisreset

- stage: EnvTests_Dev
  displayName: Environment Tests (Dev)
  dependsOn: Deploy_Dev
  jobs:
  - job: EnvTestsDev
    pool: { vmImage: 'windows-latest' }
    steps:
    - task: PowerShell@2
      displayName: Health + Smoke Tests
      inputs:
        targetType: 'inline'
        script: |
          $BaseUrl = 'https://corp.example.com/pto-track'
          $ErrorActionPreference = 'Stop'
          function Test-Url($u){ try { (Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 15).StatusCode -in 200..299 } catch { $false } }
          $ok = (Test-Url "$BaseUrl/health/ready") -and (Test-Url "$BaseUrl/health/live") -and (Test-Url "$BaseUrl/dist/asset-manifest.json") -and (Test-Url "$BaseUrl/dist/site.js")
          if (-not $ok) { throw "Post-deploy checks failed" }
    - script: npm test --prefix pto.track.tests.js
      displayName: Run UI/API Tests (Dev) via npm

- stage: Deploy_Test
  displayName: Deploy Test
  dependsOn: EnvTests_Dev
  jobs:
  - deployment: DeployTest
    displayName: Deploy to Corp-Test
    environment: 'Corp-Test'
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: drop
          # repeat extract/env/restart steps with test-specific paths

- stage: EnvTests_Test
  displayName: Environment Tests (Test)
  dependsOn: Deploy_Test
  jobs:
  - job: EnvTestsTest
    pool: { vmImage: 'windows-latest' }
    steps:
    - task: PowerShell@2
      displayName: Health + Smoke Tests
      inputs:
        targetType: 'inline'
        script: |
          $BaseUrl = 'https://corp.example.com/test/pto-track'
          $ErrorActionPreference = 'Stop'
          function Test-Url($u){ try { (Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 15).StatusCode -in 200..299 } catch { $false } }
          $ok = (Test-Url "$BaseUrl/health/ready") -and (Test-Url "$BaseUrl/health/live") -and (Test-Url "$BaseUrl/dist/asset-manifest.json") -and (Test-Url "$BaseUrl/dist/site.js")
          if (-not $ok) { throw "Post-deploy checks failed" }
    - script: npm test --prefix pto.track.tests.js
      displayName: Run UI/API Tests (Test) via npm

- stage: Deploy_Prod
  displayName: Deploy Prod
  dependsOn: EnvTests_Test
  jobs:
  - deployment: DeployProd
    displayName: Deploy to Corp-Prod
    environment: 'Corp-Prod'  # approvals/checks configured in environment UI
    strategy:
      runOnce:
        deploy:
          steps:
          - download: current
            artifact: drop
          # (repeat steps for prod with environment-specific paths/variables)
```

## Migration from PowerShellOnTargetMachines
- **Before:** `PowerShellOnTargetMachines@3` used credentials to run remote scripts.
- **After:** `deployment` jobs target Environments; scripts run on the registered agent on each corp server.
- **Replace:**
  - File copy/extract → `DownloadPipelineArtifact@2` + local `PowerShell@2` on the environment resource.
  - Env vars → `PowerShell@2` setting Machine-level vars or use `env:` in steps.
  - Service/IIS ops → `PowerShell@2` or IIS tasks running locally on the server.

## Variables & Secrets
- Store `sqlConnectionString` in an environment or variable group.
- Reference via `$(sqlConnectionString)` in YAML; avoid inline creds.
- Consider Key Vault integration for prod secrets (ADO service connection required).

## Do Environments Require VMs?
- **Short answer:** No. Environments are logical stages with approvals, checks, and deployment history.
- **When VMs are needed:** If you want deployment steps to execute on the corp servers via the environment, register those servers as **Virtual machine** resources and install an ADO agent. `deployment` jobs then run directly on those machines.
- **Alternative without VMs:** Use Environments for approvals/traceability while keeping remote tasks (e.g., `WindowsMachineFileCopy@2`, `PowerShellOnTargetMachines@3`) so steps run on the pipeline agent but target servers remotely.
- **Azure resources:** For Azure Web Apps/Kubernetes, bind the environment to those resources and deploy without Windows VM registration.

## CI vs CD Agents: Build Server vs Environment VMs
- **Build Server Agent (CI):** Runs on your build server (or hosted pool). Executes:
  - Build stage: compile, restore packages, publish artifact.
  - AnalyzeBuild stage: unit/integration tests (all csproj), coverage, lint, dependency scans.
  - EnvTests_Dev/EnvTests_Test stages: smoke tests hitting deployed endpoints from outside (no direct server access needed).
  - Uses: pool (custom or `windows-latest`).

- **Environment VM Agents (CD):** Installed on target corp servers (`server1-dev`, `server1-test`, etc.). Execute:
  - Deploy_Dev/Deploy_Test/Deploy_Prod jobs: extract artifact, set Machine-level env vars, swap IIS folders, restart service.
  - Uses: `environment: Corp-Dev` (or Test/Prod) binding; runs directly on the registered machine.

- **Workflow:**
  - Build server agent runs CI pipeline (build, test, analyze).
  - Build server agent publishes artifact to pipeline storage.
  - Environment VM agents on corp servers run CD pipeline (deployment ops).
  - Build server agent (or hosted pool) runs post-deploy smoke tests hitting the deployed endpoint.

- **Key Point:** The build server agent doesn't "go away." It's still essential for CI. Environment VM agents are *added* to handle deployment on target machines. Both work together in an end-to-end CI/CD flow.

## Approvals & Governance
- Configure environment approvals/checks (required reviewers, business hours, external validations).
- Keep audit trail in ADO Environments for each deploy.

## Operational Tips
- Restart app pool/service after setting Machine-level vars to ensure the app picks them up.
- Add logs around startup (log `UsePathBase`, static file mapping, map endpoints).
- Include rollback step: keep previous deploy and auto-swap back on health/smoke test failure.

## Next Steps
- Create `Corp-Dev`, `Corp-Test`, `Corp-Prod` environments and register corp servers.
- Convert remote PowerShell tasks to environment-bound `deployment` jobs.
- Parameterize environment-specific paths and variables; add smoke tests and rollback.
