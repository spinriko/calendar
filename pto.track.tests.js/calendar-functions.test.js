const {
    getStatusColor,
    buildAbsencesUrl,
    determineUserRole,
    getDefaultStatusFilters,
    getVisibleFilters,
    updateSelectedStatusesFromCheckboxes,
    isUserManagerOrApprover,
    canCreateAbsenceForResource,
    getResourceSelectionMessage,
    buildContextMenuItems
} = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('getStatusColor', () => {
    it('returns correct color for each status', () => {
        expect(getStatusColor('Pending')).toBe('#ffa500cc');
        expect(getStatusColor('Approved')).toBe('#6aa84fcc');
        expect(getStatusColor('Rejected')).toBe('#cc4125cc');
        expect(getStatusColor('Cancelled')).toBe('#999999cc');
        expect(getStatusColor('Unknown')).toBe('#2e78d6cc');
        expect(getStatusColor(null)).toBe('#2e78d6cc');
    });
});

describe('buildAbsencesUrl', () => {
    it('builds URL for manager with multiple statuses', () => {
        expect(buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Approved'], true, false, 123)).toBe('/api/absences?start=2025-01-01&status[]=Pending&status[]=Approved');
    });
    it('builds URL for employee with non-approved status', () => {
        expect(buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending'], false, false, 456)).toBe('/api/absences?start=2025-01-01&status[]=Pending&employeeId=456');
    });
    it('builds URL for employee with only Approved', () => {
        expect(buildAbsencesUrl('/api/absences?start=2025-01-01', ['Approved'], false, false, 456)).toBe('/api/absences?start=2025-01-01&status[]=Approved');
    });
});

describe('determineUserRole', () => {
    it('returns correct role based on precedence', () => {
        expect(determineUserRole({ roles: ['Admin', 'Manager'] })).toBe('Admin');
        expect(determineUserRole({ roles: ['Manager', 'Approver'] })).toBe('Manager');
        expect(determineUserRole({ roles: ['Approver'] })).toBe('Approver');
        expect(determineUserRole({ roles: [] })).toBe('Employee');
        expect(determineUserRole(null)).toBe('Employee');
    });
});

describe('getDefaultStatusFilters', () => {
    it('returns correct defaults for each role', () => {
        expect(getDefaultStatusFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getDefaultStatusFilters('Manager')).toEqual(['Pending', 'Approved']);
        expect(getDefaultStatusFilters('Approver')).toEqual(['Pending', 'Approved']);
        expect(getDefaultStatusFilters('Employee')).toEqual(['Pending']);
        expect(getDefaultStatusFilters('Other')).toEqual(['Pending']);
    });
});

describe('getVisibleFilters', () => {
    it('returns correct visible filters for each role', () => {
        expect(getVisibleFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getVisibleFilters('Employee')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getVisibleFilters('Manager')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Approver')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Other')).toEqual(['Pending', 'Approved']);
    });
});

describe('isUserManagerOrApprover', () => {
    it('returns true for manager/approver/isApprover', () => {
        expect(isUserManagerOrApprover({ roles: ['Manager'] })).toBe(true);
        expect(isUserManagerOrApprover({ roles: ['Approver'] })).toBe(true);
        expect(isUserManagerOrApprover({ isApprover: true, roles: ['Employee'] })).toBe(true);
        expect(isUserManagerOrApprover({ roles: ['manager'] })).toBe(true);
    });
    it('returns false for regular employee or null', () => {
        expect(isUserManagerOrApprover({ roles: ['Employee'] })).toBe(false);
        expect(isUserManagerOrApprover(null)).toBe(false);
    });
});

describe('canCreateAbsenceForResource', () => {
    it('returns true for admin/manager/approver', () => {
        expect(canCreateAbsenceForResource(1, 2, true, false)).toBe(true);
        expect(canCreateAbsenceForResource(1, 2, false, true)).toBe(true);
        expect(canCreateAbsenceForResource(1, 2, false, false, true)).toBe(true);
    });
    it('returns true for employee creating for self', () => {
        expect(canCreateAbsenceForResource(1, 1, false, false)).toBe(true);
    });
    it('returns false for employee creating for another', () => {
        expect(canCreateAbsenceForResource(1, 2, false, false)).toBe(false);
    });
});

describe('getResourceSelectionMessage', () => {
    it('returns null if allowed, error message if not', () => {
        expect(getResourceSelectionMessage(1, 1, false, false)).toBe(null);
        expect(getResourceSelectionMessage(1, 2, false, false)).toMatch(/You can only create absence requests/);
    });
});

describe('buildContextMenuItems', () => {
    it('returns menu items for pending, owner', () => {
        const absence = { status: 'Pending', employeeId: 1 };
        const userContext = { currentEmployeeId: 1, isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(absence, userContext, {});
        expect(items.some(i => i.text === 'View Details')).toBe(true);
        expect(items.some(i => i.text === 'Edit Reason')).toBe(true);
        expect(items.some(i => i.text === 'Delete')).toBe(true);
    });
    it('returns approve/reject for manager', () => {
        const absence = { status: 'Pending', employeeId: 2 };
        const userContext = { currentEmployeeId: 1, isAdmin: false, isManager: true, isApprover: false };
        const items = buildContextMenuItems(absence, userContext, {});
        expect(items.some(i => i.text === 'Approve')).toBe(true);
        expect(items.some(i => i.text === 'Reject')).toBe(true);
    });
});

describe('updateSelectedStatusesFromCheckboxes', () => {
    it('returns all checked statuses', () => {
        const mockElements = {
            filterPending: { checked: true },
            filterApproved: { checked: true },
            filterRejected: { checked: false },
            filterCancelled: { checked: false }
        };
        const result = updateSelectedStatusesFromCheckboxes(mockElements);
        expect(result.length).toBe(2);
        expect(result).toContain('Pending');
        expect(result).toContain('Approved');
    });

    it('returns empty array when none checked', () => {
        const mockElements = {
            filterPending: { checked: false },
            filterApproved: { checked: false },
            filterRejected: { checked: false },
            filterCancelled: { checked: false }
        };
        const result = updateSelectedStatusesFromCheckboxes(mockElements);
        expect(result.length).toBe(0);
        expect(result).toEqual([]);
    });

    it('returns all statuses when all checked', () => {
        const mockElements = {
            filterPending: { checked: true },
            filterApproved: { checked: true },
            filterRejected: { checked: true },
            filterCancelled: { checked: true }
        };
        const result = updateSelectedStatusesFromCheckboxes(mockElements);
        expect(result.length).toBe(4);
        expect(result).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    it('handles only Rejected checked', () => {
        const mockElements = {
            filterPending: { checked: false },
            filterApproved: { checked: false },
            filterRejected: { checked: true },
            filterCancelled: { checked: false }
        };
        const result = updateSelectedStatusesFromCheckboxes(mockElements);
        expect(result.length).toBe(1);
        expect(result[0]).toBe('Rejected');
    });
});
