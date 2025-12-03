const { canCreateAbsenceForResource, getResourceSelectionMessage } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Employee Restrictions', () => {
    it('Employee can create for self', () => {
        expect(canCreateAbsenceForResource(5, 5, false, false)).toBe(true);
    });

    it('Employee cannot create for others', () => {
        expect(canCreateAbsenceForResource(5, 3, false, false)).toBe(false);
    });

    it('Manager can create for anyone', () => {
        expect(canCreateAbsenceForResource(5, 3, true, false)).toBe(true);
    });

    it('Admin can create for anyone', () => {
        expect(canCreateAbsenceForResource(5, 3, false, true)).toBe(true);
    });

    it('Approver can create for anyone', () => {
        expect(canCreateAbsenceForResource(5, 3, false, false, true)).toBe(true);
    });

    it('getResourceSelectionMessage - Employee selecting other employee', () => {
        const message = getResourceSelectionMessage(5, 3, false, false);
        expect(message).toMatch(/only create/);
        expect(message).toMatch(/yourself/);
    });

    it('getResourceSelectionMessage - Employee selecting self', () => {
        const message = getResourceSelectionMessage(5, 5, false, false);
        expect(message).toBe(null);
    });

    it('getResourceSelectionMessage - Manager can select anyone', () => {
        const message = getResourceSelectionMessage(5, 3, true, false);
        expect(message).toBe(null);
    });
});
