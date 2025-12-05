import {
    determineUserRole,
    getDefaultStatusFilters,
    buildAbsencesUrl,
    getVisibleFilters,
    updateSelectedStatusesFromCheckboxes,
    canCreateAbsenceForResource,
    getResourceSelectionMessage,
    buildContextMenuItems
} from "../../../pto.track/wwwroot/js/calendar-functions";

describe('Integration/Cross-Function Tests', () => {
    describe('Complete workflow: determineUserRole → getDefaultStatusFilters → buildAbsencesUrl', () => {
        it('Admin user workflow: determine role, get filters, build URL', () => {
            const user = { roles: ['Admin', 'Employee'] };
            const role = determineUserRole(user);
            expect(role).toBe('Admin');

            const defaultFilters = getDefaultStatusFilters(role);
            expect(defaultFilters).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, false, true, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Approved&status[]=Rejected&status[]=Cancelled');
        });

        it('Manager user workflow: determine role, get filters, build URL', () => {
            const user = { roles: ['Manager'] };
            const role = determineUserRole(user);
            expect(role).toBe('Manager');

            const defaultFilters = getDefaultStatusFilters(role);
            expect(defaultFilters).toEqual(['Pending', 'Approved']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, true, false, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Approved');
        });

        it('Employee user workflow: determine role, get filters, build URL with employeeId', () => {
            const user = { roles: ['Employee'] };
            const role = determineUserRole(user);
            expect(role).toBe('Employee');

            const defaultFilters = getDefaultStatusFilters(role);
            expect(defaultFilters).toEqual(['Pending']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, false, false, 42);
            expect(url).toBe('/api/absences?&status[]=Pending&employeeId=42');
        });

        it('Employee viewing only approved absences (no employeeId in URL)', () => {
            const user = { roles: ['Employee'] };
            const role = determineUserRole(user);
            expect(role).toBe('Employee');

            const url = buildAbsencesUrl('/api/absences?', ['Approved'], false, false, 42);
            expect(url).toBe('/api/absences?&status[]=Approved');
            expect(url).not.toContain('employeeId');
        });
    });

    describe('Permission check workflow: role determination → canCreateAbsenceForResource → getResourceSelectionMessage', () => {
        it('Admin can create absence for any resource', () => {
            const user = { roles: ['Admin'] };
            const role = determineUserRole(user);
            expect(role).toBe('Admin');

            const canCreate = canCreateAbsenceForResource(1, 2, false, true, false);
            expect(canCreate).toBe(true);

            const message = getResourceSelectionMessage(1, 2, false, true, false);
            expect(message).toBeNull();
        });

        it('Manager can create absence for any resource', () => {
            const user = { roles: ['Manager'] };
            const role = determineUserRole(user);
            expect(role).toBe('Manager');

            const canCreate = canCreateAbsenceForResource(1, 2, true, false, false);
            expect(canCreate).toBe(true);

            const message = getResourceSelectionMessage(1, 2, true, false, false);
            expect(message).toBeNull();
        });

        it('Employee can only create absence for self', () => {
            const user = { roles: ['Employee'] };
            const role = determineUserRole(user);
            expect(role).toBe('Employee');

            const canCreateSelf = canCreateAbsenceForResource(1, 1, false, false, false);
            expect(canCreateSelf).toBe(true);

            const messageSelf = getResourceSelectionMessage(1, 1, false, false, false);
            expect(messageSelf).toBeNull();

            const canCreateOther = canCreateAbsenceForResource(1, 2, false, false, false);
            expect(canCreateOther).toBe(false);

            const messageOther = getResourceSelectionMessage(1, 2, false, false, false);
            expect(messageOther).toMatch(/only create absence requests for yourself/);
        });
    });

    describe('Filter workflow: getVisibleFilters → updateSelectedStatusesFromCheckboxes → buildAbsencesUrl', () => {
        it('Admin filter workflow: all filters visible, user selects some, builds URL', () => {
            const visibleFilters = getVisibleFilters('Admin');
            expect(visibleFilters).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: false },
                filterRejected: { checked: true },
                filterCancelled: { checked: false }
            };
            const selectedStatuses = updateSelectedStatusesFromCheckboxes(filterElements);
            expect(selectedStatuses).toEqual(['Pending', 'Rejected']);

            const url = buildAbsencesUrl('/api/absences?', selectedStatuses, false, true, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Rejected');
        });

        it('Manager filter workflow: limited filters visible, user selects all, builds URL', () => {
            const visibleFilters = getVisibleFilters('Manager');
            expect(visibleFilters).toEqual(['Pending', 'Approved']);

            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: true },
                filterRejected: { checked: false },
                filterCancelled: { checked: false }
            };
            const selectedStatuses = updateSelectedStatusesFromCheckboxes(filterElements);
            expect(selectedStatuses).toEqual(['Pending', 'Approved']);

            const url = buildAbsencesUrl('/api/absences?', selectedStatuses, true, false, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Approved');
        });

        it('Employee filter workflow: all filters visible, selects Pending, includes employeeId', () => {
            const visibleFilters = getVisibleFilters('Employee');
            expect(visibleFilters).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: false },
                filterRejected: { checked: false },
                filterCancelled: { checked: false }
            };
            const selectedStatuses = updateSelectedStatusesFromCheckboxes(filterElements);
            expect(selectedStatuses).toEqual(['Pending']);

            const url = buildAbsencesUrl('/api/absences?', selectedStatuses, false, false, 5);
            expect(url).toBe('/api/absences?&status[]=Pending&employeeId=5');
        });
    });

    describe('Context menu workflow: different roles × different statuses matrix', () => {
        it('Admin with Pending status: sees all actions', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(true);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(true);
        });

        it('Manager with Pending status (not owner): sees Approve/Reject but not Edit', () => {
            const absence = { status: 'Pending', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(false);
        });

        it('Employee with Approved status: only sees View Details', () => {
            const absence = { status: 'Approved', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('Employee with Pending status (owner): sees Edit/Delete but not Approve/Reject', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(true);
            expect(items.some(i => i.text === 'Approve')).toBe(false);
            expect(items.some(i => i.text === 'Reject')).toBe(false);
            expect(items.some(i => i.text === 'Delete')).toBe(true);
        });

        it('Admin with Cancelled status (owner): sees View Details and Delete', () => {
            const absence = { status: 'Cancelled', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Approve')).toBe(false);
            expect(items.some(i => i.text === 'Reject')).toBe(false);
        });

        it('Approver with Pending status (not owner): sees Approve/Reject', () => {
            const absence = { status: 'Pending', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: true };
            const items = buildContextMenuItems(absence, userContext, {});

            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Delete')).toBe(false);
        });
    });
});

