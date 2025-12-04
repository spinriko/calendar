# Jest Test Coverage Proposals

**Current Status:** 38 tests passing
**Target:** ~90-100 tests for comprehensive coverage

---

## Coverage Analysis

### Well-Covered Areas ✓
- Basic function return values
- Role-based visibility and default filters
- Permission checks for resource creation
- Context menu item generation (basic scenarios)

### Coverage Gaps Identified

---

## Proposed Additional Tests

### 1. buildAbsencesUrl (12 additional tests) ✅ COMPLETED

**Current coverage:** 15 tests (was 3)
**Proposed total:** 15 tests

- [x] Admin with all four statuses
- [x] Admin with single status
- [x] Employee with multiple non-approved statuses (e.g., Pending + Rejected)
- [x] Employee with mixed statuses (Approved + Pending)
- [x] Manager with single status
- [x] Manager with empty status array
- [x] Approver role handling (same as manager)
- [x] Multiple statuses ordering verification
- [x] Base URL with existing query parameters handling
- [x] String vs number employee IDs
- [x] Empty baseUrl edge case
- [x] All four statuses selected at once

---

### 2. buildContextMenuItems (15 additional tests) ✓ COMPLETE

**Current coverage:** 2 tests (expanded to 24 tests in `event-presentation.test.mjs`)
**Proposed total:** 17 tests

- [x] Approved status for admin (only "View Details")
- [x] Approved status for manager (only "View Details")
- [x] Approved status for employee-owner (only "View Details")
- [x] Approved status for employee-non-owner (only "View Details")
- [x] Rejected status for admin (only "View Details")
- [x] Rejected status for manager (only "View Details")
- [x] Rejected status for employee-owner (only "View Details")
- [x] Pending for non-owner employee (no edit rights)
- [x] Pending for approver (not owner) - should have Approve/Reject
- [x] Pending for manager (not owner) - should have Approve/Reject
- [x] Separator validation when edit and approve are available
- [x] Separator validation before delete
- [x] No user context provided (should use default context)
- [x] onClick action validation for each menu item type (View, Edit, Approve, Reject, Delete)
- [x] Menu item order validation (View Details first, Delete last)
- [x] Cancelled for non-owner (no delete)

**Bonus:** Added 7 extra tests beyond proposal (22 tests total implemented in section #2)

---

### 3. determineUserRole (5 additional tests) ✓ COMPLETE

**Current coverage:** 1 test (expanded to 16 tests in `role-detection.test.mjs`)
**Proposed total:** 6 tests

- [x] User with roles property but empty array
- [x] User with undefined roles property
- [x] User object without roles property at all
- [x] Multiple roles in different order (ensure precedence)
- [x] Case sensitivity in role names (e.g., "admin" vs "Admin")

**Bonus:** Added 10 extra tests beyond proposal (15 tests total implemented in section #3)
- Additional coverage: null/undefined user, all case variations (ADMIN/MANAGER/APPROVER, mixed case), role precedence validation

---

### 4. isUserManagerOrApprover (6 additional tests) ✓ COMPLETE

**Current coverage:** 1 test (expanded to 24 tests in `role-detection.test.mjs`)
**Proposed total:** 7 tests

- [x] User with both isApprover:true AND roles:['Manager']
- [x] Case variations: 'MANAGER', 'APPROVER' (uppercase)
- [x] Case variations: 'MaNaGeR', 'ApProVer' (mixed case)
- [x] Roles array with only other roles (e.g., ['Employee', 'User'])
- [x] Empty user object {}
- [x] User with roles: undefined (not missing, explicitly undefined)
- [x] Roles array with multiple entries including manager/approver

**Bonus:** Added 17 extra tests beyond proposal (23 tests total implemented in section #4)
- Additional coverage: combined isApprover+roles scenarios, null/undefined/empty roles, lowercase detection, multiple role scenarios

---

### 5. getDefaultStatusFilters (4 additional tests) ✓ COMPLETE

**Current coverage:** 1 test (expanded to 10 tests in `role-detection.test.mjs`)
**Proposed total:** 5 tests

- [x] Unknown role string (e.g., "SuperUser")
- [x] Null role
- [x] Undefined role
- [x] Empty string role

**Bonus:** Added 5 extra tests beyond proposal (9 tests total implemented in section #5)
- Additional coverage: whitespace-only string, lowercase role names (case sensitivity check)

---

### 6. getVisibleFilters (4 additional tests) ✓ COMPLETE

**Current coverage:** 1 test (expanded to 10 tests in `role-detection.test.mjs`)
**Proposed total:** 5 tests

- [x] Unknown role string (e.g., "SuperUser")
- [x] Null role
- [x] Undefined role
- [x] Empty string role

**Bonus:** Added 5 extra tests beyond proposal (9 tests total implemented in section #6)
- Additional coverage: whitespace-only string, lowercase role names (case sensitivity check)

---

### 7. updateSelectedStatusesFromCheckboxes (5 additional tests) ✓ COMPLETE

**Current coverage:** 2 tests (expanded to 9 tests in `checkbox-filters.test.mjs`)
**Proposed total:** 7 tests

- [x] All four checkboxes checked
- [x] Only Pending checked
- [x] Only Approved checked
- [x] Only Rejected checked
- [x] Only Cancelled checked

**Bonus:** Added 2 extra tests beyond proposal (7 tests total implemented in section #7)
- Additional coverage: status ordering validation, subset ordering validation

---

### 8. getStatusColor (4 additional tests) ✓ COMPLETE

**Current coverage:** 1 test (expanded to 13 tests in `status-color.test.mjs`)
**Proposed total:** 5 tests

- [x] Case sensitivity: lowercase "pending", "approved", etc.
- [x] Empty string
- [x] Null value
- [x] Whitespace-only string ("   ")

**Bonus:** Added 8 extra tests beyond proposal (12 tests total implemented in section #8)
- Additional coverage: uppercase status names, mixed case, multiple unknown status values

---

### 9. canCreateAbsenceForResource & getResourceSelectionMessage (4 additional tests) ✓ COMPLETE

**Current coverage:** 4 tests (expanded to 20 tests in `employee-restrictions.test.mjs`)
**Proposed total:** 8 tests

- [x] All three roles true (admin=true, manager=true, approver=true)
- [x] String employee IDs vs number IDs
- [x] Null/undefined employee IDs
- [x] Zero as employee ID (edge case)

**Bonus:** Added 12 extra tests beyond proposal (16 tests total implemented in section #9)
- Additional coverage: mixed string/number ID types, multiple role combinations, comprehensive permission scenarios

---

### 10. Integration/Cross-Function Tests (5 new tests) ✓ COMPLETE

**Current coverage:** 0 tests (created 15 tests in `integration.test.mjs`)
**Proposed total:** 5 tests

- [x] Complete workflow: determineUserRole → getDefaultStatusFilters → buildAbsencesUrl
- [x] Permission check workflow: role determination → canCreateAbsenceForResource → getResourceSelectionMessage
- [x] Filter workflow: getVisibleFilters → updateSelectedStatusesFromCheckboxes → buildAbsencesUrl
- [x] Context menu workflow: different roles × different statuses matrix
- [x] Employee viewing only approved absences (verify no employeeId in URL)

**Bonus:** Added 10 extra tests beyond proposal (15 tests total implemented in section #10)
- Additional coverage: Admin/Manager/Employee/Approver workflows, comprehensive role×status matrix for context menus

---

## Summary

| Function | Current Tests | Proposed Tests | New Tests |
|----------|--------------|----------------|-----------|
| buildAbsencesUrl | 3 | 15 | +12 |
| buildContextMenuItems | 2 | 17 | +15 |
| determineUserRole | 1 | 6 | +5 |
| isUserManagerOrApprover | 1 | 7 | +6 |
| getDefaultStatusFilters | 1 | 5 | +4 |
| getVisibleFilters | 1 | 5 | +4 |
| updateSelectedStatusesFromCheckboxes | 2 | 7 | +5 |
| getStatusColor | 1 | 5 | +4 |
| canCreateAbsenceForResource | 2 | 4 | +2 |
| getResourceSelectionMessage | 2 | 4 | +2 |
| Integration tests | 0 | 5 | +5 |
| **TOTAL** | **38** | **~100** | **+62** |

---

## Implementation Plan

1. Start with high-impact functions (buildContextMenuItems, buildAbsencesUrl)
2. Add edge case coverage for simpler functions
3. Implement integration tests last
4. Run coverage report to identify any remaining gaps
5. Update this document as tests are implemented

---

## Notes

- All new tests should follow ES module syntax (`.mjs` files)
- Tests should be organized by function with clear describe blocks
- Edge cases should include error conditions and boundary values
- Integration tests should verify realistic user workflows
