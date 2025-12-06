import { createPermissionStrategy } from "../../../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('PermissionStrategy Filters', () => {
    it('returns correct visible filters for each role', () => {
        const admin = createPermissionStrategy({ id: 1, roles: ['Admin'] });
        expect(admin.getVisibleFilters()).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

        const manager = createPermissionStrategy({ id: 2, roles: ['Manager'] });
        expect(manager.getVisibleFilters()).toEqual(['Pending', 'Approved']);

        const approver = createPermissionStrategy({ id: 3, isApprover: true });
        expect(approver.getVisibleFilters()).toEqual(['Pending', 'Approved']);

        const employee = createPermissionStrategy({ id: 4, roles: ['Employee'] });
        expect(employee.getVisibleFilters()).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);
    });

    it('returns correct default filters for each role', () => {
        const admin = createPermissionStrategy({ id: 1, roles: ['Admin'] });
        expect(admin.getDefaultFilters()).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

        const manager = createPermissionStrategy({ id: 2, roles: ['Manager'] });
        expect(manager.getDefaultFilters()).toEqual(['Pending', 'Approved']);

        const approver = createPermissionStrategy({ id: 3, isApprover: true });
        expect(approver.getDefaultFilters()).toEqual(['Pending', 'Approved']);

        const employee = createPermissionStrategy({ id: 4, roles: ['Employee'] });
        expect(employee.getDefaultFilters()).toEqual(['Pending']);
    });
});


