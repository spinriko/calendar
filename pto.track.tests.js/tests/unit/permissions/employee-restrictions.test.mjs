import { canCreateAbsenceForResource, getResourceSelectionMessage } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";
// ...existing code from employee-restrictions.test.js...
describe('canCreateAbsenceForResource', () => {
    describe('Admin/Manager/Approver permissions', () => {
        it('allows admin for any resource', () => {
            expect(canCreateAbsenceForResource(1, 2, false, true, false)).toBe(true);
        });

        it('allows manager for any resource', () => {
            expect(canCreateAbsenceForResource(1, 2, true, false, false)).toBe(true);
        });

        it('allows approver for any resource', () => {
            expect(canCreateAbsenceForResource(1, 2, false, false, true)).toBe(true);
        });

        it('allows when all three roles are true', () => {
            expect(canCreateAbsenceForResource(1, 2, true, true, true)).toBe(true);
        });

        it('allows when multiple roles are true', () => {
            expect(canCreateAbsenceForResource(1, 2, true, true, false)).toBe(true);
            expect(canCreateAbsenceForResource(1, 2, true, false, true)).toBe(true);
            expect(canCreateAbsenceForResource(1, 2, false, true, true)).toBe(true);
        });
    });

    describe('Employee permissions', () => {
        it('allows employee only for self', () => {
            expect(canCreateAbsenceForResource(1, 1, false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource(1, 2, false, false, false)).toBe(false);
        });

        it('works with string employee IDs', () => {
            expect(canCreateAbsenceForResource('1', '1', false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource('1', '2', false, false, false)).toBe(false);
        });

        it('works with mixed string and number IDs (loose equality)', () => {
            expect(canCreateAbsenceForResource(1, '1', false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource('1', 1, false, false, false)).toBe(true);
        });

        it('handles null employee IDs', () => {
            expect(canCreateAbsenceForResource(null, null, false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource(null, 1, false, false, false)).toBe(false);
            expect(canCreateAbsenceForResource(1, null, false, false, false)).toBe(false);
        });

        it('handles undefined employee IDs', () => {
            expect(canCreateAbsenceForResource(undefined, undefined, false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource(undefined, 1, false, false, false)).toBe(false);
            expect(canCreateAbsenceForResource(1, undefined, false, false, false)).toBe(false);
        });

        it('handles zero as employee ID', () => {
            expect(canCreateAbsenceForResource(0, 0, false, false, false)).toBe(true);
            expect(canCreateAbsenceForResource(0, 1, false, false, false)).toBe(false);
            expect(canCreateAbsenceForResource(1, 0, false, false, false)).toBe(false);
        });
    });
});

describe('getResourceSelectionMessage', () => {
    describe('Allowed scenarios', () => {
        it('returns null for employee creating for self', () => {
            expect(getResourceSelectionMessage(1, 1, false, false, false)).toBeNull();
        });

        it('returns null for admin creating for others', () => {
            expect(getResourceSelectionMessage(1, 2, false, true, false)).toBeNull();
        });

        it('returns null for manager creating for others', () => {
            expect(getResourceSelectionMessage(1, 2, true, false, false)).toBeNull();
        });

        it('returns null for approver creating for others', () => {
            expect(getResourceSelectionMessage(1, 2, false, false, true)).toBeNull();
        });

        it('returns null when all three roles are true', () => {
            expect(getResourceSelectionMessage(1, 2, true, true, true)).toBeNull();
        });
    });

    describe('Denied scenarios', () => {
        it('returns error for employee creating for others', () => {
            expect(getResourceSelectionMessage(1, 2, false, false, false)).toMatch(/only create absence requests for yourself/);
        });

        it('returns error message with string IDs', () => {
            expect(getResourceSelectionMessage('1', '2', false, false, false)).toMatch(/only create absence requests for yourself/);
        });

        it('returns error for null/undefined employee IDs mismatch', () => {
            expect(getResourceSelectionMessage(null, 1, false, false, false)).toMatch(/only create absence requests for yourself/);
            expect(getResourceSelectionMessage(undefined, 1, false, false, false)).toMatch(/only create absence requests for yourself/);
        });

        it('returns error for zero vs non-zero employee ID', () => {
            expect(getResourceSelectionMessage(0, 1, false, false, false)).toMatch(/only create absence requests for yourself/);
        });
    });
});


