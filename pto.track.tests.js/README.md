# JavaScript Tests for PTO Track

Pure JavaScript tests using QUnit - no Node.js required!

## Running Tests

### Option 1: Open in Browser
Simply open `test-runner.html` in any web browser:
```bash
# Using xdg-open (Linux)
xdg-open test-runner.html

# Using default browser (Windows)
start test-runner.html

# Or just drag and drop the file into your browser
```

### Option 2: Live Server (VS Code)
1. Install "Live Server" extension in VS Code
2. Right-click `test-runner.html`
3. Select "Open with Live Server"

### Option 3: Headless (CI/CD)

**Linux/WSL:**
```bash
./run-headless.sh
```

**Windows (PowerShell):**
```powershell
.\run-headless.ps1
```

Both scripts:
- Start a temporary web server on port 9999
- Run tests in headless Microsoft Edge
- Display test results in the terminal
- **Save JUnit XML results to `test-results.xml`** for CI/CD integration
- Exit with code 0 (success) or 1 (failure)

## Test Structure

```
pto.track.tests.js/
├── test-runner.html          # Main test runner (open in browser)
├── mock-daypilot.js          # Mock DayPilot library
├── calendar-functions.js     # Symlink to production code
├── run-headless.sh           # Headless test runner for CI/CD
└── tests/
    ├── status-color.test.js       # 8 tests - Status color mapping
    ├── checkbox-filters.test.js   # 4 tests - Checkbox state management
    ├── url-builder.test.js        # 6 tests - API URL construction
    ├── role-detection.test.js     # 18 tests - User role logic
    └── impersonation.test.js      # 5 tests - Role switching

Production code location: ../pto.track/wwwroot/js/calendar-functions.js
```

## Test Coverage

**Total: 41 JavaScript tests**

### Status Colors (8 tests)
- ✓ Returns correct colors for all statuses
- ✓ Handles null/undefined
- ✓ Case sensitivity
- ✓ Default color fallback

### Checkbox Filters (4 tests)
- ✓ Collects checked statuses
- ✓ Handles empty selection
- ✓ Returns all when all checked
- ✓ Single checkbox selection

### URL Builder (6 tests)
- ✓ Adds status parameters
- ✓ Adds employeeId for employees
- ✓ Excludes employeeId for managers/admins
- ✓ Multiple status filters
- ✓ Empty status array

### Role Detection (18 tests)
- ✓ Determines user role from roles array
- ✓ Prioritizes Admin > Manager > Approver > Employee
- ✓ Default status filters per role
- ✓ Visible filter checkboxes per role
- ✓ Manager/Approver detection
- ✓ Case-insensitive role matching
- ✓ Handles null/undefined users

### Impersonation (5 tests)
- ✓ Admin sees all checkboxes and statuses
- ✓ Manager sees limited checkboxes
- ✓ Employee sees all but selects only Pending
- ✓ EmployeeId filtering per role
- ✓ Role switching updates filters

## Benefits

- **No Build Step**: Pure HTML/JS, no compilation needed
- **Browser-Based**: Run directly in any browser
- **Visual Feedback**: QUnit provides clear pass/fail UI
- **Fast**: Instant test execution
- **Portable**: Works on any OS with a browser
- **Easy to Debug**: Use browser DevTools to debug tests

## Adding New Tests

1. Create a new test file in `tests/` directory:
```javascript
QUnit.module('My Feature', function() {
    QUnit.test('should do something', function(assert) {
        const result = myFunction();
        assert.equal(result, expected, "Description");
    });
});
```

2. Add script tag to `test-runner.html`:
```html
<script src="tests/my-feature.test.js"></script>
```

## Exporting Test Results

### Manual Export (Browser)
1. Open `test-runner.html` in browser
2. Click the **"📥 Download JUnit XML Results"** button
3. Save `test-results.xml` file

### Automated Export (CI/CD)
Run tests in headless Edge:

```bash
cd pto.track.tests.js
./run-headless.sh
```

**WSL Support**: Automatically detects WSL and uses Windows Edge.

**Output**: Generates `test-results.xml` in the same directory.

The `test-results.xml` file is in JUnit format, compatible with:
- Azure DevOps
- Jenkins
- GitHub Actions
- GitLab CI
- TeamCity
- Any CI system supporting JUnit XML

### GitHub Actions Integration

The `.github/workflows/js-tests.yml` workflow is included:
- Runs on every push and pull request
- Executes tests in headless Chrome (Linux) or Edge (Windows)
- Uploads test results as artifacts
- Reports pass/fail status

For Windows CI/CD pipelines, use `run-headless.ps1` instead of the bash script.

## Integration with CI/CD

Example VS Code task (Linux/WSL):
```json
{
    "label": "Run JS Tests (Headless)",
    "type": "shell",
    "command": "./run-headless.sh",
    "options": {
        "cwd": "${workspaceFolder}/pto.track.tests.js"
    }
}
```

Example VS Code task (Windows):
```json
{
    "label": "Run JS Tests (Headless - Windows)",
    "type": "shell",
    "command": "powershell -ExecutionPolicy Bypass -File run-headless.ps1",
    "options": {
        "cwd": "${workspaceFolder}/pto.track.tests.js"
    }
}
```

## Why QUnit?

- **Simple**: No build tools, no package managers
- **Proven**: Used by jQuery, WordPress, and many others
- **Well-documented**: Extensive documentation at qunitjs.com
- **Browser DevTools**: Easy debugging with familiar tools
- **Pure JavaScript**: No transpilation needed
