# JavaScript Tests for PTO Track

Pure JavaScript tests using Jest with ES Modules - Node.js 20+ required!

## Running Tests

```bash
# Run all tests with linting
npm test

# Run tests with coverage report
npm run test:coverage

# Run tests in watch mode (for TDD)
npm run test:watch

# Run linter only
npm run lint
```

## Code Quality Metrics

### Cyclomatic Complexity

ESLint is configured to warn when function complexity exceeds 10, helping maintain code quality and testability.

**Configuration** (`.eslintrc.json`):
```json
{
  "rules": {
    "complexity": ["warn", { "max": 10 }]
  }
}
```

**Benefits**:
- Identifies overly complex functions that may be hard to test
- Encourages breaking down large functions into smaller, testable units
- TypeScript-ready with `@typescript-eslint/parser` and `@typescript-eslint/eslint-plugin`
- Runs automatically before every test execution

**What It Measures**:
- Decision points: if, while, for, switch cases
- Logical operators: &&, ||
- Ternary operators: ? :
- Optional chaining with branches: ?.

**Example Warning**:
```
  32:1  warning  Function has a complexity of 12  complexity
```

When you see this warning, consider refactoring the function into smaller, single-purpose functions.

## Test Reports

### JUnit XML Report
After running `npm test`, a JUnit XML report is generated at:
- `test-results/jest-junit.xml`

This report is compatible with:
- VS Code Test Explorer
- Azure DevOps
- Jenkins
- GitHub Actions
- GitLab CI
- Any CI system supporting JUnit XML

### Coverage Reports

**Note**: Coverage collection with ES modules and `--experimental-vm-modules` has limitations in Jest. The coverage reports may show 0% even though tests are passing. This is a known issue with Jest's coverage in experimental VM modules mode.

**Workaround**: The test suite provides comprehensive functional coverage through 164 tests covering all exported functions. See `TEST-STRUCTURE.md` for detailed coverage breakdown.

If coverage metrics are critical, consider:
1. Using a different test runner (Vitest has better ESM support)
2. Transpiling to CommonJS for testing
3. Waiting for Jest's stable ESM support

## Test Structure

```
pto.track.tests.js/
├── jest.config.js            # Jest configuration
├── eslint.config.js          # ESLint configuration
├── .c8rc.json               # Coverage configuration
├── package.json              # Dependencies and scripts
├── test-results/             # JUnit XML reports
│   └── jest-junit.xml
├── coverage/                 # Coverage reports (HTML)
│   └── index.html
└── tests/
    ├── unit/                         # Unit tests (148 tests)
    │   ├── core/                     # Core business logic (58 tests)
    │   │   ├── role-detection.test.mjs      # 54 tests
    │   │   ├── url-builder.test.mjs         # 15 tests
    │   │   └── calendar-functions.test.mjs  # 3 tests
    │   ├── filters/                  # Filter management (18 tests)
    │   │   ├── checkbox-filters.test.mjs
    │   │   └── checkbox-visibility.test.mjs
    │   ├── permissions/              # Access control (26 tests)
    │   │   ├── employee-restrictions.test.mjs
    │   │   └── impersonation.test.mjs
    │   └── presentation/             # UI presentation (37 tests)
    │       ├── context-menu.test.mjs
    │       └── status-color.test.mjs
    └── integration/                  # Integration tests (16 tests)
        └── workflows.test.mjs

Production code: ../pto.track/wwwroot/js/calendar-functions.mjs
```

## Test Coverage

**Total: 164 JavaScript tests** (all passing ✓)

See `TEST-STRUCTURE.md` for detailed breakdown by category.

### Categories
- **Core** (58 tests): Role detection, URL building, filter management
- **Filters** (18 tests): Checkbox state and visibility
- **Permissions** (26 tests): Access control and restrictions
- **Presentation** (37 tests): Context menus and status colors
- **Integration** (16 tests): Real-world workflows

## Benefits

- **ES Modules**: Modern JavaScript with native import/export
- **No Build Step**: Direct Node.js execution with `--experimental-vm-modules`
- **Fast**: ~1.5s execution time for all 164 tests
- **Type Safe**: ESLint validation before every test run
- **CI/CD Ready**: JUnit XML output for all CI systems
- **Organized**: Clear folder structure by feature/concern
- **Comprehensive**: 164 tests covering all 10 exported functions

## Adding New Tests

1. Create a new test file in appropriate `tests/` subdirectory:
```javascript
import { myFunction } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";

describe('My Feature', () => {
    it('should do something', () => {
        const result = myFunction();
        expect(result).toBe(expected);
    });
});
```

2. Run tests:
```bash
npm test
```

Jest automatically discovers `*.test.mjs` files.

## Integration with CI/CD

### GitHub Actions
```yaml
- name: Run JavaScript Tests
  run: |
    cd pto.track.tests.js
    npm ci
    npm test
  
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: jest-results
    path: pto.track.tests.js/test-results/jest-junit.xml
```

### Azure DevOps
```yaml
- task: Npm@1
  inputs:
    command: 'ci'
    workingDir: 'pto.track.tests.js'

- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'test'
    workingDir: 'pto.track.tests.js'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: 'pto.track.tests.js/test-results/jest-junit.xml'
```

## Why Jest?

- **Industry Standard**: Most popular JavaScript testing framework
- **ES Module Support**: Works with modern JavaScript (experimental but stable)
- **Rich Ecosystem**: JUnit reporters, coverage tools, VS Code integration
- **Excellent DX**: Watch mode, clear error messages, fast execution
- **Well-documented**: Extensive documentation at jestjs.io
