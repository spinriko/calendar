# Absence Calendar View Improvements

## Current Behavior

### Calendar Display
- Shows single full work day view
- Difficult to visualize and select multi-day PTO requests
- Limited view of weekly availability

### Time Selection
- Users select a time frame for absences
- Current implementation unclear on granularity (full day vs. partial day)

## Proposed Changes

### 1. Default Calendar View - Work Week
**Change the default calendar view from single day to work week**

**Benefits:**
- Users can see Monday-Friday at a glance
- Easier to identify available days for PTO
- Better context for planning multi-day absences
- More intuitive for typical PTO request patterns

**Implementation Considerations:**
- Default to current week (Monday-Friday)
- Still allow navigation to other weeks
- Maintain responsive design for mobile/tablet views

### 2. Multi-Day Selection Capability
**Enable spanning selections across multiple days**

**User Experience:**
- Click and drag across multiple days to select date range
- Visual feedback showing selected range
- Clear indication of start and end dates
- Ability to modify selection before submission

**Edge Cases to Handle:**
- Selecting across weekends (skip or include?)
- Selecting across different weeks
- Partial day at start/end of range
- Overlapping with existing absences

### 3. Time Picker for Partial Day Absences
**Add time pickers in the absence details form**

**Fields:**
- **Start Time Picker**: Hour and minute when absence begins on the first day
- **End Time Picker**: Hour and minute when absence ends on the last day

**Use Cases:**
- **Full Day(s)**: Start time = 8:00 AM (or work start), End time = 5:00 PM (or work end)
- **Half Day Morning**: Start = 8:00 AM, End = 12:00 PM
- **Half Day Afternoon**: Start = 12:00 PM, End = 5:00 PM
- **Partial Day**: Any custom start/end time within work hours
- **Multi-Day with Partial Start/End**: 
  - Example: Leave at 2:00 PM Friday, return at 10:00 AM Tuesday

**Time Picker Requirements:**
- Default to full work day hours (e.g., 8:00 AM - 5:00 PM)
- Only show time pickers when start date and end date are selected
- Validation: End time must be after start time (accounting for multi-day spans)
- Consider timezone handling if applicable
- Format: 12-hour or 24-hour based on user preference/locale

### 4. Duration Calculation Display
**Show calculated absence duration**

**Display:**
- Total days requested (e.g., "3 days")
- Total hours requested (e.g., "24 hours" or "3 days, 4 hours")
- Breakdown by day if partial days involved

**Benefits:**
- User confirmation of request accuracy
- Clear understanding of PTO balance impact
- Reduces submission errors

## UI/UX Design Questions

### Calendar Component
1. Should we use DayPilot's week view or create custom week selector?
2. How to visually distinguish between:
   - Selectable days
   - Weekends
   - Already-scheduled absences
   - Holidays
   - Today's date

### Time Pickers
1. Time picker style preference:
   - Dropdown selects (hour/minute)
   - Slider/spinner controls
   - Text input with validation
   - Native HTML5 time input

2. Default behavior:
   - Always show time pickers?
   - Only show for single-day selections?
   - Show by default but allow "Full Day" checkbox to hide them?

3. Time increments:
   - 15-minute increments
   - 30-minute increments
   - 1-hour increments
   - Free-form entry

### Interaction Patterns
1. Multi-day selection method:
   - Click start date, click end date (two clicks)
   - Click and drag across dates
   - Date range picker (calendar popup with start/end)

2. Visual feedback:
   - Highlight color for selected range
   - Border/outline for start and end dates
   - Shading for intermediate dates

3. Modification:
   - Allow editing selection before "Create Request"?
   - Clear button to reset selection?
   - Click outside to deselect?

## Technical Considerations

### Data Model
- Current `AbsenceRequest` entity has `StartDate` and `EndDate` (datetime)
- Are these currently storing time components or just dates?
- May need to ensure proper datetime handling (not just date-only)

### API Changes
- `CreateAbsenceRequestDto` - already has DateTime fields
- Verify API accepts and processes time components correctly
- Frontend must send full datetime values (not just dates)

### Validation Rules
1. **Date/Time Rules:**
   - Start datetime must be before end datetime
   - Cannot request absences in the past
   - Cannot request absences beyond X months in future?
   - Check against work hours (no 2:00 AM start times)

2. **Business Rules:**
   - Minimum absence duration (e.g., 1 hour)?
   - Maximum absence duration (e.g., 4 weeks)?
   - Check PTO balance before allowing submission?
   - Prevent overlapping absence requests for same employee

3. **Work Hours:**
   - Define standard work hours (8 AM - 5 PM?)
   - Allow absences outside work hours or constrain?
   - Handle different work schedules per employee?

### Calendar Integration
- Current implementation uses DayPilot Scheduler
- Check DayPilot API for:
  - Week view configuration
  - Multi-day event creation
  - Time granularity settings
  - Custom event rendering for partial days

## Implementation Phases

### Phase 1: Work Week View (Minimum Viable)
- [ ] Change default calendar view to work week (Monday-Friday)
- [ ] Ensure existing functionality works with week view
- [ ] Test on various screen sizes

### Phase 2: Multi-Day Selection
- [ ] Implement date range selection mechanism
- [ ] Add visual feedback for selected range
- [ ] Update absence request form to display selected range
- [ ] Test edge cases (weekends, cross-week selections)

### Phase 3: Time Picker Integration
- [ ] Add time picker controls to absence request form
- [ ] Default to full work day hours
- [ ] Implement validation (end > start, within work hours)
- [ ] Update API calls to include time components

### Phase 4: Duration Display & Polish
- [ ] Calculate and display total days/hours requested
- [ ] Show duration breakdown if partial days
- [ ] Add user-friendly error messages for validation failures
- [ ] Implement loading states and success confirmations

### Phase 5: Testing & Refinement
- [ ] Unit tests for duration calculations
- [ ] Integration tests for API with time components
- [ ] UI/UX testing with actual users
- [ ] Performance testing with large date ranges
- [ ] Accessibility testing (keyboard navigation, screen readers)

## Open Questions

1. **Weekend Handling:**
   - Include weekends in absence requests or automatically skip?
   - Gray out weekends in calendar?
   - Different absence types for weekend vs. weekday?

2. **Partial Day Display:**
   - How to show partial day absences on the calendar?
   - Different color/pattern for partial vs. full day?
   - Tooltip showing exact hours?

3. **Default Time Values:**
   - What are the standard work hours?
   - Do different employee types have different work hours?
   - Should defaults be configurable per employee/department?

4. **Mobile Experience:**
   - How does work week view render on mobile?
   - Alternative UI for small screens?
   - Touch-friendly time pickers?

5. **Existing Data:**
   - How to handle existing absence requests that may only have date (not time)?
   - Migration strategy for existing data?
   - Backwards compatibility needed?

## Success Metrics

- Reduction in time to create multi-day absence requests
- Decrease in user errors/corrections needed
- User satisfaction feedback
- Adoption rate of partial day absence feature
- Reduction in support questions about absence requests

## Related Documentation

- See `ABSENCES_REQUIREMENTS.md` for overall absence management requirements
- See `../../pto.track/wwwroot/js/calendar-functions.mjs` for current calendar implementation
- See `../../pto.track/Pages/Absences.cshtml` for current absence request UI
- See `AbsenceRequest` entity in `../../pto.track.data/Models/`

## Notes

- This feature targets the employee absence request flow
- Manager approval and viewing remains unchanged (for now)
- Future enhancement: Integrate with calendar showing existing team absences
- Consider holiday calendar integration to auto-exclude non-working days
