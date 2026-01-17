# Azure DevOps Server Express: Local E2E Pipeline Dry Run

Use this checklist to spin up Azure DevOps Server Express locally and run your full pipeline before pushing to corp.

## 1) Install and Configure ADO Server Express
- Run the Azure DevOps Server Express 2022 installer.
- Use the configuration wizard to:
  - Configure with IIS + built-in SQL Express (default is fine for local).
  - Create the default Project Collection.

## 2) Create Project and Import Repo
- Create a new Project (private, Git).
- Either push your local repo to the server remote or use Import repository.
- Verify the repo contains `azure-pipelines.yml` at the root.

## 3) Create Agent Pool and Queue (DVO)
- Organization Settings -> Agent pools -> New pool -> Name: `DVO`.
- Project Settings -> Agent pools -> `DVO` -> New agent queue -> Grant project access.

## 4) Install and Register a Self-hosted Agent
- In pool `DVO`, click New agent -> Windows -> Download.
- Unzip to e.g. `C:\ado-agent\A` and run the config script:
  - Pool: `DVO`
  - Auth: PAT or Windows auth (PAT is easiest locally)
  - Run as a Windows Service
- Start the agent service and verify it shows online in the `DVO` pool.

## 5) Install Required Tooling on the Agent
- .NET SDK 8.0.100
- Node.js 20.x
- Git
- Optional: 7-Zip, ReportGenerator (the pipeline scripts can fetch if needed)
- Note: `UseDotNet@2` is disabled in YAML, so the agent must have these pre-installed.

## 6) Recreate Variable Groups
- Project -> Pipelines -> Library:
  - Group: `DevOps Automation Accounts`
    - `automationAcctName` (plain)
    - `automationAcctPass` (secret)
  - Group: `ASBDotNetWebApps-DEV`
    - `webServerDev` (machine name)
    - `pto-track-env` (e.g., `Development`)
    - `sqlConnectionString` (secret)
    - `serviceAccountName` (service Log On user)
    - `serviceAccountPassword` (secret)

## 7) Prepare the Target Web Server (for DeployDev)
- SMB access (WindowsMachineFileCopy@2):
  - Ensure admin shares are enabled and firewall allows File and Printer Sharing.
  - The account in `automationAcctName` must be a local admin on the target.
- WinRM access (PowerShellOnTargetMachines@3): on both agent and target server
```powershell
Enable-PSRemoting -Force
winrm quickconfig -q
# On agent machine, trust the target; repeat for additional hosts
Set-Item WSMan:localhost\Client\TrustedHosts <webServerDev> -Force
# Verify connectivity
Test-WSMan <webServerDev>
```

## 8) Create the Pipeline and Run
- Pipelines -> New pipeline -> Azure Repos Git -> Existing `azure-pipelines.yml`.
- Keep `pool: name: DVO` (since you created the `DVO` pool).
- First run: Build and Test stages should pass.
- Full run: Execute the entire pipeline to validate Analyze, Publish, and DeployDev.

## 9) What to Validate
- Analyze stage publishes artifacts:
  - `analyzers`, `metrics`, and `metrics-report` (HTML)
- DeployDev stage:
  - `Copy Build Artifact to Web Server` writes to `\\<webServerDev>\c$\self-contained_apps\pto.track_temp\`
  - `Extract Artifact` expands `pto.track.zip` into the temp folder.
  - `Set ASPNETCORE_ENVIRONMENT on the IIS app pool` is handled by the deployment script (`finish-deploy-iis.ps1`) which reads `$(pto-track-env)` from pipeline variables and applies it to the app pool.
  - `Set ConnectionStrings__PtoTrackDbContext` writes the machine-scope connection string.
  - `Replace Previous App Files/Folders` swaps backup/current/temp.
  - `Create New Windows Service And Start` creates/starts the Windows service.

## 10) Optional: Custom Tab for Metrics Report
- Follow the steps in `docs/run/CUSTOM-TAB.md` to add a custom tab that renders `metrics-report/index.html` directly in ADO Server.

## 11) Troubleshooting Tips
- Agent pool mismatch: If the pipeline can’t find agents, confirm the pool is `DVO` and the agent is online.
- Tooling: Verify `dotnet --info` and `node --version` on the agent.
- WinRM: If `PowerShellOnTargetMachines@3` fails, recheck WinRM config and credentials; try `Test-WSMan`.
- SMB: If file copy fails, test `\\<webServerDev>\c$` in File Explorer under the same credentials.
- Token replacement: The DeployDev logs should show "Replacing environment token in web.config" and "Token replaced".

## 12) Safety Notes
- `web.config` environment variables are process-scoped to the IIS app (in-process hosting).
- `dotnet run` and Windows Services ignore `web.config`; they use process/user/machine environment.
- Secrets should only be stored as Library variable group secrets in ADO; avoid committing them.
