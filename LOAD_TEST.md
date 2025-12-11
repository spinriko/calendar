# Load Testing Strategy for PTO Track

This document outlines potential strategies for implementing load testing in the PTO Track application.

## 1. NBomber (Recommended)
**Type:** Pure .NET / C# Code
**Best for:** Developers who want to keep everything in the .NET ecosystem.

NBomber treats load tests like unit tests. You create a console application, define scenarios in C#, and run them via the CLI.

### Pros
*   **Language:** Written entirely in C#. No need to learn a new syntax.
*   **Integration:** Can share DTOs and logic with your existing solution.
*   **CI/CD:** Runs easily in any pipeline that supports .NET.

### Getting Started
1.  Create a new console project: `dotnet new console -n pto.track.loadtests`
2.  Add the package: `dotnet add package NBomber`
3.  Define a scenario:
    ```csharp
    var scenario = Scenario.Create("fetch_absences", async context =>
    {
        var response = await httpClient.GetAsync("/api/absences");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    })
    .WithoutWarmUp()
    .WithLoadSimulations(
        Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
    );

    NBomberRunner
        .RegisterScenarios(scenario)
        .Run();
    ```

## 2. k6
**Type:** JavaScript / TypeScript CLI Tool
**Best for:** High-performance testing and industry-standard metrics.

k6 is a modern load testing tool that uses JavaScript for scripting but runs on a Go engine for high performance.

### Pros
*   **Performance:** Can generate massive load from a single machine.
*   **Ecosystem:** Huge library of extensions and integrations.
*   **VS Code:** Excellent extension support (`moonolgerd.k6-test-explorer`).

### Getting Started
1.  Install k6 (winget, choco, or download).
2.  Create a script `load-test.js`:
    ```javascript
    import http from 'k6/http';
    import { sleep } from 'k6';

    export default function () {
      http.get('http://localhost:5000/api/absences');
      sleep(1);
    }
    ```
3.  Run: `k6 run load-test.js`

## 3. Azure Load Testing
**Type:** Cloud Service
**Best for:** Testing Azure-deployed resources and simulating global traffic.

If this application is deployed to Azure, this service allows you to generate high-scale load without managing infrastructure.

### Pros
*   **Managed:** No infrastructure to set up.
*   **Integration:** Native integration with Azure Monitor and CI/CD.
*   **Scale:** Easily simulate thousands of concurrent users.

### Getting Started
1.  Install the VS Code extension: `ms-azure-load-testing.microsoft-testing`.
2.  Create a test config in the Azure portal or via the extension.
3.  Point it at your deployed App Service.
