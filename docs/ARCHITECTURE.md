# PTO Tracking Application - Architecture

## System Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        Browser[Web Browser]
        JS[JavaScript/DayPilot Scheduler]
    end

    subgraph "Presentation Layer"
        Pages[Razor Pages]
        Controllers[API Controllers]
    end

    subgraph "Application Layer"
        Services[Business Services]
        Auth[Authentication/Authorization]
        Middleware[Exception Middleware]
    end

    subgraph "Data Layer"
        UnitOfWork[Unit of Work]
        Repositories[Generic Repository]
        DbContext[EF Core DbContext]
    end

    subgraph "External Systems"
        AD[Active Directory]
        SQL[(SQL Server Database)]
    end

    Browser --> Pages
    Browser --> Controllers
    JS --> Controllers
    
    Pages --> Services
    Controllers --> Services
    Controllers --> Auth
    Controllers --> Middleware
    
    Services --> UnitOfWork
    Auth --> AD
    
    UnitOfWork --> Repositories
    Repositories --> DbContext
    DbContext --> SQL
```

## Component Architecture

```mermaid
graph LR
    subgraph "pto.track (Web)"
        PC[Pages & Controllers]
        MW[Middleware]
        WWWROOT[wwwroot/Static Files]
    end

    subgraph "pto.track.services"
        AS[AbsenceService]
        ES[EventService]
        RS[ResourceService]
        GS[GroupService]
        US[UserSyncService]
        UOW[UnitOfWork]
        SPEC[Specifications]
        MAP[AutoMapper Profiles]
    end

    subgraph "pto.track.data"
        CTX[PtoTrackDbContext]
        REPO[GenericRepository]
        MOD[Data Models]
        MIG[EF Migrations]
    end

    subgraph "pto.track.tests"
        INT[Integration Tests]
        AUTH[Authorization Tests]
        METRICS[Complexity Analyzer]
    end

    subgraph "pto.track.services.tests"
        UNIT[Unit Tests]
    end

    subgraph "pto.track.tests.js"
        JSTEST[JavaScript Tests]
    end

    PC --> AS
    PC --> ES
    PC --> RS
    PC --> GS
    PC --> US
    
    AS --> UOW
    ES --> UOW
    RS --> UOW
    GS --> UOW
    US --> UOW
    
    UOW --> REPO
    REPO --> CTX
    CTX --> MOD
    
    INT --> PC
    UNIT --> AS
    UNIT --> ES
    UNIT --> RS
    UNIT --> GS
    JSTEST --> WWWROOT
```

## Data Model

```mermaid
erDiagram
    RESOURCES ||--o{ EVENTS : has
    RESOURCES }o--|| GROUPS : belongs_to
    RESOURCES ||--o{ ABSENCE_REQUESTS : submits
    RESOURCES ||--o{ APPROVALS : approves
    
    RESOURCES {
        int Id PK
        string Name
        string EmployeeNumber
        string Email
        string Type
        bool IsActive
        bool IsManager
        string Department
        int GroupId FK
    }
    
    GROUPS {
        int GroupId PK
        string Name
    }
    
    EVENTS {
        guid Id PK
        datetime Start
        datetime End
        string Text
        string Color
        int ResourceId FK
    }
    
    ABSENCE_REQUESTS {
        guid Id PK
        int EmployeeId FK
        datetime StartDate
        datetime EndDate
        string AbsenceType
        string Status
        string Comments
        datetime RequestedDate
    }
    
    APPROVALS {
        guid Id PK
        guid AbsenceRequestId FK
        int ApproverId FK
        datetime ApprovedDate
        string Comments
    }
```

## Request Flow - Absence Request

```mermaid
sequenceDiagram
    participant U as User/Browser
    participant C as AbsencesController
    participant AS as AbsenceService
    participant UOW as UnitOfWork
    participant R as Repository
    participant DB as Database

    U->>C: POST /api/absences
    C->>C: Validate ModelState
    C->>AS: CreateAbsenceRequestAsync(dto)
    AS->>AS: Map DTO to Entity
    AS->>UOW: GetRepository<AbsenceRequest>()
    UOW->>R: Return Repository
    AS->>R: AddAsync(entity)
    AS->>UOW: SaveChangesAsync()
    UOW->>DB: Commit Transaction
    DB-->>UOW: Success
    UOW-->>AS: Changes Saved
    AS->>AS: Map Entity to DTO
    AS-->>C: Return AbsenceRequestDto
    C-->>U: 201 Created + Location Header
```

## Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant U as User
    participant MW as MockAuthMiddleware
    participant AD as Active Directory
    participant UCP as UserClaimsProvider
    participant C as Controller
    participant A as AuthZ Policy

    U->>MW: HTTP Request
    
    alt Development Mode
        MW->>MW: Check X-Test-User header
        MW->>MW: Create mock claims
    else Production Mode
        MW->>AD: Authenticate via Windows Auth
        AD-->>MW: User Principal
    end
    
    MW->>UCP: Load User Claims
    UCP->>UCP: Extract roles (Admin/Manager/Approver/Employee)
    UCP-->>MW: Claims Identity
    
    MW->>C: Forward Request + ClaimsPrincipal
    C->>A: Check [Authorize] attribute
    
    alt Has Required Role
        A-->>C: Authorized
        C->>C: Execute Action
        C-->>U: Response
    else Missing Role
        A-->>C: Forbidden
        C-->>U: 403 Forbidden
    end
```

## Technology Stack

```mermaid
mindmap
    root((PTO Tracker))
        Backend
            ASP.NET Core 9.0
            Entity Framework Core
            SQL Server
            AutoMapper
            Serilog
        Frontend
            Razor Pages
            DayPilot Scheduler
            JavaScript ES Modules
            Bootstrap 5
        Testing
            xUnit
            Jest
            Moq
            WebApplicationFactory
            ESLint
            Code Metrics Analyzer
        Development
            Visual Studio Code
            Git/GitHub
            PowerShell
            Docker (optional)
        Security
            Windows Authentication
            Active Directory Integration
            Role-Based Authorization
            Claims-Based Identity
```

## Deployment Architecture

```mermaid
graph TB
    subgraph "Development"
        DEV[Developer Workstation]
        DEVDB[(Local SQL Server)]
    end

    subgraph "Testing"
        TEST[In-Memory Database]
        TESTRUN[Test Runner]
    end

    subgraph "Production (Future)"
        LB[Load Balancer]
        WEB1[Web Server 1]
        WEB2[Web Server 2]
        SQLPROD[(SQL Server Cluster)]
        ADPROD[Active Directory]
    end

    DEV --> DEVDB
    DEV --> TEST
    TESTRUN --> TEST
    
    LB --> WEB1
    LB --> WEB2
    WEB1 --> SQLPROD
    WEB2 --> SQLPROD
    WEB1 --> ADPROD
    WEB2 --> ADPROD
```

## Key Design Patterns

### Repository Pattern
- `GenericRepository<T>` provides CRUD operations
- Abstracts data access from business logic
- Enables unit testing with in-memory data

### Unit of Work Pattern
- `UnitOfWork` manages transactions across repositories
- Ensures consistency in multi-step operations
- Coordinates SaveChanges across entities

### Specification Pattern
- `ISpecification<T>` encapsulates query logic
- Reusable, testable query conditions
- Examples: `AbsencesByEmployeeSpecification`, `PendingAbsencesSpecification`

### Service Layer Pattern
- Business logic isolated from controllers
- `IAbsenceService`, `IEventService`, `IResourceService`, `IGroupService`
- Returns `Result<T>` for operation outcomes

### DTO Pattern
- Data Transfer Objects separate API contracts from domain models
- AutoMapper handles entity ↔ DTO conversion
- Examples: `CreateAbsenceRequestDto`, `AbsenceRequestDto`

### Middleware Pattern
- `GlobalExceptionHandler` for centralized error handling
- `MockAuthenticationMiddleware` for development auth simulation
- Custom exception types for domain-specific errors

## Security Model

```mermaid
graph TD
    AUTH[Authentication] --> ROLES[Role Assignment]
    ROLES --> ADMIN[Admin Role]
    ROLES --> MANAGER[Manager Role]
    ROLES --> APPROVER[Approver Role]
    ROLES --> EMPLOYEE[Employee Role]
    
    ADMIN --> |Full Access| ALL[All Operations]
    MANAGER --> |Approve/View| MGMT[Team Management]
    APPROVER --> |Approve Only| APPR[Absence Approval]
    EMPLOYEE --> |Own Data| SELF[Personal Absences]
    
    SELF --> VIEW[View Own Requests]
    SELF --> CREATE[Create Requests]
    SELF --> UPDATE[Update Pending Requests]
    SELF --> CANCEL[Cancel Own Requests]
```

## API Endpoints

### Absences
- `GET /api/absences?start&end&employeeId&status[]` - Query absences
- `GET /api/absences/pending` - Get pending requests
- `GET /api/absences/{id}` - Get specific absence
- `POST /api/absences` - Create absence request
- `PUT /api/absences/{id}` - Update absence request
- `POST /api/absences/{id}/approve` - Approve request (Manager/Approver)
- `POST /api/absences/{id}/reject` - Reject request (Manager/Approver)
- `POST /api/absences/{id}/cancel` - Cancel request (Employee)
- `DELETE /api/absences/{id}` - Delete absence

### Events (Scheduler)
- `GET /api/events?start&end` - Get events in range
- `GET /api/events/{id}` - Get specific event
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

### Resources
- `GET /api/resources` - Get all resources
- `GET /api/resources/{id}` - Get specific resource
- `GET /api/resources/group/{groupId}` - Get resources by group

### Groups
- `GET /api/groups` - Get all groups (Admin only)
- `GET /api/groups/{id}` - Get specific group (Admin only)
- `GET /api/groups/{id}/resources` - Get group resources (Admin only)
- `POST /api/groups` - Create group (Admin only)
- `PUT /api/groups/{id}` - Update group (Admin only)
- `DELETE /api/groups/{id}` - Delete group (Admin only)

### Current User
- `GET /api/currentuser` - Get authenticated user info
- `GET /api/currentuser/impersonation-enabled` - Check impersonation status

## Testing Strategy

### Unit Tests (pto.track.services.tests)
- Service layer business logic
- Repository operations
- Mapping configurations
- Specification logic
- 117 tests

### Integration Tests (pto.track.tests)
- End-to-end API workflows
- Authentication/Authorization
- Database interactions
- Exception handling
- 64 tests

### JavaScript Tests (pto.track.tests.js)
- Calendar functions
- Context menu behavior
- Filter logic
- Impersonation UI
- Role detection
- 182 tests

### Code Quality
- Cyclomatic complexity analysis (C# and JavaScript)
- ESLint for JavaScript code quality
- Threshold: Max complexity 10

## Build & Deployment

### Build Process
```bash
dotnet build pto.track.sln
```

### Test Execution
```powershell
.\run-all-tests.ps1
```

### Database Migrations
```bash
dotnet ef migrations add MigrationName --project pto.track.data --startup-project pto.track
dotnet ef database update --project pto.track.data --startup-project pto.track
```

### Run Application
```bash
dotnet run --project pto.track
# or
dotnet watch run --project pto.track
```

## Future Enhancements (See FUTURE.md)
- Email notifications
- Calendar integration (Outlook)
- Mobile responsive design
- Reporting and analytics
- Multi-language support
- Azure deployment
- CI/CD pipeline
