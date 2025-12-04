import { getVisibleFilters, getDefaultStatusFilters } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";

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


