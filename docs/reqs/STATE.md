# State Pattern for Absence Lifecycle

## Problem Statement
The behavior of an Absence Request changes significantly based on its **Status** (`Pending`, `Approved`, `Rejected`, `Cancelled`). Currently, the UI logic repeatedly checks the status string to determine available actions:

*   "If status is Pending, show Edit button."
*   "If status is Pending, show Approve button."
*   "If status is Approved, hide Edit button."

This logic is scattered across `buildContextMenuItems`, `handleMenuAction`, and `saveAbsence`. As the lifecycle grows more complex (e.g., adding "Requested for Cancellation"), these hardcoded string checks become fragile and difficult to maintain.

## Proposed Solution: State Pattern
We will implement the **State Pattern** (or a simplified State Machine Policy) to encapsulate the behavior specific to each status. The `Absence` object (or a wrapper around it) will delegate action validation to its current State object.

### 1. The Concept
Instead of the UI asking "Is this status 'Pending'?", the UI asks the Absence object: "Can you be edited?" or "What actions are available?".

### 2. The Interface / Abstract Base
We define a structure that represents the capabilities of an absence in a specific state.

```typescript
abstract class AbsenceState {
    constructor(protected absence: Absence) {}

    abstract get name(): string;
    abstract get color(): string;

    canEdit(user: User): boolean { return false; }
    canApprove(user: User): boolean { return false; }
    canCancel(user: User): boolean { return false; }
    canDelete(user: User): boolean { return false; }

    getAvailableActions(user: User): MenuAction[] {
        const actions = [];
        if (this.canEdit(user)) actions.push('Edit');
        if (this.canApprove(user)) actions.push('Approve');
        // ...
        return actions;
    }
}
```

### 3. Concrete States

#### `PendingState`
*   **canEdit**: Returns `true` if user is Owner.
*   **canApprove**: Returns `true` if user is Approver.
*   **canDelete**: Returns `true` if user is Owner.
*   **Color**: Orange.

#### `ApprovedState`
*   **canEdit**: Returns `false` (Approved requests are locked).
*   **canApprove**: Returns `false` (Already approved).
*   **canCancel**: Returns `true` (Owner can request cancellation).
*   **Color**: Green.

#### `RejectedState`
*   **canEdit**: Returns `false`.
*   **canDelete**: Returns `true` (Cleanup).
*   **Color**: Red.

#### `CancelledState`
*   **canEdit**: Returns `false`.
*   **canDelete**: Returns `true`.
*   **Color**: Gray.

### 4. Implementation Plan
1.  Create a `states/` directory in `wwwroot/js`.
2.  Define the `AbsenceState` base class.
3.  Implement the concrete state classes.
4.  Create a `StateFactory` that takes a raw API DTO and returns the correct `AbsenceState` instance.
5.  Refactor `buildContextMenuItems` to simply call `absenceState.getAvailableActions(currentUser)`.

### Benefits
*   **Centralized Rules**: Business rules for transitions (e.g., "Cannot edit an Approved request") are enforced in the State classes, not the UI.
*   **Cleaner UI Code**: The context menu builder becomes a simple loop over `getAvailableActions()`.
*   **Visual Consistency**: Colors and labels are defined alongside the behavior.

### Alternative: Policy Object (Simplified State)
If full classes feel like overkill for the frontend, a **Policy Object** map can achieve similar results:

```typescript
const AbsencePolicies = {
    Pending: {
        color: "#ffa500",
        actions: ["Edit", "Delete", "Approve", "Reject"]
    },
    Approved: {
        color: "#6aa84f",
        actions: ["Cancel"] // No edit
    },
    // ...
};
```
This is lighter weight but less flexible if rules depend on complex logic (e.g., "Can edit only if start date is in the future").
