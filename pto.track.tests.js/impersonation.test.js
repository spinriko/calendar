const { getVisibleFilters, getDefaultStatusFilters, buildAbsencesUrl } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Impersonation', () => {
    test('Admin impersonation should show all checkboxes', () => {
        const visibleFilters = getVisibleFilters('Admin');
        const defaultFilters = getDefaultStatusFilters('Admin');
        expect(visibleFilters.length).toBe(4);
        expect(defaultFilters.length).toBe(4);
    });

    test('Manager impersonation should show limited checkboxes', () => {
        const visibleFilters = getVisibleFilters('Manager');
        const defaultFilters = getDefaultStatusFilters('Manager');
        expect(visibleFilters.length).toBe(2);
        expect(defaultFilters.length).toBe(2);
        expect(visibleFilters).toContain('Pending');
        expect(visibleFilters).toContain('Approved');
    });

    test('Employee impersonation should show all checkboxes but select only Pending', () => {
        const visibleFilters = getVisibleFilters('Employee');
        const defaultFilters = getDefaultStatusFilters('Employee');
        expect(visibleFilters.length).toBe(4);
        expect(defaultFilters.length).toBe(1);
        expect(defaultFilters).toEqual(['Pending']);
    });

    test('URL should include employeeId only for Employee role', () => {
        const employeeUrl = buildAbsencesUrl(
            '/api/absences?start=2025-11-01&end=2025-11-30',
            ['Pending'],
            false, // not manager
            false, // not admin
            5
        );

        const managerUrl = buildAbsencesUrl(
            '/api/absences?start=2025-11-01&end=2025-11-30',
            ['Pending'],
            true, // is manager
            false,
            5
        );

        expect(employeeUrl).toMatch(/employeeId=5/);
        expect(managerUrl).not.toMatch(/employeeId/);
    });

    test('Switching roles should change visible filters', () => {
        let role = 'Employee';
        let filters = getVisibleFilters(role);
        expect(filters.length).toBe(4);

        role = 'Manager';
        filters = getVisibleFilters(role);
        expect(filters.length).toBe(2);

        role = 'Admin';
        filters = getVisibleFilters(role);
        expect(filters.length).toBe(4);
    });
});