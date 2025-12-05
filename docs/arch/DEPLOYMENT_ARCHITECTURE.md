# Deployment Architecture

This diagram outlines the deployment environments, distinguishing between the development workstation setup, the testing environment, and the future production infrastructure.

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
```
