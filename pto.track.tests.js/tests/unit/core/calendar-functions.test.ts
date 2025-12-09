import {
    getStatusColor,
    buildAbsencesUrl
} from "../../../../pto.track/wwwroot/js/calendar-functions";

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


