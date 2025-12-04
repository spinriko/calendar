import { getStatusColor } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";
// ...existing code from status-color.test.js...
describe('getStatusColor', () => {
    describe('Standard statuses', () => {
        it('returns correct color for each status', () => {
            expect(getStatusColor('Pending')).toBe('#ffa500cc');
            expect(getStatusColor('Approved')).toBe('#6aa84fcc');
            expect(getStatusColor('Rejected')).toBe('#cc4125cc');
            expect(getStatusColor('Cancelled')).toBe('#999999cc');
            expect(getStatusColor('Unknown')).toBe('#2e78d6cc');
        });
    });

    describe('Case sensitivity', () => {
        it('is case-sensitive for lowercase status names', () => {
            expect(getStatusColor('pending')).toBe('#2e78d6cc');
            expect(getStatusColor('approved')).toBe('#2e78d6cc');
            expect(getStatusColor('rejected')).toBe('#2e78d6cc');
            expect(getStatusColor('cancelled')).toBe('#2e78d6cc');
        });

        it('is case-sensitive for uppercase status names', () => {
            expect(getStatusColor('PENDING')).toBe('#2e78d6cc');
            expect(getStatusColor('APPROVED')).toBe('#2e78d6cc');
            expect(getStatusColor('REJECTED')).toBe('#2e78d6cc');
            expect(getStatusColor('CANCELLED')).toBe('#2e78d6cc');
        });

        it('is case-sensitive for mixed case status names', () => {
            expect(getStatusColor('PeNdInG')).toBe('#2e78d6cc');
            expect(getStatusColor('ApPrOvEd')).toBe('#2e78d6cc');
        });
    });

    describe('Edge cases', () => {
        it('returns default color for empty string', () => {
            expect(getStatusColor('')).toBe('#2e78d6cc');
        });

        it('returns default color for null value', () => {
            expect(getStatusColor(null)).toBe('#2e78d6cc');
        });

        it('returns default color for undefined value', () => {
            expect(getStatusColor(undefined)).toBe('#2e78d6cc');
        });

        it('returns default color for whitespace-only string', () => {
            expect(getStatusColor('   ')).toBe('#2e78d6cc');
        });

        it('returns default color for unknown status', () => {
            expect(getStatusColor('InProgress')).toBe('#2e78d6cc');
            expect(getStatusColor('Draft')).toBe('#2e78d6cc');
        });
    });
});


