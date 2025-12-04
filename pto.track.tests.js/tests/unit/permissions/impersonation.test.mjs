import { getVisibleFilters, getDefaultStatusFilters, buildAbsencesUrl } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";
// ...existing code from impersonation.test.js...
describe('getVisibleFilters', () => {
    it('returns correct visible filters for each role', () => {
        expect(getVisibleFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        expect(getVisibleFilters('Manager')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Approver')).toEqual(['Pending', 'Approved']);
        expect(getVisibleFilters('Employee')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
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

describe('buildAbsencesUrl', () => {
    it('builds URL for manager', () => {
        const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Approved'], true, false, 123);
        expect(url).toContain('status[]=Pending');
        expect(url).toContain('status[]=Approved');
        expect(url).not.toContain('employeeId=123');
    });
});


