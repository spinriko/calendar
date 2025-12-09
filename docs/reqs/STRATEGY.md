# Strategy Pattern for Role-Based Permissions

## Problem Statement
The current application logic relies on complex, nested conditional checks to determine user permissions. Functions like `buildContextMenuItems`, `canCreateAbsenceForResource`, and `getVisibleFilters` manually evaluate combinations of:
1.  **User Roles** (Admin, Manager, Approver, Employee)
2.  **Resource Ownership** (Is the user acting on their own record?)
3.  **Data Context** (Is the absence Pending or Approved?)

This approach violates the **Open/Closed Principle**. Adding a new role (e.g., "Auditor") or changing a permission rule requires modifying multiple functions across the codebase, increasing the risk of regression bugs.

## Proposed Solution: Strategy Pattern
We will implement the **Strategy Pattern** to encapsulate permission logic into dedicated classes based on the user's role. The application will select the appropriate strategy at runtime (upon user load) and delegate all permission checks to it.

### 1. The Interface
We define a common interface that all role strategies must implement.

```typescript
interface IPermissionStrategy {
    /**
     * Determines if the current user can create an absence for the target resource.
     */
    canCreateFor(targetResourceId: number): boolean;

    /**
     * Determines if the current user can edit the specific absence.
     */
    canEdit(absence: Absence): boolean;

    /**
     * Determines if the current user can approve or reject the absence.
     */
    canApprove(absence: Absence): boolean;

    /**
     * Determines if the current user can delete the absence.
     */
    canDelete(absence: Absence): boolean;

    /**
     * Returns the list of status filters visible to this user role.
     */
    getVisibleFilters(): string[];

    /**
     * Returns the default selected filters for this user role.
     */
    getDefaultFilters(): string[];
    
    /**
     * Returns the CSS class for a scheduler cell based on permissions.
     */
    getCellCssClass(cellData: any): string | null;
}
```

### 2. Concrete Strategies

#### `AdminStrategy`
*   **canCreateFor**: Returns `true` (can create for anyone).
*   **canEdit**: Returns `true` (can edit any record).
*   **canApprove**: Returns `true`.
*   **getVisibleFilters**: Returns all statuses.

#### `ManagerStrategy`
*   **canCreateFor**: Returns `true` (managers can manage their team's time).
*   **canEdit**: Returns `false` (managers usually approve/reject, not edit employee requests directly, unless configured otherwise).
*   **canApprove**: Returns `true`.
*   **getVisibleFilters**: Returns `['Pending', 'Approved']`.

#### `EmployeeStrategy`
*   **canCreateFor**: Returns `true` only if `targetResourceId === currentUserId`.
*   **canEdit**: Returns `true` only if `absence.employeeId === currentUserId` AND `absence.status === 'Pending'`.
*   **canApprove**: Returns `false`.
*   **getVisibleFilters**: Returns `['Pending', 'Approved', 'Rejected', 'Cancelled']` (but data fetching filters to own records).

### 3. Implementation Plan
1.  Create a `strategies/` directory in `wwwroot/js`.
2.  Define the `IPermissionStrategy` interface (or abstract base class).
3.  Implement the concrete classes.
4.  Create a `PermissionStrategyFactory` to instantiate the correct strategy based on the `currentUser` object.
5.  Refactor `AbsenceSchedulerApp` to hold a reference to `this.permissionStrategy`.
6.  Replace calls to `calendar-functions.ts` with `this.permissionStrategy.method()`.

### Benefits
*   **Simplification**: `absences-scheduler.ts` no longer contains `if (isAdmin || isManager)` logic.
*   **Extensibility**: Adding a new role is as simple as creating a new class.
*   **Testability**: Each strategy can be unit tested in isolation.
