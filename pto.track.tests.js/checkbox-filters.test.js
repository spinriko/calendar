const { updateSelectedStatusesFromCheckboxes } = require('../pto.track/wwwroot/js/calendar-functions.js');

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
