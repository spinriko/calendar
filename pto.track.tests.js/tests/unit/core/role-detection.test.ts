import { createPermissionStrategy, AdminStrategy, ManagerStrategy, EmployeeStrategy } from "../../../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('PermissionStrategy Factory', () => {
    it('creates AdminStrategy for admin role', () => {
        const strategy = createPermissionStrategy({ roles: ['Admin'] });
        expect(strategy).toBeInstanceOf(AdminStrategy);
    });

    it('creates ManagerStrategy for manager role', () => {
        const strategy = createPermissionStrategy({ roles: ['Manager'] });
        expect(strategy).toBeInstanceOf(ManagerStrategy);
    });

    it('creates ManagerStrategy for approver role', () => {
        const strategy = createPermissionStrategy({ roles: ['Approver'] });
        expect(strategy).toBeInstanceOf(ManagerStrategy);
    });

    it('creates ManagerStrategy for isApprover flag', () => {
        const strategy = createPermissionStrategy({ isApprover: true });
        expect(strategy).toBeInstanceOf(ManagerStrategy);
    });

    it('creates EmployeeStrategy for employee role', () => {
        const strategy = createPermissionStrategy({ roles: ['Employee'] });
        expect(strategy).toBeInstanceOf(EmployeeStrategy);
    });

    it('creates EmployeeStrategy for no roles', () => {
        const strategy = createPermissionStrategy({ roles: [] });
        expect(strategy).toBeInstanceOf(EmployeeStrategy);
    });
});


