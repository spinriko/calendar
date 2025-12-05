# PTO Track - Resource Scheduling Calendar

A comprehensive ASP.NET Core resource scheduling and PTO (Paid Time Off) tracking application built with clean architecture principles. Features two interactive calendar interfaces powered by DayPilot for managing events across multiple resources.

## Features

### 🗓️ Dual Calendar System

The application provides two specialized calendar views:

1. **Scheduling Calendar** (`/Scheduling`)
   - Resource-based event scheduling across multiple resources
   - Interactive drag-and-drop interface for event management
   - Color-coded events with customizable attributes
   - Real-time event creation, editing, and deletion
   - Date range filtering and navigation
   - Perfect for managing equipment, rooms, or personnel schedules

2. **Absence Calendar** (`/Absences`)
   - Employee PTO (Paid Time Off) and absence request management
   - Approval workflow with four states: Pending, Approved, Rejected, Cancelled
   - Request submission and approval interface
   - Manager approval actions with comments
   - Employee-specific absence tracking
   - Comprehensive absence request history

Both calendars share the same clean architecture and provide seamless navigation through an intuitive landing page.

## Architecture Overview

This solution implements a clean, layered architecture with clear separation of concerns:

```
pto.track              → Web layer (UI + API Controllers)
pto.track.services     → Business logic layer (Services + DTOs)
pto.track.data         → Data access layer (EF Core + Entities)
```

### Project Structure

#### **pto.track** (Main Web Application)
- **Technology**: ASP.NET Core 10.0 Razor Pages + Web API
- **Purpose**: Frontend UI and RESTful API endpoints
- **Key Components**:
  - `Controllers/EventsController.cs` - Event CRUD operations API
  - `Controllers/ResourcesController.cs` - Resource management API
  - `Controllers/AbsenceController.cs` - Absence request approval workflow API
  - `Pages/Index.cshtml` - Landing page with navigation to both calendars
  - `Pages/Scheduling.cshtml` - Interactive scheduling calendar UI using DayPilot
  - `Pages/Absences.cshtml` - Absence request calendar UI with approval actions
  - `Pages/AbsencesScheduler.cshtml` - Enhanced scheduler view for absence management
  - `Program.cs` - Application configuration and service registration
- **Features**:
  - Dual calendar system (Scheduling + Absences)
  - Interactive drag-and-drop calendar interface
  - Date range filtering for events
  - Resource-based event scheduling
  - Color-coded event display
  - Modal dialogs for event creation/editing
  - Absence request approval workflow (Pending/Approved/Rejected/Cancelled)
  - Responsive Bootstrap 5 UI with navigation

#### **pto.track.services** (Business Logic Layer)
- **Technology**: .NET 10.0 Class Library
- **Purpose**: Decouples business logic from web and data layers
- **Key Components**:
  - `IEventService.cs` / `EventService.cs` - Event business logic with Result pattern
  - `IResourceService.cs` / `ResourceService.cs` - Resource business logic
  - `IAbsenceService.cs` / `AbsenceService.cs` - Absence request approval workflow business logic
  - `IUnitOfWork.cs` / `UnitOfWork.cs` - Centralized transaction management
  - `DTOs/EventDto.cs` - Data transfer objects with AutoMapper profiles
  - `DTOs/ResourceDto.cs` - Resource data transfer objects
  - `DTOs/AbsenceRequestDto.cs` - Absence request DTOs (Create/Update/Approve/Reject variants)
  - `Exceptions/` - Custom exception hierarchy (NotFoundException, InvalidOperationException, ValidationException)
  - `ServiceCollectionExtensions.cs` - Dependency injection configuration
- **Features**:
  - Result pattern for consistent error handling
  - AutoMapper v13.0.1 for DTO mapping (eliminates manual mapping boilerplate)
  - Unit of Work pattern for transaction management
  - DTO-based API contracts (camelCase JSON serialization for JavaScript compatibility)
  - Validation logic (IValidatableObject implementation)
  - CancellationToken support in all async operations
  - Database migration management
  - Clean separation from data entities
  - Approval workflow state management

#### **pto.track.data** (Data Access Layer)
- **Technology**: Entity Framework Core 10.0
- **Purpose**: Database access and entity management
- **Key Components**:
  - `PtoTrackDbContext.cs` - EF Core database context
  - `Entities/SchedulerEvent.cs` - Event entity model
  - `Entities/SchedulerResource.cs` - Resource entity model
  - `Entities/AbsenceRequest.cs` - PTO/absence request entity with approval workflow
  - `Migrations/` - Database schema migrations
- **Database**: SQL Server (configurable via connection string)
- **Features**:
  - Code-first migrations
  - Entity relationships and constraints
  - Data seeding support
  - IValidatableObject validation on entities

### Test Projects

#### **pto.track.tests** (Integration Tests)
- **Tests**: 50 integration tests + 8 code quality metrics analyzers
- **Coverage**:
  - Controller endpoint testing (including absences API)
  - End-to-end CRUD workflows
  - Resource and event retrieval
  - Absence request approval workflows
  - **Code Quality Metrics**: Cyclomatic complexity, maintainability index, LOC analysis, parameter counts, nesting depth, class coupling, inheritance depth
- **Dependencies**: xUnit, Microsoft.AspNetCore.Mvc.Testing, In-Memory Database, Microsoft.CodeAnalysis (Roslyn)

#### **pto.track.services.tests** (Service Layer Tests)
- **Tests**: 83 unit tests
- **Coverage**:
  - EventService business logic (14 tests)
  - ResourceService operations (7 tests)
  - AbsenceService workflow logic (21 tests)
  - UnitOfWork transaction management (11 tests - 8 passing, 3 skipped due to InMemory limitations)
  - UserSyncService operations (22 tests)
  - DTO JSON serialization (8 tests)
- **Dependencies**: xUnit, Entity Framework In-Memory Database

#### **pto.track.data.tests** (Data Layer Tests)
- **Tests**: 24 entity validation tests
- **Coverage**:
  - SchedulerEvent validation (9 tests)
  - AbsenceRequest validation (11 tests)
  - SchedulerResource validation (4 tests)
- **Dependencies**: xUnit, System.ComponentModel.DataAnnotations

#### **pto.track.tests.js** (JavaScript Tests)
- **Tests**: 41 pure JavaScript tests using Jest
- **Coverage**:
  - Status color mapping (8 tests)
  - Checkbox filters (4 tests)
  - URL builder (6 tests)
  - Role detection (18 tests)
  - Impersonation (5 tests)
- **Technology**: Jest, pure JavaScript (no build tools)
- **Runs**: Browser-based or headless (CI/CD ready)

**Total Test Coverage**: 165 tests (162 passing, 3 skipped)
- **C# Tests**: 124 tests
  - Integration Tests: 50 tests (pto.track.tests)
  - Code Quality Metrics: 8 analyzers (pto.track.tests)
  - Service Layer Tests: 83 tests (pto.track.services.tests) including UnitOfWork tests
  - Data Layer Tests: 24 tests (pto.track.data.tests)
- **JavaScript Tests**: 41 tests (pto.track.tests.js)
- **Code Coverage**: 67.9% overall (coverage.xml available)

**Code Quality Metrics** (Solution-wide):
- Cyclomatic complexity analysis
- Maintainability index (0-100 scale)
- Lines of code per method/class
- Method parameter counts
- Nesting depth analysis
- Class coupling analysis
- Inheritance depth analysis
- Comprehensive summary dashboard

See [TESTING.md](docs/TESTING.md) for detailed C# test documentation and code metrics, and [pto.track.tests.js/README.md](pto.track.tests.js/README.md) for JavaScript test documentation.

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- SQL Server (or SQL Server Express/LocalDB for development)
- Visual Studio Code (recommended) or Visual Studio 2022+

### Configuration

1. **Set up the database connection**:
   ```bash
   cd pto.track
   dotnet user-secrets set "ConnectionStrings:PtoTrackDbContext" "Server=localhost;Database=PtoTrackDb;Trusted_Connection=True;TrustServerCertificate=True"
   ```

2. **Apply database migrations**:
   ```bash
   dotnet ef database update --project pto.track.data --startup-project pto.track
   ```

3. **Run the application**:
   ```bash
   dotnet run --project pto.track
   ```

4. **Access the application**:
   - Navigate to `https://localhost:5001` (or the port specified in console output)

### Building and Testing

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test pto.track.services.tests
dotnet test pto.track.tests

# Build for release
dotnet publish pto.track/pto.track.csproj -c Release -o ./publish
```

## Key Features

### Calendar Functionality
- **Resource-based Scheduling**: Organize events by resources (employees, rooms, equipment)
- **Drag & Drop**: Move events between resources or adjust time ranges
- **Date Range Navigation**: Previous/Today/Next day navigation
- **Multi-month Date Picker**: 3-month view for quick date selection
- **Color Coding**: Visual categorization with customizable colors
- **Event CRUD**: Create, read, update, and delete events via intuitive UI

### Technical Highlights
- **Clean Architecture**: Clear separation between presentation, business logic, and data layers
- **API-First Design**: RESTful API endpoints that support both UI and external integrations
- **Result Pattern**: Consistent error handling with `Result<T>` for all service operations
- **Unit of Work Pattern**: Centralized transaction management across service operations
- **DTO Pattern**: AutoMapper-powered mapping with decoupled data contracts and validation
- **Dependency Injection**: Constructor-based DI throughout the application
- **Health Checks**: Built-in endpoints for monitoring application health (`/health`, `/health/ready`, `/health/live`)
- **Custom Exception Handling**: Comprehensive exception hierarchy with global exception middleware
- **JSON Compatibility**: Proper camelCase serialization for JavaScript frontend
- **Async/Await**: Non-blocking operations with CancellationToken support for better performance and cancellation
- **Comprehensive Testing**: 154 automated tests covering critical functionality (67.9% code coverage)

## API Endpoints

### Health Check API (`/health`)
- `GET /health` - Overall application health status
- `GET /health/ready` - Readiness probe (checks if app is ready to serve requests)
- `GET /health/live` - Liveness probe (checks if app is running)

### Events API (`/api/events`)
- `GET /api/events?start={date}&end={date}` - Get events in date range
- `GET /api/events/{id}` - Get specific event
- `POST /api/events` - Create new event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

### Resources API (`/api/resources`)
- `GET /api/resources` - Get all resources

### Absence API (`/api/absence`)
- `GET /api/absence?start={date}&end={date}` - Get absence requests in date range
- `GET /api/absence/employee/{employeeId}` - Get absence requests for specific employee
- `GET /api/absence/pending` - Get all pending absence requests
- `GET /api/absence/{id}` - Get specific absence request
- `POST /api/absence` - Create new absence request
- `PUT /api/absence/{id}` - Update absence request
- `POST /api/absence/{id}/approve` - Approve absence request
- `POST /api/absence/{id}/reject` - Reject absence request
- `POST /api/absence/{id}/cancel` - Cancel absence request
- `DELETE /api/absence/{id}` - Delete absence request

## Technology Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: Entity Framework Core 10.0 + SQL Server
- **Object Mapping**: AutoMapper 13.0.1 (MIT License)
- **Testing**: xUnit 2.9.3+ with In-Memory Database
- **Frontend**: DayPilot Lite for JavaScript (Apache License 2.0)
- **UI**: Razor Pages with interactive JavaScript components
- **Monitoring**: ASP.NET Core Health Checks

## Development

### VS Code Setup
- Install recommended extensions (C# Dev Kit, .NET Test Explorer)
- Use `F5` to start debugging
- Test Explorer available via beaker icon 🧪
- See [TESTING_VSCODE.md](TESTING_VSCODE.md) for detailed VS Code testing instructions

### Project Dependencies
```
pto.track → pto.track.services → pto.track.data
pto.track.tests → pto.track → pto.track.services → pto.track.data
pto.track.services.tests → pto.track.services → pto.track.data
```

## License
- Application code: Apache License 2.0
- DayPilot Lite for JavaScript: Apache License 2.0
- Third-party libraries: See [LicensesThirdParty/nuget.txt](LicensesThirdParty/nuget.txt)

## Attribution
This project was originally based on the [DayPilot Resource Scheduling Calendar](https://code.daypilot.org/20604/asp-net-core-resource-calendar-open-source) tutorial and has been significantly enhanced with clean architecture, comprehensive testing, and service layer implementation.
