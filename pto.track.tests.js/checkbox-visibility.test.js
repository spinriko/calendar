const { getVisibleFilters, getDefaultStatusFilters } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Checkbox Visibility', () => {
    it('Admin sees all 4 checkboxes', () => {
        const visibleFilters = getVisibleFilters('Admin');
        expect(visibleFilters.length).toBe(4);
        expect(visibleFilters).toContain('Pending');
        expect(visibleFilters).toContain('Approved');
        expect(visibleFilters).toContain('Rejected');
        expect(visibleFilters).toContain('Cancelled');
    });

    it('Manager sees only Pending and Approved checkboxes', () => {
        const visibleFilters = getVisibleFilters('Manager');
        expect(visibleFilters.length).toBe(2);
        expect(visibleFilters).toContain('Pending');
        expect(visibleFilters).toContain('Approved');
        expect(visibleFilters).not.toContain('Rejected');
        expect(visibleFilters).not.toContain('Cancelled');
    });

    it('Approver sees only Pending and Approved checkboxes', () => {
        const visibleFilters = getVisibleFilters('Approver');
        expect(visibleFilters.length).toBe(2);
        expect(visibleFilters).toContain('Pending');
        expect(visibleFilters).toContain('Approved');
        expect(visibleFilters).not.toContain('Rejected');
        expect(visibleFilters).not.toContain('Cancelled');
    });

    it('Employee sees all 4 checkboxes', () => {
        const visibleFilters = getVisibleFilters('Employee');
        expect(visibleFilters.length).toBe(4);
        expect(visibleFilters).toContain('Pending');
        expect(visibleFilters).toContain('Approved');
        expect(visibleFilters).toContain('Rejected');
        expect(visibleFilters).toContain('Cancelled');
    });
});

describe('Default Filter Tests', () => {
    it('Admin defaults to all 4 statuses selected', () => {
        const defaultFilters = getDefaultStatusFilters('Admin');
        expect(defaultFilters.length).toBe(4);
        expect(defaultFilters).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    it('Manager defaults to Pending and Approved', () => {
        const defaultFilters = getDefaultStatusFilters('Manager');
        expect(defaultFilters.length).toBe(2);
        expect(defaultFilters).toEqual(['Pending', 'Approved']);
    });

    it('Approver defaults to Pending and Approved', () => {
        const defaultFilters = getDefaultStatusFilters('Approver');
        expect(defaultFilters.length).toBe(2);
        expect(defaultFilters).toEqual(['Pending', 'Approved']);
    });

    it('Employee defaults to only Pending', () => {
        const defaultFilters = getDefaultStatusFilters('Employee');
        expect(defaultFilters.length).toBe(1);
        expect(defaultFilters).toEqual(['Pending']);
    });
});
