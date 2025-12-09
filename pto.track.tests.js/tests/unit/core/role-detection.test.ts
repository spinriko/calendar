import {
    getVisibleFilters,
    getDefaultStatusFilters,
    isUserManagerOrApprover,
    determineUserRole
} from "../../../../pto.track/wwwroot/js/calendar-functions";

describe('Role-based filter visibility', () => {
    describe('Standard roles', () => {
        it('Admin sees all filters', () => {
            expect(getVisibleFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        });

        it('Manager sees only pending/approved', () => {
            expect(getVisibleFilters('Manager')).toEqual(['Pending', 'Approved']);
        });

        it('Approver sees only pending/approved', () => {
            expect(getVisibleFilters('Approver')).toEqual(['Pending', 'Approved']);
        });

        it('Employee sees all filters', () => {
            expect(getVisibleFilters('Employee')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        });
    });

    describe('Edge cases', () => {
        it('Unknown role string defaults to Manager/Approver filters', () => {
            expect(getVisibleFilters('SuperUser')).toEqual(['Pending', 'Approved']);
        });

        it('Null role defaults to Manager/Approver filters', () => {
            expect(getVisibleFilters(null)).toEqual(['Pending', 'Approved']);
        });

        it('Undefined role defaults to Manager/Approver filters', () => {
            expect(getVisibleFilters(undefined)).toEqual(['Pending', 'Approved']);
        });

        it('Empty string role defaults to Manager/Approver filters', () => {
            expect(getVisibleFilters('')).toEqual(['Pending', 'Approved']);
        });

        it('Whitespace-only string defaults to Manager/Approver filters', () => {
            expect(getVisibleFilters('   ')).toEqual(['Pending', 'Approved']);
        });

        it('Lowercase role name defaults to Manager/Approver filters (case-sensitive)', () => {
            expect(getVisibleFilters('admin')).toEqual(['Pending', 'Approved']);
            expect(getVisibleFilters('manager')).toEqual(['Pending', 'Approved']);
            expect(getVisibleFilters('employee')).toEqual(['Pending', 'Approved']);
        });
    });
});

describe('Role-based default status filters', () => {
    describe('Standard roles', () => {
        it('Admin default filters', () => {
            expect(getDefaultStatusFilters('Admin')).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
        });

        it('Manager default filters', () => {
            expect(getDefaultStatusFilters('Manager')).toEqual(['Pending', 'Approved']);
        });

        it('Approver default filters', () => {
            expect(getDefaultStatusFilters('Approver')).toEqual(['Pending', 'Approved']);
        });

        it('Employee default filters', () => {
            expect(getDefaultStatusFilters('Employee')).toEqual(['Pending']);
        });
    });

    describe('Edge cases', () => {
        it('Unknown role string defaults to Employee filter', () => {
            expect(getDefaultStatusFilters('SuperUser')).toEqual(['Pending']);
        });

        it('Null role defaults to Employee filter', () => {
            expect(getDefaultStatusFilters(null)).toEqual(['Pending']);
        });

        it('Undefined role defaults to Employee filter', () => {
            expect(getDefaultStatusFilters(undefined)).toEqual(['Pending']);
        });

        it('Empty string role defaults to Employee filter', () => {
            expect(getDefaultStatusFilters('')).toEqual(['Pending']);
        });

        it('Whitespace-only string defaults to Employee filter', () => {
            expect(getDefaultStatusFilters('   ')).toEqual(['Pending']);
        });

        it('Lowercase role name defaults to Employee filter (case-sensitive)', () => {
            expect(getDefaultStatusFilters('admin')).toEqual(['Pending']);
            expect(getDefaultStatusFilters('manager')).toEqual(['Pending']);
            expect(getDefaultStatusFilters('approver')).toEqual(['Pending']);
        });
    });
});

describe('isUserManagerOrApprover', () => {
    describe('Basic role detection', () => {
        it('detects isApprover flag', () => {
            expect(isUserManagerOrApprover({ isApprover: true })).toBe(true);
        });

        it('detects Manager role in roles array', () => {
            expect(isUserManagerOrApprover({ roles: ['Manager'] })).toBe(true);
        });

        it('detects manager role (lowercase) in roles array', () => {
            expect(isUserManagerOrApprover({ roles: ['manager', 'Employee'] })).toBe(true);
        });

        it('returns false for Employee role only', () => {
            expect(isUserManagerOrApprover({ roles: ['Employee'] })).toBe(false);
        });

        it('returns false for null user', () => {
            expect(isUserManagerOrApprover(null)).toBe(false);
        });
    });

    describe('Combined conditions', () => {
        it('returns true when user has both isApprover:true AND roles:["Manager"]', () => {
            const user = { isApprover: true, roles: ['Manager'] };
            expect(isUserManagerOrApprover(user)).toBe(true);
        });

        it('returns true when isApprover:true regardless of roles array', () => {
            expect(isUserManagerOrApprover({ isApprover: true, roles: ['Employee'] })).toBe(true);
        });

        it('returns true when roles include Manager regardless of isApprover:false', () => {
            expect(isUserManagerOrApprover({ isApprover: false, roles: ['Manager'] })).toBe(true);
        });
    });

    describe('Case variations', () => {
        it('detects MANAGER (uppercase)', () => {
            expect(isUserManagerOrApprover({ roles: ['MANAGER'] })).toBe(true);
        });

        it('detects APPROVER (uppercase)', () => {
            expect(isUserManagerOrApprover({ roles: ['APPROVER'] })).toBe(true);
        });

        it('detects MaNaGeR (mixed case)', () => {
            expect(isUserManagerOrApprover({ roles: ['MaNaGeR'] })).toBe(true);
        });

        it('detects ApProVer (mixed case)', () => {
            expect(isUserManagerOrApprover({ roles: ['ApProVer'] })).toBe(true);
        });

        it('detects manager in array with multiple entries', () => {
            expect(isUserManagerOrApprover({ roles: ['Employee', 'manager', 'User'] })).toBe(true);
        });

        it('detects approver in array with multiple entries', () => {
            expect(isUserManagerOrApprover({ roles: ['Employee', 'APPROVER', 'User'] })).toBe(true);
        });
    });

    describe('Edge cases', () => {
        it('returns false for roles array with only other roles', () => {
            expect(isUserManagerOrApprover({ roles: ['Employee', 'User'] })).toBe(false);
        });

        it('returns false for empty user object', () => {
            expect(isUserManagerOrApprover({})).toBe(undefined);
        });

        it('returns false for user with roles: undefined (explicitly undefined)', () => {
            expect(isUserManagerOrApprover({ roles: undefined })).toBe(undefined);
        });

        it('returns false for user with roles: null', () => {
            expect(isUserManagerOrApprover({ roles: null })).toBe(undefined);
        });

        it('returns false for user with empty roles array', () => {
            expect(isUserManagerOrApprover({ roles: [] })).toBe(false);
        });

        it('returns false for undefined user', () => {
            expect(isUserManagerOrApprover(undefined)).toBe(false);
        });

        it('returns true for roles array with multiple entries including manager/approver', () => {
            expect(isUserManagerOrApprover({ roles: ['Admin', 'Manager', 'Employee'] })).toBe(true);
            expect(isUserManagerOrApprover({ roles: ['Admin', 'Approver', 'Employee'] })).toBe(true);
        });
    });
});

describe('determineUserRole', () => {
    describe('Role precedence', () => {
        it('returns Admin when roles include Admin', () => {
            const user = { roles: ['Admin', 'Manager', 'Employee'] };
            expect(determineUserRole(user)).toBe('Admin');
        });

        it('returns Manager when roles include Manager but not Admin', () => {
            const user = { roles: ['Manager', 'Employee'] };
            expect(determineUserRole(user)).toBe('Manager');
        });

        it('returns Approver when roles include Approver but not Admin or Manager', () => {
            const user = { roles: ['Approver', 'Employee'] };
            expect(determineUserRole(user)).toBe('Approver');
        });

        it('respects precedence: Admin > Manager > Approver', () => {
            expect(determineUserRole({ roles: ['Approver', 'Manager', 'Admin'] })).toBe('Admin');
            expect(determineUserRole({ roles: ['Manager', 'Approver'] })).toBe('Manager');
            expect(determineUserRole({ roles: ['Approver'] })).toBe('Approver');
        });
    });

    describe('Edge cases', () => {
        it('returns Employee when user has roles property with empty array', () => {
            const user = { roles: [] };
            expect(determineUserRole(user)).toBe('Employee');
        });

        it('returns Employee when user has undefined roles property', () => {
            const user = { roles: undefined };
            expect(determineUserRole(user)).toBe('Employee');
        });

        it('returns Employee when user object has no roles property', () => {
            const user = { name: 'John Doe' };
            expect(determineUserRole(user)).toBe('Employee');
        });

        it('returns Employee when user is null', () => {
            expect(determineUserRole(null)).toBe('Employee');
        });

        it('returns Employee when user is undefined', () => {
            expect(determineUserRole(undefined)).toBe('Employee');
        });
    });

    describe('Case sensitivity', () => {
        it('is case-sensitive for role names (lowercase does not match)', () => {
            expect(determineUserRole({ roles: ['admin'] })).toBe('Employee');
            expect(determineUserRole({ roles: ['manager'] })).toBe('Employee');
            expect(determineUserRole({ roles: ['approver'] })).toBe('Employee');
        });

        it('requires exact case match for Admin', () => {
            expect(determineUserRole({ roles: ['ADMIN'] })).toBe('Employee');
            expect(determineUserRole({ roles: ['AdMiN'] })).toBe('Employee');
        });

        it('requires exact case match for Manager', () => {
            expect(determineUserRole({ roles: ['MANAGER'] })).toBe('Employee');
            expect(determineUserRole({ roles: ['MaNaGeR'] })).toBe('Employee');
        });

        it('requires exact case match for Approver', () => {
            expect(determineUserRole({ roles: ['APPROVER'] })).toBe('Employee');
            expect(determineUserRole({ roles: ['ApProVer'] })).toBe('Employee');
        });
    });
});


