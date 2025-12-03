const {
    determineUserRole,
    getDefaultStatusFilters,
    getVisibleFilters,
    isUserManagerOrApprover
} = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Role Detection', () => {
    test('determineUserRole returns Admin for user with Admin role', () => {
        const user = { roles: ['Admin', 'Employee'] };
        const result = determineUserRole(user);
        expect(result).toBe('Admin');
    });

    test('determineUserRole returns Manager for user with Manager role', () => {
        const user = { roles: ['Manager', 'Employee'] };
        const result = determineUserRole(user);
        expect(result).toBe('Manager');
    });

    test('determineUserRole returns Approver for user with Approver role', () => {
        const user = { roles: ['Approver', 'Employee'] };
        const result = determineUserRole(user);
        expect(result).toBe('Approver');
    });

    test('determineUserRole returns Employee for user with only Employee role', () => {
        const user = { roles: ['Employee'] };
        const result = determineUserRole(user);
        expect(result).toBe('Employee');
    });

    test('determineUserRole prioritizes Admin over other roles', () => {
        const user = { roles: ['Employee', 'Manager', 'Admin'] };
        const result = determineUserRole(user);
        expect(result).toBe('Admin');
    });

    test('determineUserRole handles null user', () => {
        const result = determineUserRole(null);
        expect(result).toBe('Employee');
    });

    test('determineUserRole handles undefined user', () => {
        const result = determineUserRole(undefined);
        expect(result).toBe('Employee');
    });

    test('determineUserRole handles user without roles', () => {
        const user = { id: 1, name: 'Test' };
        const result = determineUserRole(user);
        expect(result).toBe('Employee');
    });

    test('getDefaultStatusFilters returns all statuses for Admin', () => {
        const result = getDefaultStatusFilters('Admin');
        expect(result).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    test('getDefaultStatusFilters returns Pending and Approved for Manager', () => {
        const result = getDefaultStatusFilters('Manager');
        expect(result).toEqual(['Pending', 'Approved']);
    });

    test('getDefaultStatusFilters returns Pending and Approved for Approver', () => {
        const result = getDefaultStatusFilters('Approver');
        expect(result).toEqual(['Pending', 'Approved']);
    });

    test('getDefaultStatusFilters returns only Pending for Employee', () => {
        const result = getDefaultStatusFilters('Employee');
        expect(result).toEqual(['Pending']);
    });

    test('getVisibleFilters returns all filters for Admin', () => {
        const result = getVisibleFilters('Admin');
        expect(result).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    test('getVisibleFilters returns all filters for Employee', () => {
        const result = getVisibleFilters('Employee');
        expect(result).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    test('getVisibleFilters returns limited filters for Manager', () => {
        const result = getVisibleFilters('Manager');
        expect(result).toEqual(['Pending', 'Approved']);
    });

    test('isUserManagerOrApprover returns true for user with Manager role', () => {
        const user = { roles: ['Manager', 'Employee'] };
        const result = isUserManagerOrApprover(user);
        expect(result).toBe(true);
    });

    test('isUserManagerOrApprover returns true for user with Approver role', () => {
        const user = { roles: ['Approver', 'Employee'] };
        const result = isUserManagerOrApprover(user);
        expect(result).toBe(true);
    });

    test('isUserManagerOrApprover returns true when isApprover flag is true', () => {
        const user = { isApprover: true, roles: ['Employee'] };
        const result = isUserManagerOrApprover(user);
        expect(result).toBe(true);
    });

    test('isUserManagerOrApprover returns false for regular Employee', () => {
        const user = { roles: ['Employee'] };
        const result = isUserManagerOrApprover(user);
        expect(result).toBe(false);
    });

    test('isUserManagerOrApprover handles case-insensitive role names', () => {
        const user = { roles: ['manager'] };
        const result = isUserManagerOrApprover(user);
        expect(result).toBe(true);
    });
});