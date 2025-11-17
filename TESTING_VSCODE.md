# Testing in VS Code

Complete guide for running and debugging tests in Visual Studio Code for the PTO Track solution.

## Quick Start

### 1. Install Required Extensions
VS Code will prompt you to install recommended extensions on first open. If not, manually install:
- **C# Dev Kit** (`ms-dotnettools.csharp`) - Official C# support from Microsoft
- **.NET Test Explorer** (`formulahendry.dotnet-test-explorer`) - Test discovery and execution
- **Test Explorer UI** (`hbenl.vscode-test-explorer`) - Unified test UI

### 2. Open Test Explorer
- Click the beaker icon 🧪 in the Activity Bar (left sidebar)
- Tests will auto-discover from all three test projects:
  - `pto.track.tests` (16 integration tests)
  - `pto.track.services.tests` (29 service layer tests)
  - `pto.track.data.tests` (1 placeholder test)

### 3. Run Tests
- **Run All**: Click the ▶️ icon at the top of Test Explorer
- **Run Single Test**: Click ▶️ next to any test name
- **Run Test File**: Click ▶️ next to a test class name
- **Run Test Project**: Click ▶️ next to a project name
- **Debug Test**: Click 🐛 icon next to any test to debug with breakpoints

---

## Test Explorer Organization

Tests are organized hierarchically:

```
🧪 Test Explorer
├── pto.track.tests (16 tests)
│   ├── EventsControllerTests (8 tests)
│   │   ├── GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents
│   │   ├── GetSchedulerEvent_WithValidId_ReturnsEvent
│   │   ├── GetSchedulerEvent_WithInvalidId_ReturnsNotFound
│   │   ├── PostSchedulerEvent_WithValidEvent_ReturnsCreatedAtAction
│   │   ├── PutSchedulerEvent_WithValidIdAndEvent_ReturnsNoContent
│   │   ├── PutSchedulerEvent_WithMismatchedId_ReturnsNotFound
│   │   ├── DeleteSchedulerEvent_WithValidId_ReturnsNoContent
│   │   └── DeleteSchedulerEvent_WithInvalidId_ReturnsNotFound
│   ├── IntegrationTests (5 tests)
│   │   ├── GetResources_ReturnsSeededResources
│   │   ├── GetEvents_ReturnsSeededEventsForRange
│   │   ├── Events_EndToEnd_CRUD_Works
│   │   ├── PostSchedulerEvent_InvalidDates_ReturnsBadRequest
│   │   └── PutSchedulerEvent_InvalidDates_ReturnsBadRequest
│   └── ResourcesControllerTests (3 tests)
│       ├── GetResources_ReturnsAllResources
│       ├── GetResources_WithNoResources_ReturnsEmptyList
│       └── GetResources_ReturnsResourcesWithCorrectData
├── pto.track.services.tests (29 tests)
│   ├── DtoSerializationTests (8 tests)
│   │   ├── EventDto_SerializesToCamelCase
│   │   ├── EventDto_DoesNotSerializeWithPascalCase
│   │   ├── CreateEventDto_DeserializesFromCamelCase
│   │   ├── UpdateEventDto_DeserializesFromCamelCase
│   │   ├── ResourceDto_SerializesToCamelCase
│   │   ├── ResourceDto_DoesNotSerializeWithPascalCase
│   │   ├── EventDto_RoundTripSerialization
│   │   └── EventDto_SerializedJsonMatchesExpectedFormat
│   ├── EventServiceTests (14 tests)
│   │   ├── GetEventsAsync_WithEventsInDateRange_ReturnsMatchingEvents
│   │   ├── GetEventsAsync_WithNoEventsInRange_ReturnsEmpty
│   │   ├── GetEventsAsync_WithOverlappingEvents_ReturnsCorrectly
│   │   ├── GetEventByIdAsync_WithValidId_ReturnsEvent
│   │   ├── GetEventByIdAsync_WithInvalidId_ReturnsNull
│   │   ├── CreateEventAsync_WithValidDto_CreatesAndReturnsEvent
│   │   ├── CreateEventAsync_WithNullableFields_CreatesSuccessfully
│   │   ├── UpdateEventAsync_WithValidIdAndDto_UpdatesEvent
│   │   ├── UpdateEventAsync_WithInvalidId_ReturnsFalse
│   │   ├── DeleteEventAsync_WithValidId_DeletesEvent
│   │   ├── DeleteEventAsync_WithInvalidId_ReturnsFalse
│   │   ├── CreateEventAsync_PreservesAllFields
│   │   └── GetEventsAsync_WithMultipleResources_ReturnsAllEvents
│   └── ResourceServiceTests (7 tests)
│       ├── GetResourcesAsync_WithMultipleResources_ReturnsAll
│       ├── GetResourcesAsync_WithNoResources_ReturnsEmpty
│       ├── GetResourcesAsync_ReturnsCorrectData
│       ├── GetResourcesAsync_ReturnsDtosNotEntities
│       ├── GetResourcesAsync_ReturnsInDatabaseOrder
│       ├── GetResourcesAsync_WithSingleResource_ReturnsSingle
│       └── GetResourcesAsync_UsesNoTracking
└── pto.track.data.tests (1 test)
    └── UnitTest1
        └── Test1 (placeholder)
```

---

## Keyboard Shortcuts

### Test Execution
- **Run All Tests**: `Ctrl+Shift+P` → type "Test: Run All Tests"
- **Run Test at Cursor**: Place cursor in test method → `Ctrl+Shift+P` → "Test: Run Test at Cursor"
- **Debug Test at Cursor**: Place cursor in test method → `Ctrl+Shift+P` → "Test: Debug Test at Cursor"
- **Rerun Last Test**: `Ctrl+Shift+P` → "Test: Rerun Last Run"
- **Show Test Output**: `Ctrl+Shift+P` → "Test: Show Output"

### Building
- **Build Solution**: `Ctrl+Shift+B` (runs the default build task)
- **Run Task**: `Ctrl+Shift+P` → "Tasks: Run Task"
  - `build` - Build the entire solution
  - `test` - Run all tests
  - `watch` - Run app with hot reload
  - `publish` - Create release build

### Debugging
- **Start Debugging**: `F5` (launches web app with debugger attached)
- **Start Without Debugging**: `Ctrl+F5`
- **Toggle Breakpoint**: `F9`
- **Step Over**: `F10`
- **Step Into**: `F11`
- **Continue**: `F5`

---

## Command Palette Tasks

Press `Ctrl+Shift+P` and type:

### Testing Commands
- `Test: Run All Tests` - Execute all 45 tests
- `Test: Run Failed Tests` - Rerun only failed tests
- `Test: Debug Last Run` - Debug the last executed test
- `Test: Cancel Test Run` - Stop running tests
- `Test: Refresh Tests` - Reload test discovery

### Build Commands  
- `Tasks: Run Build Task` - Build solution (default: `build`)
- `Tasks: Run Task` → `test` - Run all tests via dotnet CLI
- `Tasks: Run Task` → `watch` - Start app with file watcher
- `Tasks: Run Task` → `publish` - Create release package

### Debug Commands
- `Debug: Start Debugging` - Launch app with debugger (F5)
- `Debug: Add Configuration` - Add new launch.json config
- `Debug: Select and Start Debugging` - Choose debug configuration

---

## Terminal Commands

Open integrated terminal: `` Ctrl+` `` or `View → Terminal`

```bash
# Navigate to solution root
cd /home/spinriko/code/dotnet/resource

# Run all tests (45 tests)
dotnet test

# Run specific test project
dotnet test pto.track.tests                    # 16 integration tests
dotnet test pto.track.services.tests           # 29 service layer tests
dotnet test pto.track.data.tests               # 1 placeholder test

# Run specific test class
dotnet test --filter "EventsControllerTests"
dotnet test --filter "EventServiceTests"
dotnet test --filter "DtoSerializationTests"

# Run specific test method
dotnet test --filter "GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents"

# Run with detailed output
dotnet test -v detailed

# Run with minimal output
dotnet test -v minimal

# Run and stop on first failure
dotnet test --blame-crash

# Run with code coverage (requires coverlet)
dotnet test /p:CollectCoverage=true

# Build then test
dotnet build && dotnet test --no-build

# Watch mode (reruns tests on file changes)
dotnet watch test --project pto.track.tests
```

---

## Debugging Tests

### Setting Breakpoints
1. Open the test file (e.g., `EventsControllerTests.cs`)
2. Click in the left margin (gutter) next to line numbers to set breakpoints (red dot)
3. Breakpoints work in:
   - Test methods
   - Controller methods
   - Service methods
   - Any code called by the test

### Debug a Single Test
1. **Option 1 - Test Explorer**:
   - Open Test Explorer (🧪 icon)
   - Click 🐛 next to the test name
   
2. **Option 2 - Code Lens**:
   - Hover over test method
   - Click "Debug Test" link that appears above the method
   
3. **Option 3 - Command Palette**:
   - Place cursor inside test method
   - `Ctrl+Shift+P` → "Test: Debug Test at Cursor"

### Debug Session Controls
When debugger is active:
- **Variables Panel**: View current variable values
- **Call Stack**: See method call hierarchy
- **Watch Panel**: Add expressions to monitor
- **Debug Console**: Execute code at current breakpoint
- **Continue (F5)**: Run until next breakpoint
- **Step Over (F10)**: Execute current line, don't enter methods
- **Step Into (F11)**: Enter method calls
- **Step Out (Shift+F11)**: Return to calling method

---

## Test Output and Logs

### View Test Results
- **Test Explorer**: Shows ✓/✗ icons next to each test
- **Output Panel**: `View → Output` → select ".NET Test Log" from dropdown
- **Problems Panel**: `Ctrl+Shift+M` to see compilation errors
- **Terminal**: Full test output with timing information

### Understanding Test Output
```
Test run for /home/spinriko/code/dotnet/resource/pto.track.tests/bin/Debug/net10.0/pto.track.tests.dll

Test summary: total: 16, failed: 0, succeeded: 16, skipped: 0, duration: 2.1s
```

### Viewing Detailed Logs
Integration tests log to console. View them in:
1. Terminal output when running `dotnet test`
2. Test Explorer → Right-click test → "View Test Output"
3. Output panel (`.NET Test Log` channel)

Expected log patterns:
```
info: Microsoft.EntityFrameworkCore.Update[30100]
      Saved 10 entities to in-memory store.
      
fail: pto.track.data.SchedulerDbContext[0]
      An error occurred migrating or creating the DB.
```
Note: Migration errors in test logs are **expected** for in-memory database tests.

---

## Troubleshooting

### Tests Not Appearing in Test Explorer
**Symptoms**: Test Explorer is empty or not showing all tests

**Solutions**:
1. Reload window: `Ctrl+Shift+P` → "Developer: Reload Window"
2. Verify .NET Test Explorer extension is installed and enabled
3. Build solution: `Ctrl+Shift+B` or `dotnet build`
4. Check test project references in `.csproj` files
5. Restart OmniSharp: `Ctrl+Shift+P` → "OmniSharp: Restart OmniSharp"

### Debugger Not Stopping at Breakpoints
**Symptoms**: Breakpoints are hollow circles, debugger doesn't pause

**Solutions**:
1. Ensure you clicked 🐛 (debug) not ▶️ (run)
2. Verify breakpoint is in code that actually executes
3. Check `launch.json` → `preLaunchTask` completes successfully
4. Build in Debug configuration: `dotnet build -c Debug`
5. Clear bin/obj folders: `dotnet clean && dotnet build`

### Test Failures After Code Changes
**Symptoms**: Previously passing tests now fail

**Solutions**:
1. Check if entity/DTO property names changed
2. Verify database seeding in test setup
3. Review recent changes to services or controllers
4. Run single failing test with `-v detailed` for more info
5. Check for timing issues in async tests

### "Program Not Found" Error
**Symptoms**: Tests fail with cannot find 'Program' or entry point

**Solutions**:
1. Verify `pto.track/Program.cs` has `public partial class Program { }`
2. Ensure `pto.track.tests` references `pto.track` project
3. Build main project: `dotnet build pto.track/pto.track.csproj`
4. Clean and rebuild: `dotnet clean && dotnet build`

### In-Memory Database Migration Errors
**Symptoms**: Log shows "An error occurred migrating or creating the DB"

**Status**: This is **EXPECTED** behavior in tests using in-memory database  
**Reason**: In-memory database doesn't support SQL migrations  
**Impact**: None - tests work correctly despite the logged error

### Performance Issues
**Symptoms**: Test Explorer slow to load, tests take long time

**Solutions**:
1. Close unnecessary test projects in Test Explorer
2. Run specific test projects instead of all tests
3. Increase test timeout in settings
4. Check for resource-intensive background processes
5. Consider upgrading hardware (RAM, SSD)

---

## Configuration Files

### .vscode/launch.json
Defines debug configurations. Key settings:
- `preLaunchTask`: "build" - ensures project builds before debugging
- `program`: Path to DLL to execute
- `cwd`: Working directory for the application
- `env`: Environment variables (e.g., `ASPNETCORE_ENVIRONMENT`)

### .vscode/tasks.json
Defines build and test tasks. Available tasks:
- `build` - Build entire solution
- `test` - Run all tests  
- `publish` - Create release build
- `watch` - Run with hot reload

### .vscode/settings.json (if present)
Configures test-related VS Code settings:
```json
{
  "dotnet-test-explorer.testProjectPath": "**/*tests.csproj",
  "dotnet-test-explorer.enableTelemetry": false
}
```

---

## Tips and Best Practices

### Efficient Testing Workflow
1. **Run Specific Tests First**: Don't always run all 45 tests
2. **Use Watch Mode**: `dotnet watch test` for rapid feedback
3. **Debug Strategically**: Set breakpoints before debugging
4. **Check Test Output**: Read error messages carefully
5. **Run Related Tests**: If you change EventService, run EventServiceTests

### Writing New Tests
1. Follow existing naming convention: `MethodName_Scenario_ExpectedResult`
2. Use AAA pattern: Arrange, Act, Assert
3. One assertion per test when possible
4. Use descriptive variable names
5. Add tests to appropriate project:
   - Controller/API tests → `pto.track.tests`
   - Service logic tests → `pto.track.services.tests`
   - Data layer tests → `pto.track.data.tests`

### Debugging Tips
1. **Console.WriteLine**: Add debug output in tests
2. **Immediate Window**: Evaluate expressions during debug
3. **Conditional Breakpoints**: Right-click breakpoint → Add condition
4. **Data Breakpoints**: Break when variable value changes
5. **Exception Settings**: Break on specific exception types

---

## Additional Resources

- **xUnit Documentation**: https://xunit.net/
- **EF Core Testing**: https://learn.microsoft.com/en-us/ef/core/testing/
- **ASP.NET Core Testing**: https://learn.microsoft.com/en-us/aspnet/core/test/
- **VS Code Debugging**: https://code.visualstudio.com/docs/editor/debugging

For detailed test descriptions and coverage, see [TESTING.md](TESTING.md)
