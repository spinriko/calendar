import { updateSelectedStatusesFromCheckboxes } from "../../../../pto.track/wwwroot/js/calendar-functions";

describe('updateSelectedStatusesFromCheckboxes', () => {
    describe('Multiple checkboxes', () => {
        it('returns selected statuses from checked boxes', () => {
            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: false },
                filterRejected: { checked: true },
                filterCancelled: { checked: false }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Pending', 'Rejected']);
        });

        it('returns empty array if none checked', () => {
            const filterElements = {
                filterPending: { checked: false },
                filterApproved: { checked: false },
                filterRejected: { checked: false },
                filterCancelled: { checked: false }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual([]);
        });

        it('returns all four statuses when all checkboxes checked', () => {
            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: true },
                filterRejected: { checked: true },
                filterCancelled: { checked: true }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        });
    });

    describe('Single checkbox scenarios', () => {
        it('returns only Pending when only Pending checked', () => {
            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: false },
                filterRejected: { checked: false },
                filterCancelled: { checked: false }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Pending']);
        });

        it('returns only Approved when only Approved checked', () => {
            const filterElements = {
                filterPending: { checked: false },
                filterApproved: { checked: true },
                filterRejected: { checked: false },
                filterCancelled: { checked: false }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Approved']);
        });

        it('returns only Rejected when only Rejected checked', () => {
            const filterElements = {
                filterPending: { checked: false },
                filterApproved: { checked: false },
                filterRejected: { checked: true },
                filterCancelled: { checked: false }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Rejected']);
        });

        it('returns only Cancelled when only Cancelled checked', () => {
            const filterElements = {
                filterPending: { checked: false },
                filterApproved: { checked: false },
                filterRejected: { checked: false },
                filterCancelled: { checked: true }
            };
            expect(updateSelectedStatusesFromCheckboxes(filterElements)).toEqual(['Cancelled']);
        });
    });

    describe('Status ordering', () => {
        it('maintains correct order: Pending, Approved, Rejected, Cancelled', () => {
            const filterElements = {
                filterPending: { checked: true },
                filterApproved: { checked: true },
                filterRejected: { checked: true },
                filterCancelled: { checked: true }
            };
            const result = updateSelectedStatusesFromCheckboxes(filterElements);
            expect(result[0]).toBe('Pending');
            expect(result[1]).toBe('Approved');
            expect(result[2]).toBe('Rejected');
            expect(result[3]).toBe('Cancelled');
        });

        it('maintains order with subset of statuses', () => {
            const filterElements = {
                filterPending: { checked: false },
                filterApproved: { checked: true },
                filterRejected: { checked: false },
                filterCancelled: { checked: true }
            };
            const result = updateSelectedStatusesFromCheckboxes(filterElements);
            expect(result).toEqual(['Approved', 'Cancelled']);
            expect(result[0]).toBe('Approved');
            expect(result[1]).toBe('Cancelled');
        });
    });
});


