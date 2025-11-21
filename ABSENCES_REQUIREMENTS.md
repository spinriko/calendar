# Absence Management System - Requirements Document

## Overview
The Absence Management system provides a comprehensive interface for employees to request time off and for managers to review and approve/reject absence requests. The system supports different user roles with distinct capabilities and views.

## User Roles

### 1. Employee (Standard User)
Employees can manage their own absence requests.

#### Capabilities
- **View Own Requests**: See all their absence requests (Pending, Approved, Rejected, Cancelled)
- **Submit New Request**: Create new absence requests with start date, end date, and reason
- **Edit Pending Requests**: Modify their own requests while in Pending status
- **Cancel Requests**: Cancel their own absence requests (any status except Cancelled)
- **View Calendar**: See all approved absences for planning purposes

#### Restrictions
- Cannot approve or reject requests
- Cannot view other employees' pending requests
- Cannot modify requests after approval/rejection
- Cannot delete requests (only cancel)

#### Default View
- Calendar showing all **approved** absence requests
- Quick access to view their own pending requests via toggle button

### 2. Manager/Approver
Managers have elevated permissions to manage team absences.

#### Capabilities (in addition to Employee capabilities)
- **View All Pending Requests**: Access all pending absence requests across the organization
- **Approve Requests**: Approve pending absence requests with optional comments
- **Reject Requests**: Reject pending absence requests with mandatory reason
- **View All Absences**: See all absence requests regardless of status or employee
- **Override Edits**: Administrative access to modify any request
- **Delete Requests**: Permanently remove absence requests (admin function)

#### Default View
- Calendar showing all **approved** absence requests
- Prominent access to pending requests requiring review
- Badge showing count of pending requests awaiting approval

### 3. Administrator (System Admin)
Full system access for maintenance and data management.

#### Capabilities (in addition to Manager capabilities)
- **Delete Any Request**: Remove any absence request from the system
- **Bulk Operations**: Process multiple requests simultaneously
- **View Audit Trail**: Access request history and approval workflow
- **System Configuration**: Manage absence policies, employee records, and approval chains

## Page Behavior by User Role

### Initial Page Load

#### Employee View
```
- Calendar displays: All approved absence requests (organization-wide)
- Employee resources listed in left sidebar
- "View Pending" button displayed (unselected)
- Status: Showing all approved absences
```

#### Manager View
```
- Calendar displays: All approved absence requests (organization-wide)
- Employee resources listed in left sidebar
- "View Pending" button displayed with badge showing pending count
- "Approve Requests" action available in context menu
- Status: Showing all approved absences with X pending requests
```

### "View Pending" Button Behavior

#### Employee Click
```
Toggle ON:
- Calendar shows: ONLY the logged-in employee's pending requests
- Button highlighted/selected state
- Other employees' absences hidden
- Status message: "Showing your pending requests"

Toggle OFF:
- Calendar shows: All approved absence requests (organization-wide)
- Button normal state
- Status message: "Showing all approved absences"
```

#### Manager Click
```
Toggle ON:
- Calendar shows: ALL pending requests from all employees
- Button highlighted/selected state
- Requests grouped by employee
- Approve/Reject actions enabled in context menu
- Status message: "Showing all pending requests (X total)"

Toggle OFF:
- Calendar shows: All approved absence requests (organization-wide)
- Button normal state
- Status message: "Showing all approved absences"
```

## API Endpoints Required

### For Employees
```
GET /api/absences?status=Approved&start={date}&end={date}
GET /api/absences?employeeId={currentUserId}&status=Pending
POST /api/absences
PUT /api/absences/{id}
POST /api/absences/{id}/cancel?employeeId={currentUserId}
```

### For Managers
```
GET /api/absences?status=Approved&start={date}&end={date}
GET /api/absences?status=Pending
POST /api/absences/{id}/approve
POST /api/absences/{id}/reject
DELETE /api/absences/{id}
```

## Workflow Examples

### Employee Submits Request
1. Employee clicks on calendar date range
2. Modal opens with form (Start Date, End Date, Reason)
3. Employee fills form and clicks "Submit"
4. System creates request with Status=Pending
5. Request appears in employee's pending view
6. Manager sees new request in their pending queue
7. Notification sent to manager (future enhancement)

### Manager Reviews Request
1. Manager clicks "View Pending" button
2. Calendar shows all pending requests
3. Manager clicks on a request
4. Context menu shows "Approve" and "Reject" options
5. Manager clicks "Approve"
6. Modal prompts for optional comments
7. System updates request Status=Approved, sets ApproverId, ApprovedDate
8. Request appears in organization calendar
9. Employee receives notification (future enhancement)

### Employee Cancels Request
1. Employee views their pending requests or approved absences
2. Employee clicks on their request
3. Context menu shows "Cancel" option (if not already Cancelled)
4. Confirmation dialog appears
5. System updates request Status=Cancelled
6. Request remains visible with Cancelled status
7. Manager receives notification (future enhancement)

## Status Transitions

```
Pending → Approved (Manager action)
Pending → Rejected (Manager action)
Pending → Cancelled (Employee action)
Approved → Cancelled (Employee action)
Rejected → [No transitions allowed]
Cancelled → [No transitions allowed]
```

## UI States

### Calendar Event Colors by Status
- **Approved**: Green (#4CAF50)
- **Pending**: Yellow/Amber (#FFC107)
- **Rejected**: Red (#F44336)
- **Cancelled**: Gray (#9E9E9E)

### Button States
- **View Pending (Unselected)**: Default button styling
- **View Pending (Selected)**: Primary color, highlighted background
- **View Pending (With Badge)**: Shows count of pending requests (Manager only)

## Data Filters

### Status Filter
- Approved: Show only approved requests
- Pending: Show only pending requests
- Rejected: Show only rejected requests
- Cancelled: Show only cancelled requests
- All: Show requests in any status

### Employee Filter
- Current User: Show only logged-in user's requests
- All Employees: Show requests from all employees
- Specific Employee: Show requests from selected employee (Manager view)

### Date Range Filter
- Current Month: Default view
- Custom Range: User-specified start and end dates
- Next 30 Days: Forward-looking view
- Past 90 Days: Historical view

## Future Enhancements

### Phase 2
- Email notifications on request submission, approval, rejection
- Approval chain (multiple approvers)
- Absence balance tracking (remaining PTO days)
- Conflict detection (overlapping requests for same employee)
- Team capacity view (manager sees team coverage)

### Phase 3
- Mobile app integration
- Calendar export (iCal, Google Calendar)
- Recurring absence requests (weekly, bi-weekly patterns)
- Absence types (Vacation, Sick Leave, Personal Day, etc.)
- Integration with HR systems

## User and Approver Management

### Resource Table Schema
The `Resources` table represents both employees and managers in the system. It has been extended to support user roles and approval capabilities:

#### Current Schema
```
Id (int, Primary Key, Identity)
Name (nvarchar(100), Required)
```

#### Proposed Schema Extensions
```
Id (int, Primary Key, Identity)
Name (nvarchar(100), Required)
Email (nvarchar(255), Unique, Indexed)
EmployeeNumber (nvarchar(50), Unique, Indexed, Nullable)
Role (nvarchar(50)) - Values: "Employee", "Manager", "Administrator"
IsApprover (bit, Default: false)
IsActive (bit, Default: true)
Department (nvarchar(100), Nullable)
ManagerId (int, Foreign Key to Resources.Id, Nullable)
ActiveDirectoryId (nvarchar(255), Unique, Indexed, Nullable)
LastSyncDate (datetime2, Nullable)
CreatedDate (datetime2, Default: GETUTCDATE())
ModifiedDate (datetime2, Default: GETUTCDATE())
```

### Active Directory Synchronization

#### Sync Strategy
- **Periodic Sync**: Scheduled job (daily/hourly) pulls user data from Active Directory
- **Sync Scope**: Active employees, role assignments, manager relationships, department info
- **Sync Operations**:
  - **Create**: Add new employees from AD to Resources table
  - **Update**: Sync name changes, role changes, manager reassignments, department moves
  - **Deactivate**: Set `IsActive = false` for terminated employees (do NOT delete to preserve historical data)
  - **Reactivate**: Set `IsActive = true` if employee returns

#### AD Attributes Mapping
```
AD Attribute → Resources Column
-----------------------------------
objectGuid → ActiveDirectoryId
displayName → Name
mail → Email
employeeNumber → EmployeeNumber
department → Department
title → (used to determine Role)
manager → ManagerId (lookup by AD objectGuid)
accountEnabled → IsActive
```

#### Role Assignment Logic
```
Administrator: Members of AD group "PTOTrack-Admins"
Manager: Members of AD group "PTOTrack-Managers" OR has direct reports in AD
Employee: All other active users
```

#### Approver Assignment
- `IsApprover = true` for users with Role = "Manager" or "Administrator"
- Can be manually overridden in database for special cases (e.g., team leads, project managers)

### User Identification in Application

#### Current State (Development)
- Hardcoded user IDs in JavaScript (e.g., `approverId: 1`)
- No authentication layer

#### Production Requirements
- Authenticate users via Azure AD / Entra ID
- Extract `ActiveDirectoryId` from authentication claims
- Lookup `Resources.Id` using `ActiveDirectoryId`
- Store `Resources.Id` in session/claims for API calls
- Determine user role and `IsApprover` status from database

### Query Patterns

#### Get Approvers for Department
```sql
SELECT * FROM Resources 
WHERE IsApprover = 1 
  AND IsActive = 1 
  AND Department = @Department
```

#### Get Employee's Manager
```sql
SELECT m.* FROM Resources e
JOIN Resources m ON e.ManagerId = m.Id
WHERE e.Id = @EmployeeId AND m.IsActive = 1
```

#### Get All Active Employees
```sql
SELECT * FROM Resources 
WHERE IsActive = 1 
ORDER BY Name
```

### Data Integrity Considerations

#### Historical Data Preservation
- Never delete Resources records referenced by AbsenceRequests
- Use `IsActive = false` for terminated employees
- Maintain audit trail of role changes (future: separate audit table)

#### Orphaned Data Prevention
- AbsenceRequests.EmployeeId has foreign key with CASCADE on delete
- AbsenceRequests.ApproverId has foreign key with NO ACTION (preserve approver reference)
- Migration strategy for inactive approvers: reassign or leave as-is with historical record

## Technical Notes

### Authentication
- User authentication required for all operations
- EmployeeId derived from authenticated user context (via ActiveDirectoryId lookup)
- Role-based authorization for approve/reject/delete operations based on Resources.Role and IsApprover

### Performance
- Date range limited to 6-month window to optimize queries
- Pagination for large result sets (future enhancement)
- Caching for frequently accessed data (resource list, approved absences)
- Index on Email, EmployeeNumber, ActiveDirectoryId for fast lookups

### Validation
- Start date must be before or equal to end date
- Cannot request absences in the past (configurable grace period)
- Minimum 1-day absence duration
- Maximum absence duration: 30 consecutive days (configurable)
- Only active employees can submit/approve requests

## Acceptance Criteria

### Employee Experience
- ✅ Can view all approved absences on initial page load
- ✅ Can toggle to see only their pending requests
- ✅ Can create new absence requests
- ✅ Can edit their pending requests
- ✅ Can cancel their requests
- ✅ Cannot see other employees' pending requests

### Manager Experience
- ✅ Can view all approved absences on initial page load
- ✅ Can toggle to see all pending requests (any employee)
- ✅ Can approve pending requests with comments
- ✅ Can reject pending requests with reason
- ✅ Can see pending request count badge
- ✅ Has access to all employee requests

### System Behavior
- ✅ Calendar refreshes after any status change
- ✅ Status colors correctly indicate request state
- ✅ Toggle button state persists during session
- ✅ Error messages displayed for failed operations
- ✅ Loading indicators shown during API calls
