# GROUP_REQUIREMENTS.md

## Overview
This document defines requirements for introducing the concept of "Groups" for resources in the PTO tracking application. Groups will allow logical organization of resources (e.g., employees, assets) and enable group-based filtering and management in the UI.

## Requirements

### 1. Group Entity ✅ COMPLETED
- Each group represents a collection of resources.
- Group has:
  - Unique identifier (GroupId)
  - Name (required)
  - Additional attributes (optional, extensible in future)

**Status:** ✅ Created [Models/Group.cs](pto.track.data/Models/Group.cs) with GroupId and Name properties, including navigation property to Resources collection.


### 2. Resource Renaming ✅ COMPLETED
- Renamed entity from `SchedulerResource` to `Resource` throughout the codebase
- Deleted old SchedulerResource.cs file
- Updated all references in code files including:
  - Entity configuration in PtoTrackDbContext
  - Test classes (renamed SchedulerResourceValidationTests to ResourceValidationTests)
- Build verified successful

**Status:** ✅ Entity renamed to Resource across the codebase.


### 3. Resource Association and Initial Data ✅ COMPLETED
- Existing resources must consist only of:
  - Test Employee 1
  - Test Employee 2
  - Administrator
  - Approver
  - Manager
- All other resources should be removed.
- These resources should be assigned to "Group 1" as their group.
- Each resource can belong to one group (initially; future: support multiple groups if needed).
- Resources must reference their group (e.g., via GroupId foreign key).

**Status:** ✅ Added GroupId foreign key to Resource entity. Seed data configured with 5 test resources all assigned to Group 1. Migration file: [20251201211824_AddGroupsAndUpdateResources.cs](pto.track.data/Migrations/20251201211824_AddGroupsAndUpdateResources.cs)


### 4. UI Changes (Absences Page) ❌ NOT IMPLEMENTED
- The group indicator should be:
  - A label for most users (shows current group only)
  - A dropdown for Administrators (allows switching between groups)
- Only show resources for the currently selected group.
- Allow Administrators to switch between groups to view associated resources and absences.

**Status:** ❌ UI changes not yet implemented. [Absences.cshtml](pto.track/Pages/Absences.cshtml) shows status filters and impersonation panel, but no group selection UI.

### 5. Roles and Permissions ✅ COMPLETED
- Existing roles continue to represent permission sets for UI actions.
- Group membership does not affect permissions; roles remain the authority for access control.

**Status:** ✅ Roles remain unchanged and function as before. Group membership is separate from permissions.


### 6. API Changes ⚠️ PARTIAL
- Endpoints to:
  - ❌ List groups
  - ❌ Create, update, delete groups
  - ✅ List resources by group (GET /api/resources/group/{groupId}) - Implemented in [ResourcesController.cs:51-58](pto.track/Controllers/ResourcesController.cs#L51-L58)
- ❌ Absences API should support filtering by group.
- The group API endpoints should only be accessible to users with the Administrator role.

**Status:** ⚠️ Partial - Only resource filtering by group implemented via `GetResourcesByGroupAsync()`. Missing GroupsController and absence filtering by group in [AbsencesController.cs](pto.track/Controllers/AbsencesController.cs).

### 7. Suggestions for Extensibility
- Consider supporting nested groups or group hierarchies in future.
- Allow resources to belong to multiple groups if business need arises.
- Add group attributes (e.g., description, color, type) as needed.
- Audit changes to group membership for compliance.
- Enable group-based reporting and analytics.

### 8. Migration and Backward Compatibility ✅ COMPLETED
- Existing resources should be assigned to a default group during migration.
- UI should gracefully handle cases where no group is selected or available.

**Status:** ✅ Migration applied successfully! [20251201211824_AddGroupsAndUpdateResources.cs](pto.track.data/Migrations/20251201211824_AddGroupsAndUpdateResources.cs). This migration:
  - Created Groups table with GroupId and Name
  - Added GroupId column to Resources table
  - Inserted "Group 1" as default group
  - Deleted resources with IDs 6-10 (now only 5 required test resources remain)
  - Updated all remaining resources to GroupId = 1
  - Added foreign key constraint from Resources to Groups

**Note:** Migration was regenerated after the SchedulerResource → Resource rename to ensure proper entity references.



## 9. Navigation and Access ❌ NOT IMPLEMENTED
- Add a menu item in the main layout to access the group management page.
- This menu item should only be visible to users with the Administrator role.
- Create a Group Administration page for managing groups (create, update, delete, view group details and members).

**Status:** ❌ No group management UI or navigation implemented yet. Would need to modify [_Layout.cshtml](pto.track/Pages/Shared/_Layout.cshtml) and create new Razor page.

## Remaining Tasks

### High Priority (Core Functionality)
1. ✅ **Apply Database Migration** - COMPLETED - Groups migration applied to database
2. ❌ **Groups API** - Create GroupsController with CRUD operations (Admin-only access)
3. ❌ **Absence Filtering** - Add group filtering to AbsencesController API (add `groupId` parameter)
4. ❌ **UI Group Selection** - Add group dropdown/label to Absences page
5. ❌ **Update ResourceService** - Ensure GetResourcesByGroupAsync is properly implemented with tests

### Medium Priority (Management UI)
6. ❌ **Group Service** - Create IGroupService and GroupService for business logic
7. ❌ **Group Management Page** - Admin-only Razor page for CRUD operations on groups
8. ❌ **Navigation Menu** - Add Groups menu item for Administrators in _Layout.cshtml

### Low Priority (Polish)
9. ❌ **Error Handling** - Graceful handling when no group selected
10. ⚠️ **Testing** - Update existing tests to include group scenarios (some tests reference groups already)
11. ❌ **Documentation** - Update [README.md](../README.md) and API documentation with group functionality

## Summary Status
✅ **Completed:**
- Database schema design (Group entity, Resource with GroupId FK)
- Entity renamed from SchedulerResource to Resource
- Migration created and **APPLIED to database**
- Basic API endpoint (GET /api/resources/group/{groupId})
- Seed data configuration
- Database now has Groups table and Resources.GroupId column
- Only 5 test resources remain (IDs 1-5)

⚠️ **In Progress/Needs Attention:**
- Testing partially updated but needs completion

❌ **Not Started:**
- GroupsController API
- Group management service layer
- UI implementation (group selection on Absences page)
- Group management page
- Navigation menu updates
- Absence filtering by group

---
Feel free to expand or adjust these requirements as the concept evolves.
