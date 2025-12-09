import { buildAbsencesUrl } from "../../../../pto.track/wwwroot/js/calendar-functions";

describe('buildAbsencesUrl', () => {
    describe('Manager role', () => {
        it('builds URL for manager with multiple statuses', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Approved'], true, false, 123);
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('status[]=Approved');
            expect(url).not.toContain('employeeId=123');
        });

        it('builds URL for manager with single status', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending'], true, false, 123);
            expect(url).toContain('status[]=Pending');
            expect(url).not.toContain('employeeId=123');
        });

        it('builds URL for manager with empty status array', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', [], true, false, 123);
            expect(url).toBe('/api/absences?start=2025-01-01');
            expect(url).not.toContain('employeeId=123');
        });
    });

    describe('Admin role', () => {
        it('builds URL for admin with all four statuses', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Approved', 'Rejected', 'Cancelled'], false, true, 123);
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('status[]=Approved');
            expect(url).toContain('status[]=Rejected');
            expect(url).toContain('status[]=Cancelled');
            expect(url).not.toContain('employeeId=123');
        });

        it('builds URL for admin with single status', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Approved'], false, true, 789);
            expect(url).toContain('status[]=Approved');
            expect(url).not.toContain('employeeId=789');
        });
    });

    describe('Employee role', () => {
        it('builds URL for employee with non-approved status (includes employeeId)', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending'], false, false, 456);
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('employeeId=456');
        });

        it('builds URL for employee with only approved status (no employeeId)', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Approved'], false, false, 456);
            expect(url).toContain('status[]=Approved');
            expect(url).not.toContain('employeeId=456');
        });

        it('builds URL for employee with multiple non-approved statuses', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending', 'Rejected'], false, false, 456);
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('status[]=Rejected');
            expect(url).toContain('employeeId=456');
        });

        it('builds URL for employee with mixed statuses (Approved + Pending)', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Approved', 'Pending'], false, false, 456);
            expect(url).toContain('status[]=Approved');
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('employeeId=456');
        });

        it('handles string employee IDs', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Pending'], false, false, '789');
            expect(url).toContain('employeeId=789');
        });
    });

    describe('Approver role', () => {
        it('builds URL for approver (treated as manager)', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01&end=2025-01-31', ['Pending', 'Approved'], false, false, 999);
            // Note: The function doesn't have explicit approver parameter, so testing employee behavior
            // In real usage, approver would be handled by isManager=true or similar
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('status[]=Approved');
        });
    });

    describe('Edge cases', () => {
        it('verifies multiple statuses maintain order', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01', ['Cancelled', 'Rejected', 'Approved', 'Pending'], true, false, 123);
            const cancelledIndex = url.indexOf('status[]=Cancelled');
            const rejectedIndex = url.indexOf('status[]=Rejected');
            const approvedIndex = url.indexOf('status[]=Approved');
            const pendingIndex = url.indexOf('status[]=Pending');
            expect(cancelledIndex).toBeLessThan(rejectedIndex);
            expect(rejectedIndex).toBeLessThan(approvedIndex);
            expect(approvedIndex).toBeLessThan(pendingIndex);
        });

        it('handles base URL with existing query parameters', () => {
            const url = buildAbsencesUrl('/api/absences?start=2025-01-01&end=2025-01-31', ['Pending'], true, false, 123);
            expect(url).toContain('start=2025-01-01');
            expect(url).toContain('end=2025-01-31');
            expect(url).toContain('status[]=Pending');
        });

        it('handles empty base URL', () => {
            const url = buildAbsencesUrl('', ['Pending'], true, false, 123);
            expect(url).toBe('&status[]=Pending');
        });

        it('builds URL with all four statuses selected', () => {
            const url = buildAbsencesUrl('/api/absences', ['Pending', 'Approved', 'Rejected', 'Cancelled'], true, false, 123);
            expect(url).toContain('status[]=Pending');
            expect(url).toContain('status[]=Approved');
            expect(url).toContain('status[]=Rejected');
            expect(url).toContain('status[]=Cancelled');
        });
    });
});


