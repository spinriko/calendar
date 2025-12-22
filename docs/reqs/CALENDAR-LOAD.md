# Work Schedule Calendar Load

**Status**: Draft  
**Created**: December 22, 2025  
**Last Updated**: December 22, 2025

## Overview

Work schedules for resources are currently maintained in Excel spreadsheets. This feature will enable uploading these spreadsheets, parsing schedule data, syncing groups/resources, and displaying work schedules in a calendar view similar to the existing Absence Scheduler.

## Business Requirements

### Excel Upload and Parsing

- **BR-1**: System shall accept Excel file uploads (.xlsx format)
- **BR-2**: Each sheet within the workbook represents one group
- **BR-3**: Sheet name corresponds to the group name
- **BR-4**: Each sheet contains resource names and their work schedule data
- **BR-5**: System shall parse all sheets in the uploaded workbook

### Group and Resource Synchronization

- **BR-6**: System shall create groups that do not already exist in the database
- **BR-7**: System shall add resources (users) to their respective groups
- **BR-8**: If a resource does not exist, system shall create the resource record
- **BR-9**: If a resource exists in multiple groups, system shall maintain all group memberships
- **BR-10**: Sync operation shall be atomic per workbook (all or nothing)

### Work Schedule Events

- **BR-11**: Any contiguous block of scheduled time represents a single work event
- **BR-12**: Work schedule events are distinct from absence events
- **BR-13**: Events shall capture: resource, date/time range, group association
- **BR-14**: System shall support updating/replacing work schedules via subsequent uploads

### Work Schedule View

- **BR-15**: New "Work Schedule" page displays work events in calendar format
- **BR-16**: Initial implementation: month view only
- **BR-17**: Calendar view similar to existing Absence Scheduler layout
- **BR-18**: No status filters required (unlike absence events)
- **BR-19**: All user types can view work schedules (no authorization restrictions)
- **BR-20**: View shall show which resources are scheduled to work during selected time period

## Technical Considerations

### Data Model Options

**Option A: Inheritance with Type Discriminator**
- Create base `Event` entity
- `AbsenceEvent` and `WorkEvent` inherit from base
- Use EF Core TPH (Table-Per-Hierarchy) with discriminator column
- **Pros**: Single table, shared event logic, easier to query all events together
- **Cons**: Nullable columns for type-specific properties, larger table

**Option B: Separate Tables**
- `Absences` table (existing)
- New `WorkSchedules` table
- No inheritance relationship
- **Pros**: Clean separation, optimized queries per type, easier to maintain independently
- **Cons**: Duplicate code for shared event logic, harder to query combined calendar

**Recommendation**: Start with **Option B (Separate Tables)** for clarity and maintainability. Can refactor to inheritance later if combined queries become critical.

### Excel Parsing

- Use **EPPlus** (already in use?) or **ClosedXML** library
- Validate sheet structure before processing
- Handle errors gracefully (log invalid rows, report to user)

### Work Schedule Entity (Proposed)

```
WorkSchedule
- Id (int, PK)
- ResourceId (int, FK to Resources)
- GroupId (int, FK to Groups)
- StartDateTime (DateTime)
- EndDateTime (DateTime)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)
- UploadBatchId (Guid) - to track which upload created this event
```

### Upload Processing Flow

1. User uploads Excel file via new "Upload Work Schedule" page
2. System validates file format
3. Parse each sheet:
   - Extract group name from sheet name
   - Extract resource schedules from rows
4. Begin transaction
5. For each group:
   - Create group if not exists
   - For each resource in group:
     - Create resource if not exists
     - Add to group if not already member
   - For each contiguous work block:
     - Create WorkSchedule event
6. Commit transaction
7. Display summary (groups created, resources synced, events added)

## UI/UX Requirements

### Upload Page (New)

- **UI-1**: New page: `/work-schedules/upload`
- **UI-2**: File input control accepting .xlsx files
- **UI-3**: Upload button with progress indicator
- **UI-4**: Results summary after upload:
  - Groups created/updated
  - Resources synced
  - Events added
  - Errors/warnings
- **UI-5**: Link to view uploaded schedules

### Work Schedule Calendar Page (New)

- **UI-6**: New page: `/work-schedules` or `/work-schedules/calendar`
- **UI-7**: Month view calendar (default)
- **UI-8**: Navigation: Previous/Next month buttons
- **UI-9**: Display work events for all resources in selected month
- **UI-10**: Event visualization: show resource name, group, scheduled time block
- **UI-11**: Optional: Color-code by group for visual distinction
- **UI-12**: Optional: Click event to see details (resource, group, hours)
- **UI-13**: No filters required initially (show all work schedules)

### Navigation

- **UI-14**: Add "Work Schedules" link to main navigation menu
- **UI-15**: Distinguish from "Absence Scheduler" in navigation

## Open Questions

1. **Excel Format**: What is the expected column structure? (Resource Name, Date, Start Time, End Time, Hours, etc.)
2. **Contiguous Blocks**: How to determine contiguity? Same day? Within X hours? Explicit column?
3. **Time Zones**: Are work schedules in local time or specific timezone?
4. **Overlapping Events**: Can same resource have overlapping work blocks? Validation needed?
5. **Updates vs Appends**: When re-uploading for same group, should system replace or append events?
6. **Date Range**: Should upload specify effective date range, or infer from data?
7. **Validation Rules**: Required fields? Date range limits? Business hour constraints?
8. **Permissions**: Who can upload work schedules? Admins only? Group managers?

## Future Enhancements (Out of Scope for Initial Release)

- Week view and day view for work schedules
- Edit individual work events via UI
- Delete/archive old work schedules
- Export work schedules back to Excel
- Combined view showing both work schedules and absences
- Filtering by group, resource, date range
- Authorization: restrict viewing to specific groups/managers
- Conflict detection: show when absences overlap with work schedules
- Recurring work schedules (e.g., weekly patterns)

## Dependencies

- Excel parsing library (EPPlus or ClosedXML)
- Existing `Groups` and `Resources` entities
- Calendar view component (can reuse from Absence Scheduler)

## Success Criteria

- [ ] Excel file successfully uploads and parses without errors
- [ ] Groups and resources correctly synced to database
- [ ] Work schedule events created and persisted
- [ ] Work Schedule calendar page displays events in month view
- [ ] No performance degradation with large uploads (e.g., 50+ resources, 1000+ events)
- [ ] Error handling provides clear feedback for invalid files

## Implementation Notes

**Phase 1**: Backend
1. Add WorkSchedule entity and DbContext configuration
2. Create Excel parser service
3. Create group/resource sync service
4. Create upload controller endpoint
5. Add validation and error handling
6. Write unit tests

**Phase 2**: Frontend
1. Create upload page with file input
2. Add upload API call and progress handling
3. Display upload results summary

**Phase 3**: Calendar View
1. Create WorkSchedule calendar page
2. Reuse calendar component from Absence Scheduler
3. Add WorkSchedule data service
4. Render work events in month view
5. Add navigation controls

**Phase 4**: Integration
1. Add navigation menu items
2. Test end-to-end flow
3. Performance testing with realistic data volumes
