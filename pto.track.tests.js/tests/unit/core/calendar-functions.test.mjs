import {
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
} from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";

describe('getStatusColor', () => {
    it('returns correct color for each status', () => {
        expect(getStatusColor('Pending')).toBe('#ffa500cc');
        expect(getStatusColor('Approved')).toBe('#6aa84fcc');
        expect(getStatusColor('Rejected')).toBe('#cc4125cc');
        expect(getStatusColor('Cancelled')).toBe('#999999cc');
        expect(getStatusColor('Unknown')).toBe('#2e78d6cc');
    });
});

describe('buildAbsencesUrl', () => {
    it('builds URL for manager', () => {
        const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Approved'], true, false, 123);
        expect(url).toContain('status[]=Pending');
        expect(url).toContain('status[]=Approved');
        expect(url).not.toContain('employeeId=123');
    });
    it('builds URL for employee (not only approved)', () => {
        const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending'], false, false, 456);
        expect(url).toContain('employeeId=456');
    });
    it('builds URL for employee (only approved)', () => {
        const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Approved'], false, false, 456);
        expect(url).not.toContain('employeeId=456');
    });
});

describe('determineUserRole', () => {
    it('returns correct role precedence', () => {
        expect(determineUserRole({ roles: ['Admin', 'Manager'] })).toBe('Admin');
        expect(determineUserRole({ roles: ['Manager', 'Approver'] })).toBe('Manager');
        expect(determineUserRole({ roles: ['Approver'] })).toBe('Approver');
        expect(determineUserRole({ roles: [] })).toBe('Employee');
        expect(determineUserRole(null)).toBe('Employee');
    });
});

describe('getDefaultStatusFilters', () => {
    it('returns correct filters for each role', () => {
        expect(getDefaultStatusFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getDefaultStatusFilters('Manager')).toEqual(['Pending', 'Approved']);
        expect(getDefaultStatusFilters('Approver')).toEqual(['Pending', 'Approved']);
        expect(getDefaultStatusFilters('Employee')).toEqual(['Pending']);
    });
});

describe('getVisibleFilters', () => {
    it('returns correct visible filters for each role', () => {
        expect(getVisibleFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getVisibleFilters('Manager')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Approver')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Employee')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });
});

describe('canCreateAbsenceForResource', () => {
    it('allows admin/manager/approver for any resource', () => {
        expect(canCreateAbsenceForResource(1, 2, true, false, false)).toBe(true);
        expect(canCreateAbsenceForResource(1, 2, false, true, false)).toBe(true);
        expect(canCreateAbsenceForResource(1, 2, false, false, true)).toBe(true);
    });
    it('allows employee only for self', () => {
        expect(canCreateAbsenceForResource(1, 1, false, false, false)).toBe(true);
        expect(canCreateAbsenceForResource(1, 2, false, false, false)).toBe(false);
    });
});

describe('getResourceSelectionMessage', () => {
    it('returns null for allowed', () => {
        expect(getResourceSelectionMessage(1, 1, false, false, false)).toBeNull();
        expect(getResourceSelectionMessage(1, 2, true, false, false)).toBeNull();
    });
    it('returns error for denied', () => {
        expect(getResourceSelectionMessage(1, 2, false, false, false)).toMatch(/only create absence requests for yourself/);
    });
});

describe('isUserManagerOrApprover', () => {
    it('detects manager/approver roles', () => {
        expect(isUserManagerOrApprover({ isApprover: true })).toBe(true);
        expect(isUserManagerOrApprover({ roles: ['Manager'] })).toBe(true);
        expect(isUserManagerOrApprover({ roles: ['manager', 'Employee'] })).toBe(true);
        expect(isUserManagerOrApprover({ roles: ['Employee'] })).toBe(false);
        expect(isUserManagerOrApprover(null)).toBe(false);
    });
});


