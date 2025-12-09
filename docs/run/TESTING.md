# Testing Documentation

This solution includes comprehensive test coverage across multiple test projects, ensuring reliability and maintainability of the codebase.

## Test Projects Overview

### Summary Statistics
- **Total Tests**: 340 (337 passing ✓, 3 skipped)
- **C# Test Projects**: 3 projects with 157 tests
- **TypeScript Test Suites**: 13 suites with 183 tests
- **Code Coverage**: 67.9% overall (C# code)
- **Test Coverage**: Controllers, Services, Integration workflows, JSON serialization, Entity validation, User management, Status filtering, Authorization & Role-based access control, Transaction management, User synchronization, TypeScript business logic

## TypeScript Tests

### pto.track.tests.js (Jest/TypeScript)
**Total Tests**: 164  
**Technology**: Jest 30.2.0, jsdom environment, TypeScript, ESLint validation

Comprehensive TypeScript unit and integration tests for client-side calendar business logic. All tests run with ESLint pre-validation to ensure code quality.

#### Test Structure

```
pto.track.tests.js/
├── tests/
│   ├── unit/                           # Unit tests (148 tests)
│   │   ├── core/                       # Core business logic (58 tests)
│   │   │   ├── role-detection.test.ts       # 54 tests - User role determination & permissions
│   │   │   ├── url-builder.test.ts          # 15 tests - API URL construction
│   │   │   └── calendar-functions.test.ts   #  3 tests - Basic function tests
│   │   │
│   │   ├── filters/                    # Filter management (18 tests)
│   │   │   ├── checkbox-filters.test.ts          #  9 tests - Status checkbox state
│   │   │   └── checkbox-visibility.test.ts       #  9 tests - Role-based filter visibility
│   │   │
│   │   ├── permissions/                # Access control (26 tests)
│   │   │   ├── employee-restrictions.test.ts     # 20 tests - Resource creation permissions
│   │   │   └── impersonation.test.ts             #  6 tests - Role switching behavior
│   │   │
│   │   └── presentation/               # UI presentation (37 tests)
│   │       ├── context-menu.test.ts         # 24 tests - Context menu item generation
│   │       └── status-color.test.ts         # 13 tests - Status color mapping
│   │
│   └── integration/                    # Integration tests (19 tests)
│       ├── workflows.test.ts                # 16 tests - Cross-function workflows
│       └── impersonation-flow.test.ts       #  3 tests - Full impersonation flow verification
│
├── tests/scheduler-permissions.test.ts      # 10 tests - Scheduler row coloring & selection
├── tests/date-validation.test.ts            #  6 tests - Retroactive date prevention
├── package.json                        # Test runner configuration
├── jest.config.js                      # Jest TypeScript setup
├── tsconfig.json                       # TypeScript configuration
├── eslint.config.js                    # Linting rules
└── TEST-STRUCTURE.md                   # Detailed test documentation
```

#### Core Business Logic (58 tests)
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

#### Filter Management (18 tests)
Tests filter state and visibility:
- **Checkbox Filters** (9 tests)
  - Status selection from checkbox state
  - Order preservation (Pending, Approved, Rejected, Cancelled)
  - Empty/full selection handling

- **Checkbox Visibility** (9 tests)
  - Role-based filter availability
  - Admin/Employee: all 4 filters
  - Manager/Approver: Pending + Approved only

#### Permissions & Access Control (26 tests)
Tests who can do what:
- **Employee Restrictions** (20 tests)
  - `canCreateAbsenceForResource`: Admin/Manager/Approver can create for anyone, Employees only for self
  - `getResourceSelectionMessage`: User-friendly error messages
  - Edge cases: string vs number IDs, null/undefined IDs, zero IDs

- **Impersonation** (6 tests)
  - Role switching behavior
  - Filter updates on role change
  - Permission changes during impersonation

#### Presentation Layer (37 tests)
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

#### Integration Workflows (16 tests)
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

#### Scheduler Logic (19 tests)
Tests for the enhanced scheduler UI:
- **Scheduler Permissions** (10 tests)
  - `getSchedulerRowColor`: Verifies gray color for unauthorized rows
  - `shouldAllowSelection`: Verifies selection blocking for unauthorized rows
  - Role-based overrides (Admin/Manager can select anyone)

- **Date Validation** (6 tests)
  - `getCellCssClass`: Verifies gray color for past dates (retroactive prevention)
  - Ensures past dates are disabled even for the owner
  - Ensures future dates respect role permissions

- **Impersonation Flow** (3 tests)
  - Integration test for the impersonation panel
  - Verifies role mapping and API calls

#### Running TypeScript Tests

```bash
# Run all tests with linting
cd pto.track.tests.js
npm test

# Run tests without linting
npm run test:only
```

**Key Testing Features**:
- TypeScript support via ts-jest
- ES module support
- ESLint validation runs before tests
- Fresh jsdom environment per test
- Tests pure functions in isolation
- Integration tests verify cross-function workflows
- ~1.5s total execution time for 164 tests

---

## Code Quality Metrics

### Comprehensive Code Metrics Analysis

The test suite includes comprehensive code quality metrics analysis across all C# projects in the solution using custom Roslyn analyzers.

**Run All Metrics**:
```bash
dotnet test pto.track.tests/pto.track.tests.csproj --filter "FullyQualifiedName~CodeMetricsAnalyzer"
```

**Scope**: Analyzes `pto.track`, `pto.track.services`, and `pto.track.data` projects

#### 1. Cyclomatic Complexity Analysis
**Threshold**: Complexity > 10  
**What It Measures**:
- Decision points: if, while, for, foreach, switch cases
- Logical operators: && and ||
- Exception handling: catch clauses
- Ternary operators: ? :

**Output Example**:
```
=== Cyclomatic Complexity Report ===
Analyzed 65 source files

✓ All methods have acceptable complexity (≤10)
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeProjectComplexity"
```

#### 2. Maintainability Index
**Threshold**: Index < 65 triggers report  
**Scale**: 0-100 (higher = more maintainable)  
- 85-100: Excellent maintainability
- 65-84: Good maintainability
- <65: Low maintainability (consider refactoring)

**Formula**: `171 - 5.2 * ln(Volume) - 0.23 * Complexity - 16.2 * ln(Lines)`

**Output Example**:
```
=== Maintainability Index Report ===
Analyzed 65 source files
Scale: 0-100 (65+ = Good, 85+ = Excellent)

Found 72 method(s) with low maintainability (<65):
  [34.3] CurrentUserController.GetCurrentUser
      in pto.track\Controllers\CurrentUserController.cs
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeMaintainabilityIndex"
```

#### 3. Lines of Code Analysis
**Thresholds**:
- Methods: > 50 lines
- Classes: > 500 lines

**Output Example**:
```
=== Lines of Code Report ===
Analyzed 65 source files

Found 3 method(s) > 50 lines:
  [81 lines] CurrentUserController.GetCurrentUser
      in pto.track\Controllers\CurrentUserController.cs

✓ All classes ≤500 lines
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeLinesOfCode"
```

#### 4. Method Parameter Count
**Threshold**: > 5 parameters  
**Recommendation**: Consider using parameter objects or builder pattern for methods exceeding this threshold

**Output Example**:
```
=== Method Parameter Count Report ===
Analyzed 65 source files

✓ All methods have ≤5 parameters
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeMethodParameters"
```

#### 5. Nesting Depth Analysis
**Threshold**: > 4 levels  
**Recommendation**: Extract nested logic to separate methods

**Output Example**:
```
=== Nesting Depth Report ===
Analyzed 65 source files

Found 1 method(s) with nesting depth >4:
  [depth 6] CurrentUserController.GetCurrentUser
      in pto.track\Controllers\CurrentUserController.cs
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeNestingDepth"
```

#### 6. Class Coupling Analysis
**Threshold**: > 10 dependencies  
**What It Measures**: Field types, property types, constructor parameters (excluding built-in types)

**Output Example**:
```
=== Class Coupling Report ===
Analyzed 65 source files

✓ All classes have ≤10 dependencies
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeClassCoupling"
```

#### 7. Inheritance Depth Analysis
**Threshold**: > 4 levels  
**What It Measures**: Depth of inheritance tree

**Output Example**:
```
=== Inheritance Depth Report ===
Analyzed 65 source files

✓ All classes have inheritance depth ≤4
```

**Individual Test**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.AnalyzeInheritanceDepth"
```

#### 8. Comprehensive Summary Report
Combines all metrics into a single dashboard view.

**Run Command**:
```bash
dotnet test --filter "FullyQualifiedName~CodeMetricsAnalyzer.GenerateComprehensiveSummary"
```

**Output Example**:
```
=== Comprehensive Code Quality Summary ===
Solution: pto.track (all C# projects)

Code Base Statistics:
  Files:   65
  Lines:   4,375
  Classes: 56
  Methods: 155

Cyclomatic Complexity:
  Average: 1.9
  Median:  1.0
  Max:     13
  >10:     1 methods

Maintainability Index (0-100):
  Average: 64.7
  Median:  67.8
  Min:     34.3
  <65:     72 methods

Method Parameters:
  Average: 1.3
  Max:     5
  >5:      0 methods

✓ Analysis complete
```

### JavaScript Complexity Analysis
**Tool**: ESLint with complexity plugin  
**Threshold**: Complexity > 10 triggers a warning  
**Run Command**: Runs automatically with `npm test` (ESLint validation before tests)  
**Configuration**: `.eslintrc.json` with `complexity` rule set to `["warn", { "max": 10 }]`

**Benefits**:
- Prevents overly complex functions from being committed
- TypeScript-ready (using @typescript-eslint/parser and @typescript-eslint/eslint-plugin)
- Integrated into development workflow

---

## Test Projects

### 1. pto.track.tests (Integration Tests)
**Total Tests**: 50  
**Technology**: xUnit 2.9.3, Microsoft.AspNetCore.Mvc.Testing, EF Core In-Memory Database, Moq 4.20.72

Integration tests that verify the entire application stack works together correctly, from HTTP requests through controllers and services to the database layer. Includes comprehensive authorization and role-based access control tests.

#### EventsControllerTests.cs (8 tests)
Tests the `/api/events` API endpoints:

1. **GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents**  
   Verifies date range filtering returns only events within the specified start/end dates

2. **GetSchedulerEvent_WithValidId_ReturnsEvent**  
   Confirms retrieving a single event by ID returns correct data

3. **GetSchedulerEvent_WithInvalidId_ReturnsNotFound**  
   Ensures 404 Not Found is returned for non-existent event IDs

4. **PostSchedulerEvent_WithValidEvent_ReturnsCreatedAtAction**  
   Tests event creation returns 201 Created with correct location header

5. **PutSchedulerEvent_WithValidIdAndEvent_ReturnsNoContent**  
   Validates event updates work correctly and return 204 No Content

6. **PutSchedulerEvent_WithMismatchedId_ReturnsNotFound**  
   Ensures updating non-existent events returns 404 Not Found

7. **DeleteSchedulerEvent_WithValidId_ReturnsNoContent**  
   Confirms event deletion returns 204 No Content

8. **DeleteSchedulerEvent_WithInvalidId_ReturnsNotFound**  
   Verifies deleting non-existent events returns 404 Not Found

#### ResourcesControllerTests.cs (3 tests)
Tests the `/api/resources` API endpoints:

1. **GetResources_ReturnsAllResources**  
   Verifies all resources are returned from the GET endpoint

2. **GetResources_WithNoResources_ReturnsEmptyList**  
   Ensures empty array is returned when no resources exist

3. **GetResources_ReturnsResourcesWithCorrectData**  
   Validates resource data (ID, Name) is serialized correctly

#### IntegrationTests.cs (5 tests)
End-to-end integration tests:

1. **GetResources_ReturnsSeededResources**  
   Verifies seeded test data is accessible via API

2. **GetEvents_ReturnsSeededEventsForRange**  
   Tests event retrieval with date filtering on seeded data

3. **Events_EndToEnd_CRUD_Works**  
   Complete workflow test: Create → Read → Update → Delete an event

4. **PostSchedulerEvent_InvalidDates_ReturnsBadRequest**  
   Validates that events with end date before start date are rejected

5. **PutSchedulerEvent_InvalidDates_ReturnsBadRequest**  
   Ensures updates with invalid date ranges are rejected

#### AbsencesAuthorizationTests.cs (18 tests)
Tests role-based access control and authorization in the AbsencesController:

**Approve Authorization Tests (6 tests)**:
1. **ApproveAbsenceRequest_WithManagerRole_Succeeds** - Manager can approve
2. **ApproveAbsenceRequest_WithApproverRole_Succeeds** - Approver can approve
3. **ApproveAbsenceRequest_WithAdminRole_Succeeds** - Admin can approve
4. **ApproveAbsenceRequest_WithEmployeeRole_ReturnsForbid** - Employee cannot approve
5. **ApproveAbsenceRequest_WithMismatchedApproverId_ReturnsBadRequest** - Approver ID must match current user
6. **ApproveAbsenceRequest_WithoutRole_LogsWarning** - Logs security warning

**Reject Authorization Tests (3 tests)**:
1. **RejectAbsenceRequest_WithManagerRole_Succeeds** - Manager can reject
2. **RejectAbsenceRequest_WithEmployeeRole_ReturnsForbid** - Employee cannot reject
3. **RejectAbsenceRequest_WithMismatchedApproverId_ReturnsBadRequest** - Approver ID must match current user

**Update Authorization Tests (4 tests)**:
1. **PutAbsenceRequest_UserUpdatesOwnRequest_Succeeds** - User can update own pending request
2. **PutAbsenceRequest_UserUpdatesOthersRequest_ReturnsForbid** - User cannot update others' requests
3. **PutAbsenceRequest_UnauthenticatedUser_ReturnsUnauthorized** - Unauthenticated access denied
4. **PutAbsenceRequest_NonexistentRequest_ReturnsNotFound** - 404 for non-existent requests

**Cancel Authorization Tests (3 tests)**:
1. **CancelAbsenceRequest_UserCancelsOwnRequest_Succeeds** - User can cancel own request
2. **CancelAbsenceRequest_UserCancelsOthersRequest_ReturnsForbid** - User cannot cancel others' requests
3. **CancelAbsenceRequest_UnauthenticatedUser_ReturnsUnauthorized** - Unauthenticated access denied

**Logging Verification Tests (2 tests)**:
1. **PutAbsenceRequest_WrongUser_LogsWarning** - Security violations are logged
2. **CancelAbsenceRequest_WrongUser_LogsWarning** - Unauthorized attempts are logged

**Key Testing Features**:
- Uses `WebApplicationFactory<Program>` for in-memory test server
- Fresh database per test via in-memory provider
- Tests actual HTTP responses and status codes
- Validates both happy path and error scenarios
- Mock-based unit tests for authorization logic
- Verifies security logging for audit trails

---

### 2. pto.track.services.tests (Service Layer Unit Tests)
**Total Tests**: 83  
**Technology**: xUnit 3.1.4, EF Core In-Memory Database

Unit tests for the business logic layer, ensuring services work correctly in isolation.

#### EventServiceTests.cs (14 tests)
Tests the `EventService` business logic:

1. **GetEventsAsync_WithEventsInDateRange_ReturnsMatchingEvents**  
   Verifies date range filtering logic in service layer

2. **GetEventsAsync_WithNoEventsInRange_ReturnsEmpty**  
   Ensures empty results when no events match date range

3. **GetEventsAsync_WithOverlappingEvents_ReturnsCorrectly**  
   Tests edge cases where events partially overlap date range

4. **GetEventByIdAsync_WithValidId_ReturnsEvent**  
   Confirms single event retrieval by ID works correctly

5. **GetEventByIdAsync_WithInvalidId_ReturnsNull**  
   Validates null is returned for non-existent IDs

6. **CreateEventAsync_WithValidDto_CreatesAndReturnsEvent**  
   Tests event creation from DTO and entity mapping

7. **CreateEventAsync_WithNullableFields_CreatesSuccessfully**  
   Ensures events can be created with optional fields (Text, Color) set to null

8. **UpdateEventAsync_WithValidIdAndDto_UpdatesEvent**  
   Verifies event updates modify all fields correctly

9. **UpdateEventAsync_WithInvalidId_ReturnsFalse**  
   Confirms false is returned when updating non-existent events

10. **DeleteEventAsync_WithValidId_DeletesEvent**  
    Tests event deletion and confirms removal from database

11. **DeleteEventAsync_WithInvalidId_ReturnsFalse**  
    Validates false is returned when deleting non-existent events

12. **CreateEventAsync_PreservesAllFields**  
    Ensures all DTO fields are correctly mapped to entity fields

13. **GetEventsAsync_WithMultipleResources_ReturnsAllEvents**  
    Tests events across different resources are all retrieved

#### ResourceServiceTests.cs (7 tests)
Tests the `ResourceService` business logic:

1. **GetResourcesAsync_WithMultipleResources_ReturnsAll**  
   Verifies all resources are returned

2. **GetResourcesAsync_WithNoResources_ReturnsEmpty**  
   Ensures empty list is returned when no resources exist

3. **GetResourcesAsync_ReturnsCorrectData**  
   Validates resource data mapping from entities to DTOs

4. **GetResourcesAsync_ReturnsDtosNotEntities**  
   Confirms DTOs are returned, not raw entities

5. **GetResourcesAsync_ReturnsInDatabaseOrder**  
   Tests resources are returned in insertion order

6. **GetResourcesAsync_WithSingleResource_ReturnsSingle**  
   Validates single resource scenarios

7. **GetResourcesAsync_UsesNoTracking**  
   Ensures queries use AsNoTracking for performance

8. **GetActiveResourcesAsync_FiltersInactiveResources**  
   Tests filtering to return only active resources (IsActive=true)

9. **GetActiveResourcesAsync_WithAllInactive_ReturnsEmpty**  
   Validates empty results when all resources are inactive

10. **GetApproversAsync_ReturnsOnlyActiveApprovers**  
    Tests filtering to return only active approvers (IsApprover=true AND IsActive=true)

11. **GetApproversAsync_WithNoApprovers_ReturnsEmpty**  
    Confirms empty results when no resources are marked as approvers

12. **GetResourceByIdAsync_WithValidId_ReturnsResource**  
    Tests single resource retrieval by ID with all DTO properties

13. **GetResourceByIdAsync_WithInvalidId_ReturnsNull**  
    Validates null is returned for non-existent resource IDs

14. **GetResourcesAsync_IncludesAllNewProperties**  
    Verifies all new DTO fields (Email, EmployeeNumber, Role, IsApprover, IsActive, Department) are correctly mapped

#### AbsenceServiceTests.cs (21 tests)
Tests the `AbsenceService` approval workflow business logic:

1. **GetAbsencesAsync_WithAbsencesInDateRange_ReturnsMatchingAbsences**  
   Verifies date range filtering logic

2. **GetAbsencesAsync_WithNoAbsencesInRange_ReturnsEmpty**  
   Ensures empty results when no absences match date range

3. **GetAbsencesByEmployeeAsync_WithValidEmployeeId_ReturnsEmployeeAbsences**  
   Tests filtering by employee ID

4. **GetAbsencesByEmployeeAsync_WithNoAbsences_ReturnsEmpty**  
   Validates empty results for employees with no absences

5. **GetPendingAbsencesAsync_ReturnsOnlyPendingAbsences**  
   Confirms only Pending status absences are returned

6. **GetPendingAbsencesAsync_WithNoPending_ReturnsEmpty**  
   Ensures empty results when no pending absences exist

7. **GetAbsenceByIdAsync_WithValidId_ReturnsAbsence**  
   Tests single absence retrieval by ID

8. **GetAbsenceByIdAsync_WithInvalidId_ReturnsNull**  
   Validates null is returned for non-existent IDs

9. **CreateAbsenceAsync_WithValidDto_CreatesAndReturnsAbsence**  
   Tests absence creation from DTO with default Pending status

10. **CreateAbsenceAsync_SetsDefaultStatus_ToPending**  
    Confirms new absences default to Pending status

11. **UpdateAbsenceAsync_WithValidIdAndDto_UpdatesAbsence**  
    Verifies absence updates modify all fields correctly

12. **UpdateAbsenceAsync_WithInvalidId_ReturnsFalse**  
    Confirms false is returned when updating non-existent absences

13. **ApproveAbsenceAsync_WithValidId_ApprovesAbsence**  
    Tests approval workflow sets Status to Approved

14. **ApproveAbsenceAsync_SetsApproverIdAndDate**  
    Validates approver ID and approval date are set

15. **ApproveAbsenceAsync_WithInvalidId_ReturnsFalse**  
    Confirms false is returned for non-existent absence IDs

16. **RejectAbsenceAsync_WithValidId_RejectsAbsence**  
    Tests rejection workflow sets Status to Rejected

17. **RejectAbsenceAsync_SetsApproverIdAndComments**  
    Validates approver ID, date, and comments are set on rejection

18. **CancelAbsenceAsync_WithValidId_CancelsAbsence**  
    Tests cancellation workflow sets Status to Cancelled

19. **CancelAbsenceAsync_WithInvalidId_ReturnsFalse**  
    Confirms false is returned for non-existent absence IDs

20. **DeleteAbsenceAsync_WithValidId_DeletesAbsence**  
    Tests absence deletion and confirms removal from database

21. **DeleteAbsenceAsync_WithInvalidId_ReturnsFalse**  
    Validates false is returned when deleting non-existent absences

22. **GetAbsenceRequestsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus**  
    Tests filtering by AbsenceStatus (e.g., Approved) returns only matching absences

23. **GetAbsenceRequestsAsync_WithPendingStatusFilter_ReturnsOnlyPending**  
    Validates filtering specifically for Pending status absences

24. **GetAbsenceRequestsAsync_WithNullStatus_ReturnsAllStatuses**  
    Confirms that when status parameter is null, all absences are returned

25. **GetAbsenceRequestsAsync_WithCancelledStatusFilter_ReturnsCancelled**  
    Tests filtering for Cancelled status absences

26. **GetAbsenceRequestsAsync_WithStatusAndDateRange_AppliesBothFilters**  
    Validates that status filtering works correctly in combination with date range filtering

#### UnitOfWorkTests.cs (11 tests - 8 passing, 3 skipped)
Tests the `UnitOfWork` transaction management:

1. **SaveChangesAsync_WithChanges_SavesSuccessfully**  
   Verifies SaveChangesAsync persists changes to the database

2. **SaveChangesAsync_WithCancellationToken_PropagatesToken**  
   Tests that CancellationToken is properly passed through to EF Core

3. **BeginTransactionAsync_StartsTransaction** ⚠️ *Skipped*  
   *InMemory database does not support actual transactions*

4. **CommitTransactionAsync_CommitsTransaction** ⚠️ *Skipped*  
   *InMemory database does not support actual transactions*

5. **RollbackTransactionAsync_RollsBackTransaction** ⚠️ *Skipped*  
   *InMemory database does not support actual transactions*

6. **Transaction_ExceptionDuringOperation_CanBeRolledBack**  
   Tests that operations in transactions can be rolled back on exception (uses non-transactional InMemory behavior)

7. **MultipleOperations_InTransaction_AreAtomic**  
   Validates that multiple operations within a transaction are handled correctly

8. **BeginTransactionAsync_WhenTransactionAlreadyActive_ReturnsExistingTransaction**  
   Tests edge case of starting transaction when one already exists

9. **CommitTransactionAsync_WhenNoTransaction_ReturnsGracefully**  
   Validates graceful handling when committing without an active transaction

10. **RollbackTransactionAsync_WhenNoTransaction_ReturnsGracefully**  
    Tests graceful handling when rolling back without an active transaction

11. **Dispose_DisposesContext**  
    Verifies proper disposal of DbContext resources

**Note**: Three tests are skipped because EF Core's InMemory database provider does not support actual transactions. The UnitOfWork implementation handles these edge cases gracefully in production with real databases.

#### UserSyncServiceTests.cs (22 tests)
Tests the `UserSyncService` for Active Directory synchronization:

1. **SyncResourcesAsync_WithNewResources_AddsToDatabase**  
   Verifies that new users from AD are added to the database

2. **SyncResourcesAsync_WithExistingResources_UpdatesProperties**  
   Tests that existing users are updated with latest AD information

3. **SyncResourcesAsync_WithInactiveUsers_MarksAsInactive**  
   Validates that users no longer in AD are marked as inactive

4. **SyncResourcesAsync_WithNoChanges_DoesNotModifyDatabase**  
   Confirms no unnecessary database operations when data is unchanged

5. **SyncResourcesAsync_WithMixedScenario_HandlesCorrectly**  
   Tests combination of adds, updates, and inactivation in single sync

6. **SyncResourcesAsync_PreservesCustomFields**  
   Ensures custom fields set manually are not overwritten by sync

7. **SyncResourcesAsync_WithEmptyAdList_MarksAllInactive**  
   Tests that all users are marked inactive when AD returns empty list

8. **SyncResourcesAsync_WithDuplicateIds_HandlesGracefully**  
   Validates proper handling of duplicate employee IDs from AD

9-22. Additional tests for edge cases, error handling, and data consistency

#### DtoSerializationTests.cs (8 tests)
Tests JSON serialization of DTOs:

1. **EventDto_SerializesToCamelCase**  
   Verifies EventDto serializes to camelCase JSON (id, start, end, text, color, resource)

2. **EventDto_DoesNotSerializeWithPascalCase**  
   Confirms PascalCase properties (Id, Start, ResourceId) are NOT in JSON

3. **CreateEventDto_DeserializesFromCamelCase**  
   Tests deserialization of camelCase JSON from JavaScript frontend

4. **UpdateEventDto_DeserializesFromCamelCase**  
   Validates update DTOs can be deserialized from camelCase JSON

5. **ResourceDto_SerializesToCamelCase**  
   Ensures ResourceDto serializes to camelCase (id, name)

6. **ResourceDto_DoesNotSerializeWithPascalCase**  
   Confirms PascalCase properties are NOT in serialized JSON

7. **EventDto_RoundTripSerialization**  
   Tests serialize → deserialize maintains data integrity

8. **EventDto_SerializedJsonMatchesExpectedFormat**  
   Validates actual JSON string format matches JavaScript expectations

**Key Testing Features**:
- Isolated unit tests using in-memory database
- Fresh DbContext per test for isolation
- Tests both success and failure paths
- Validates DTO/Entity mapping logic
- Ensures JSON compatibility with JavaScript frontend

---

### 3. pto.track.data.tests (Data Layer Tests)
**Total Tests**: 24  
**Technology**: xUnit 2.5.3, System.ComponentModel.DataAnnotations

Unit tests for entity validation logic using IValidatableObject implementations.

#### SchedulerEventValidationTests.cs (9 tests)
Tests the `SchedulerEvent` entity validation:

1. **Validate_EndAfterStart_NoValidationErrors**  
   Verifies valid events (End > Start) pass validation

2. **Validate_EndBeforeStart_ReturnsValidationError**  
   Tests that End date before Start date produces validation error

3. **Validate_EndEqualsStart_ReturnsValidationError**  
   Confirms End date equal to Start date produces validation error

4. **Validate_ResourceIdZero_ReturnsValidationError**  
   Tests that ResourceId of 0 fails validation (must be positive)

5. **Validate_ResourceIdNegative_ReturnsValidationError**  
   Validates that negative ResourceId fails validation

6. **Validate_TextExceedsMaxLength_ReturnsValidationError**  
   Tests that Text exceeding 200 characters fails validation

7. **Validate_ColorExceedsMaxLength_ReturnsValidationError**  
   Validates that Color exceeding 50 characters fails validation

8. **Validate_TextAtMaxLength_NoValidationErrors**  
   Confirms Text at exactly 200 characters passes validation

9. **Validate_ColorAtMaxLength_NoValidationErrors**  
   Tests that Color at exactly 50 characters passes validation

#### AbsenceRequestValidationTests.cs (11 tests)
Tests the `AbsenceRequest` entity validation:

1. **Validate_EndAfterStart_NoValidationErrors**  
   Verifies valid absence requests (End > Start) pass validation

2. **Validate_EndBeforeStart_ReturnsValidationError**  
   Tests that End date before Start date produces validation error

3. **Validate_EndEqualsStart_ReturnsValidationError**  
   Confirms End date equal to Start date produces validation error

4. **Validate_PastDateWithPendingStatus_ReturnsValidationError**  
   Tests that Pending absences cannot be created for past dates

5. **Validate_PastDateWithApprovedStatus_NoValidationError**  
   Validates that Approved absences can exist for past dates

6. **Validate_ReasonExceedsMaxLength_ReturnsValidationError**  
   Tests that Reason exceeding 500 characters fails validation

7. **Validate_ReasonAtMaxLength_NoValidationErrors**  
   Confirms Reason at exactly 500 characters passes validation

8. **Validate_ApprovalCommentsExceedsMaxLength_ReturnsValidationError**  
   Tests that ApprovalComments exceeding 1000 characters fails validation

9. **Validate_ApprovalCommentsAtMaxLength_NoValidationErrors**  
   Validates that ApprovalComments at exactly 1000 characters passes

10. **DefaultStatus_IsPending**  
    Tests that new absence requests default to Pending status

11. **RequestedDate_DefaultsToUtcNow**  
    Confirms RequestedDate is automatically set to current UTC time

#### SchedulerResourceValidationTests.cs (4 tests)
Tests the `SchedulerResource` entity validation:

1. **Validate_NameWithinMaxLength_NoValidationErrors**  
   Verifies valid resource names pass validation

2. **Validate_NameAtMaxLength_NoValidationErrors**  
   Tests that Name at exactly 100 characters passes validation

3. **Validate_NameExceedsMaxLength_ReturnsValidationError**  
   Validates that Name exceeding 100 characters fails validation

4. **Validate_NameRequired_ReturnsValidationError**  
   Tests that null or empty Name fails validation

**Key Testing Features**:
- Direct entity validation using System.ComponentModel.DataAnnotations
- Tests both attribute-based validation (StringLength, Required) and custom IValidatableObject validation
- Validates business rules (date ranges, status-based constraints)
- Ensures data integrity at the entity level
- Complex query tests

---

## Running Tests

### Command Line

```bash
# Run all tests in the solution
dotnet test

# Run tests with detailed output
dotnet test -v detailed

# Run specific test project
dotnet test pto.track.tests
dotnet test pto.track.services.tests
dotnet test pto.track.data.tests

# Run specific test class
dotnet test --filter "EventsControllerTests"
dotnet test --filter "EventServiceTests"

# Run specific test method
dotnet test --filter "GetSchedulerEvents_WithValidDateRange_ReturnsMatchingEvents"

# Run tests with code coverage
dotnet test /p:CollectCoverage=true

# Run tests and stop on first failure
dotnet test --blame-crash
```

### Visual Studio Code
See [TESTING_VSCODE.md](./TESTING_VSCODE.md) for detailed VS Code testing instructions.

### Visual Studio
- Open Test Explorer: `Test → Test Explorer`
- Run all tests: `Ctrl+R, A`
- Debug test: Right-click test → Debug
- Run tests on build: `Test → Test Settings → Run Tests After Build`

---

## Test Architecture

### Test Isolation
Each test uses a fresh in-memory database to ensure complete isolation:
```csharp
var options = new DbContextOptionsBuilder<SchedulerDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
```

### Test Patterns
- **Arrange-Act-Assert (AAA)**: All tests follow this pattern for clarity
- **Async/Await**: All async operations properly tested
- **Entity Tracking**: Proper handling of EF Core change tracking in tests
- **WebApplicationFactory**: Integration tests use in-memory test server
- **Descriptive Naming**: Test names describe what is being tested and expected outcome

### Test Data
- **Minimal Setup**: Each test only seeds the data it needs
- **Realistic Data**: Uses realistic dates, names, and scenarios
- **Edge Cases**: Tests include boundary conditions and error scenarios

---

## Coverage Summary

| Layer | Project | Tests | Status |
|-------|---------|-------|--------|
| **JavaScript - Core** | pto.track.tests.js | 58 | ✓ All Passing |
| **JavaScript - Filters** | pto.track.tests.js | 18 | ✓ All Passing |
| **JavaScript - Permissions** | pto.track.tests.js | 26 | ✓ All Passing |
| **JavaScript - Presentation** | pto.track.tests.js | 37 | ✓ All Passing |
| **JavaScript - Integration** | pto.track.tests.js | 16 | ✓ All Passing |
| **Controllers** | pto.track.tests | 11 | ✓ All Passing |
| **Integration** | pto.track.tests | 23 | ✓ All Passing |
| **Authorization** | pto.track.tests | 18 | ✓ All Passing |
| **Services** | pto.track.services.tests | 54 | ✓ All Passing |
| **Transaction Mgmt** | pto.track.services.tests | 11 | ✓ 8 Passing, ⚠️ 3 Skipped |
| **User Sync** | pto.track.services.tests | 22 | ✓ All Passing |
| **Serialization** | pto.track.services.tests | 8 | ✓ All Passing |
| **Entity Validation** | pto.track.data.tests | 24 | ✓ All Passing |
| **Total** | | **321** | **✓ 318 Passing, ⚠️ 3 Skipped** |

**Code Coverage**: 67.9% overall for C# code (see `coverage.xml` for detailed line-by-line coverage)

---

## Continuous Integration

These tests are designed to run in CI/CD pipelines:

```bash
# Example CI command
dotnet test --configuration Release --logger "trx;LogFileName=test-results.trx" --no-build --no-restore
```

---

## Future Testing Enhancements

Potential areas for additional test coverage:
1. **Performance Tests**: Load testing with multiple concurrent users
2. **Security Tests**: Authentication/authorization scenarios
3. **Data Layer Tests**: Complex query optimization tests
4. **UI Tests**: Selenium/Playwright for end-to-end UI testing
5. **API Contract Tests**: OpenAPI/Swagger validation tests
6. **Chaos Engineering**: Resilience testing with fault injection
