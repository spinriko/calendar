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
- **Deploy stages**: `deployment` jobs, each targeting an ADO environment (`Corp-Dev`, `Corp-Test`, `Corp-Prod`).
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

- stage: Deploy_Dev
  displayName: Deploy Dev
  dependsOn: Build
  jobs:
  - deployment: DeployDev
    displayName: Deploy to Corp-Dev
    environment: 'Corp-Dev'  # targets registered corp servers
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

- stage: Deploy_Prod
  displayName: Deploy Prod
  dependsOn: Deploy_Dev
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
