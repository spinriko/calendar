# Azure Pipelines Summary for PTO Track

This document outlines the CI/CD pipeline steps for building, testing, publishing, and deploying the PTO Track application using Azure Pipelines. The pipeline is designed to ensure code quality, automate deployment, and update IIS rewrite rules for proper routing.

## Pipeline Steps

1. **Build Stage**
   - Restore NuGet packages and build the solution using `dotnet build`.
   - If the build fails, the pipeline stops and no further steps are executed.

2. **Test Stage**
   - Run all C# unit and integration tests using `dotnet test` with code coverage enabled.
   - Execute JavaScript Jest tests (using Node.js runner).
   - Collect and publish HTML test and coverage reports as pipeline artifacts for review.

3. **Publish Stage**
   - If all tests pass, publish the application as a standalone app using `dotnet publish --self-contained`.
   - Output is placed in the specified deploy folder (e.g., `c:\inet\<AppName>`).

4. **Deploy Stage**
   - Copy published files to the target server and deploy folder.
   - Update IIS rewrite rules to forward HTTP requests to `http://localhost:5139` and HTTPS requests to `https://localhost:7241`.
   - Use PowerShell or IIS Web App Deployment tasks for automation.

5. **Variables**
   - `serverName`: The name or address of the target deployment server.
   - `deployFolder`: The folder path on the server where the app will be deployed (e.g., `c:\inet\pto.track`).
   - `forwardedHttpUrl`: The local HTTP endpoint (e.g., `http://localhost:5139`).
   - `forwardedHttpsUrl`: The local HTTPS endpoint (e.g., `https://localhost:7241`).
   - `rewritePath`: The path to the IIS rewrite rules file or configuration section to update.

## Artifacts & Reports
- HTML test reports and code coverage reports are linked in the pipeline summary for easy access.
- All build/test/publish logs are retained for troubleshooting.

## Notes
- The pipeline assumes the target server and IIS are pre-configured for deployment.
- IIS rewrite rules are updated automatically as part of the deployment step.
- Sensitive variables (e.g., server credentials) should be stored securely in Azure Pipeline variable groups or secrets.

---

For implementation, create an `azure-pipelines.yml` file with the above steps, using appropriate Azure DevOps tasks for .NET, Jest, artifact publishing, and IIS deployment.
