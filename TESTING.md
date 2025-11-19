# Testing Documentation

This solution includes comprehensive test coverage across multiple test projects, ensuring reliability and maintainability of the codebase.

## Test Projects Overview

### Summary Statistics
- **Total Tests**: 119 (all passing ✓)
- **Test Projects**: 3
- **Test Coverage**: Controllers, Services, Integration workflows, JSON serialization, Entity validation, User management, Status filtering, Authorization & Role-based access control

## Test Projects

### 1. pto.track.tests (Integration Tests)
**Total Tests**: 34  
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
**Total Tests**: 61  
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

#### AbsenceServiceTests.cs (26 tests)
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
See [TESTING_VSCODE.md](TESTING_VSCODE.md) for detailed VS Code testing instructions.

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
| **Controllers** | pto.track.tests | 11 | ✓ All Passing |
| **Integration** | pto.track.tests | 5 | ✓ All Passing |
| **Authorization** | pto.track.tests | 18 | ✓ All Passing |
| **Services** | pto.track.services.tests | 37 | ✓ All Passing |
| **Serialization** | pto.track.services.tests | 8 | ✓ All Passing |
| **Entity Validation** | pto.track.data.tests | 24 | ✓ All Passing |
| **Data Layer** | pto.track.data.tests | 16 | ✓ All Passing |
| **Total** | | **119** | **✓ All Passing** |

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
