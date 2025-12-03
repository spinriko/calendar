# JavaScript Test Suite Structure

**Total: 164 tests** | **10 test suites** | All passing ✓

## Organization

```
pto.track.tests.js/
├── tests/
│   ├── unit/                           # Unit tests (148 tests)
│   │   ├── core/                       # Core business logic (58 tests)
│   │   │   ├── role-detection.test.mjs      # 54 tests - User role determination & permissions
│   │   │   ├── url-builder.test.mjs         # 15 tests - API URL construction
│   │   │   └── calendar-functions.test.mjs  #  3 tests - Basic function tests
│   │   │
│   │   ├── filters/                    # Filter management (18 tests)
│   │   │   ├── checkbox-filters.test.mjs         #  9 tests - Status checkbox state
│   │   │   └── checkbox-visibility.test.mjs      #  9 tests - Role-based filter visibility
│   │   │
│   │   ├── permissions/                # Access control (26 tests)
│   │   │   ├── employee-restrictions.test.mjs    # 20 tests - Resource creation permissions
│   │   │   └── impersonation.test.mjs            #  6 tests - Role switching behavior
│   │   │
│   │   └── presentation/               # UI presentation (37 tests)
│   │       ├── context-menu.test.mjs        # 24 tests - Context menu item generation
│   │       └── status-color.test.mjs        # 13 tests - Status color mapping
│   │
│   └── integration/                    # Integration tests (16 tests)
│       └── workflows.test.mjs               # 16 tests - Cross-function workflows
│
├── package.json                        # Test runner configuration
├── jest.config.js                      # Jest ES module setup
├── eslint.config.js                    # Linting rules
└── README.md                           # Test documentation
```

## Test Categories

### Core Business Logic (58 tests)
Tests fundamental calendar application logic:
- **Role Detection** (54 tests)
  - `determineUserRole`: User role hierarchy (Admin > Manager > Approver > Employee)
  - `getDefaultStatusFilters`: Default filter states per role
  - `getVisibleFilters`: Role-based filter visibility
  - `isUserManagerOrApprover`: Manager/Approver detection with case-insensitive matching
  - Edge cases: null/undefined users, empty roles, case sensitivity

- **URL Builder** (15 tests)
  - Status query parameter construction
  - Employee ID filtering logic
  - Role-specific URL generation
  - Edge cases: empty statuses, existing query params

### Filter Management (18 tests)
Tests filter state and visibility:
- **Checkbox Filters** (9 tests)
  - Status selection from checkbox state
  - Order preservation (Pending, Approved, Rejected, Cancelled)
  - Empty/full selection handling

- **Checkbox Visibility** (9 tests)
  - Role-based filter availability
  - Admin/Employee: all 4 filters
  - Manager/Approver: Pending + Approved only

### Permissions & Access Control (26 tests)
Tests who can do what:
- **Employee Restrictions** (20 tests)
  - `canCreateAbsenceForResource`: Admin/Manager/Approver can create for anyone, Employees only for self
  - `getResourceSelectionMessage`: User-friendly error messages
  - Edge cases: string vs number IDs, null/undefined IDs, zero IDs

- **Impersonation** (6 tests)
  - Role switching behavior
  - Filter updates on role change
  - Permission changes during impersonation

### Presentation Layer (37 tests)
Tests UI display logic:
- **Context Menu** (24 tests)
  - `buildContextMenuItems`: Role × Status matrix
  - Status-specific actions (Pending: Approve/Reject, Approved: View only, etc.)
  - Ownership-based actions (Edit/Delete)
  - Separator logic
  - onClick action validation

- **Status Colors** (13 tests)
  - Color mapping for each status
  - Case sensitivity (exact match required)
  - Default color for unknown statuses
  - Edge cases: null, undefined, empty string

### Integration Workflows (16 tests)
Tests real-world user scenarios:
- **Role → Filters → URL Workflow** (4 tests)
  - Admin: All statuses, no employeeId filter
  - Manager: Pending + Approved, no employeeId filter
  - Employee: Pending only, includes employeeId (except Approved-only)

- **Permission Check Workflow** (3 tests)
  - Role determination → permission check → error message

- **Filter Selection Workflow** (3 tests)
  - Visible filters → user selection → URL construction

- **Context Menu Matrix** (6 tests)
  - Different roles viewing different statuses
  - Comprehensive permission validation

## Running Tests

```bash
# Run all tests with linting
npm test

# Watch mode (not yet configured)
npm run test:watch

# Coverage report (not yet configured)
npm run test:coverage
```

## Test Quality Metrics

- **Coverage**: Comprehensive coverage of all exported functions
- **Edge Cases**: Extensive null/undefined/empty/type variation testing
- **Integration**: Real-world workflow validation
- **Readability**: Descriptive test names, organized by scenario
- **Performance**: ~1.5s total execution time

## Recent Improvements

1. ✅ Migrated from QUnit to Jest with ES modules
2. ✅ Expanded from 38 to 164 tests (4.3x increase)
3. ✅ Added ESLint pre-test validation
4. ✅ Organized into logical folder structure
5. ✅ Renamed files for clarity (`event-presentation` → `context-menu`)
6. ✅ Removed duplicate CommonJS test files
7. ✅ Comprehensive edge case coverage

## Next Steps

Consider adding:
- [ ] Code coverage reporting (Istanbul/c8)
- [ ] Watch mode for TDD workflow
- [ ] Performance benchmarks
- [ ] Visual regression tests for calendar UI
- [ ] Mutation testing for test quality validation
