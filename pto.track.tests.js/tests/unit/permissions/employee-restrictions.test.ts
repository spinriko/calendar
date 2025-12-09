import { createPermissionStrategy } from "../../../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('PermissionStrategy Creation', () => {
    describe('Admin/Manager/Approver permissions', () => {
        it('allows admin for any resource', () => {
            const strategy = createPermissionStrategy({ id: 1, roles: ['Admin'] });
            expect(strategy.canCreateFor(2)).toBe(true);
        });

        it('allows manager for any resource', () => {
            const strategy = createPermissionStrategy({ id: 1, roles: ['Manager'] });
            expect(strategy.canCreateFor(2)).toBe(true);
        });

        it('allows approver for any resource', () => {
            const strategy = createPermissionStrategy({ id: 1, isApprover: true });
            expect(strategy.canCreateFor(2)).toBe(true);
        });
    });

    describe('Employee permissions', () => {
        it('allows employee only for self', () => {
            const strategy = createPermissionStrategy({ id: 1, roles: ['Employee'] });
            expect(strategy.canCreateFor(1)).toBe(true);
            expect(strategy.canCreateFor(2)).toBe(false);
        });
    });
});


