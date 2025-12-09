# System Architecture

This diagram illustrates the high-level architecture of the PTO Tracking application, showing the interaction between the client, presentation, application, and data layers, as well as external systems.

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
