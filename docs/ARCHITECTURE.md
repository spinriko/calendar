# PTO Tracking Application - Architecture Overview

This document serves as the entry point for the architectural documentation of the PTO Tracking Application. The architecture is documented through a series of diagrams covering system design, component interactions, data modeling, and request flows.

## Architectural Diagrams

The following diagrams provide detailed views into different aspects of the system:

### [System Architecture](arch/SYSTEM_ARCHITECTURE.md)
**High-Level Overview:** Illustrates the interaction between the client (Browser/JS), presentation layer (Razor Pages/Controllers), application layer (Services/Auth), data layer (EF Core/SQL), and external systems (Active Directory).

### [Component Architecture](arch/COMPONENT_ARCHITECTURE.md)
**Internal Dependencies:** Maps the relationships and dependencies between the web project, service layer, data access layer, and the various test projects (Integration, Unit, JS Tests).

### [Data Model](arch/DATA_MODEL.md)
**Database Schema:** An Entity-Relationship Diagram (ERD) defining the core entities: Resources, Groups, Events, Absence Requests, and Approvals, along with their relationships.

### [Request Flow - Absence Request](arch/REQUEST_FLOW.md)
**Sequence Diagram:** Traces the lifecycle of an absence request creation, detailing the flow from the user's browser through the controller, service, repository, and database transaction.

### [Authentication & Authorization Flow](arch/AUTH_FLOW.md)
**Security Process:** Explains the authentication mechanism (Mock vs. Windows Auth) and the role-based authorization logic involving the UserClaimsProvider and Middleware.

### [Technology Stack](arch/TECH_STACK.md)
**Tech Overview:** A mind map categorizing the technologies used for the Backend, Frontend, Testing, Development, and Security.

### [Deployment Architecture](arch/DEPLOYMENT_ARCHITECTURE.md)
**Infrastructure:** Outlines the deployment environments, including the Development setup, Testing environment (In-Memory DB), and the planned Production infrastructure.
