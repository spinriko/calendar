# Request Flow - Absence Request

This sequence diagram illustrates the end-to-end flow of creating an absence request, from the user's browser action through the controller, service, and data layers, and back.

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
