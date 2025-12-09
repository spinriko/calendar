# Authentication & Authorization Flow

This sequence diagram details the security process, including how the Mock Authentication Middleware handles development vs. production modes, and how role-based authorization is enforced.

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
