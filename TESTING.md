# Testing Documentation

This solution includes comprehensive test coverage across multiple test projects, ensuring reliability and maintainability of the codebase.

## Test Projects Overview

### Summary Statistics
- **Total Tests**: 45 (all passing ✓)
- **Test Projects**: 3
- **Test Coverage**: Controllers, Services, Integration workflows, JSON serialization

## Test Projects

### 1. pto.track.tests (Integration Tests)
**Total Tests**: 16  
**Technology**: xUnit 2.9.3, Microsoft.AspNetCore.Mvc.Testing, EF Core In-Memory Database

Integration tests that verify the entire application stack works together correctly, from HTTP requests through controllers and services to the database layer.

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

**Key Testing Features**:
- Uses `WebApplicationFactory<Program>` for in-memory test server
- Fresh database per test via in-memory provider
- Tests actual HTTP responses and status codes
- Validates both happy path and error scenarios

---

### 2. pto.track.services.tests (Service Layer Unit Tests)
**Total Tests**: 29  
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
**Total Tests**: 1 (placeholder)  
**Status**: Placeholder project for future data layer specific testing

Currently contains a basic placeholder test. Future enhancements could include:
- Entity validation tests
- Database constraint tests
- Migration tests
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
| **Services** | pto.track.services.tests | 21 | ✓ All Passing |
| **Serialization** | pto.track.services.tests | 8 | ✓ All Passing |
| **Data Layer** | pto.track.data.tests | 1 | ✓ Placeholder |
| **Total** | | **45** | **✓ All Passing** |

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
