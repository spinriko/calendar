# Testing Setup in VS Code

## Quick Start

1. **Install Extensions** (VS Code will prompt on first open)
   - Accept the recommended extensions popup, or manually install:
     - `ms-dotnettools.csharp` (C# Dev Kit)
     - `formulahendry.dotnet-test-explorer` (.NET Test Explorer)
     - `hbenl.vscode-test-explorer` (Test Explorer UI)

2. **Open Test Explorer**
   - Click the beaker icon 🧪 in the left sidebar (Activity Bar)
   - Or: `Ctrl+Shift+X` → search "Test Explorer" → install

3. **Run Tests**
   - Click the beaker icon to open Test Explorer
   - Tests should auto-discover from `Project.Tests`
   - Click ▶️ next to a test name to run it
   - Click 🐛 to debug a test

## Keyboard Shortcuts

- Run all tests: `Ctrl+Shift+P` → "Run All Tests"
- Run current test: `Ctrl+Shift+P` → "Run Test at Cursor"
- Debug current test: `Ctrl+Shift+P` → "Debug Test at Cursor"

## Command Palette Tasks

`Ctrl+Shift+P` then type:
- "Tasks: Run Task" → `test` (run all tests)
- "Tasks: Run Task" → `build` (build project)
- "Debug: Start Debugging" → `.NET Core Launch (web)` (run app with debugger)

## Terminal Alternative

```bash
cd /home/spinriko/code/dotnet/resource

# Run all tests
dotnet test Project.Tests

# Run specific test
dotnet test Project.Tests --filter "GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents"

# Run with verbose output
dotnet test Project.Tests -v d

# Run with code coverage
dotnet test Project.Tests /p:CollectCoverage=true
```

## Troubleshooting

- **Tests not showing in Test Explorer?**
  - Reload VS Code: `Ctrl+Shift+P` → "Developer: Reload Window"
  - Ensure `.NET Test Explorer` extension is installed
  - Run `dotnet build Project.Tests` in terminal

- **Debugger not stopping at breakpoints?**
  - Make sure you clicked the 🐛 icon (debug) not ▶️ (run)
  - In launch.json, check the `preLaunchTask` completes successfully

- **Tests fail with "Program" not found?**
  - Make sure `Project/Program.cs` has `public partial class Program { }`
  - Run `dotnet build Project/Project.csproj`
