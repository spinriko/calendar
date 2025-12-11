import { createPermissionStrategy } from "../../../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('PermissionStrategy Context Menu Actions', () => {
    describe('Pending status', () => {
        it('allows all actions for manager/owner', () => {
            const absence = { status: 'Pending', employeeId: 1 };
            const strategy = createPermissionStrategy({ id: 1, roles: ['Manager'] });

            expect(strategy.canEdit(absence)).toBe(true);
            expect(strategy.canApprove(absence)).toBe(true);
            expect(strategy.canDelete(absence)).toBe(true);
        });

        it('allows only view for non-owner employee', () => {
            const absence = { status: 'Pending', employeeId: 2 };
            const strategy = createPermissionStrategy({ id: 1, roles: ['Employee'] });

            expect(strategy.canEdit(absence)).toBe(false);
            expect(strategy.canApprove(absence)).toBe(false);
            expect(strategy.canDelete(absence)).toBe(false);
        });

        it('allows approve/reject for approver (not owner)', () => {
            const absence = { status: 'Pending', employeeId: 2 };
            const strategy = createPermissionStrategy({ id: 1, isApprover: true });

            expect(strategy.canEdit(absence)).toBe(false);
            expect(strategy.canApprove(absence)).toBe(true);
            expect(strategy.canDelete(absence)).toBe(false);
        });
    });

    describe('Approved status', () => {
        it('allows no actions for employee owner', () => {
            const absence = { status: 'Approved', employeeId: 1 };
            const strategy = createPermissionStrategy({ id: 1, roles: ['Employee'] });

            expect(strategy.canEdit(absence)).toBe(false);
            expect(strategy.canApprove(absence)).toBe(false);
            expect(strategy.canDelete(absence)).toBe(false);
        });
    });
});


