import { createPermissionStrategy } from "../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('PermissionStrategy Scheduler UI', () => {
    const EMPLOYEE_ID = 100;
    const OTHER_EMPLOYEE_ID = 200;
    const DISABLED_CLASS = "disabled-row";
    const TODAY = new Date();
    const FUTURE = new Date(TODAY.getTime() + 86400000);

    describe('getCellCssClass (Row Color)', () => {
        test('should return null for own row', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.getCellCssClass(FUTURE, TODAY, EMPLOYEE_ID)).toBeNull();
        });

        test('should return disabled class for other employee row when regular employee', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.getCellCssClass(FUTURE, TODAY, OTHER_EMPLOYEE_ID)).toBe(DISABLED_CLASS);
        });

        test('should return null for other employee row when Manager', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Manager'] });
            expect(strategy.getCellCssClass(FUTURE, TODAY, OTHER_EMPLOYEE_ID)).toBeNull();
        });
    });

    describe('canCreateFor (Selection)', () => {
        test('should allow selection for own row', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.canCreateFor(EMPLOYEE_ID)).toBe(true);
        });

        test('should NOT allow selection for other employee row when regular employee', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.canCreateFor(OTHER_EMPLOYEE_ID)).toBe(false);
        });

        test('should allow selection for other employee row when Manager', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Manager'] });
            expect(strategy.canCreateFor(OTHER_EMPLOYEE_ID)).toBe(true);
        });
    });
});
