import { createPermissionStrategy } from "../../pto.track/wwwroot/js/strategies/permission-strategies";

describe('Date Validation (Strategy)', () => {
    const EMPLOYEE_ID = 100;
    const OTHER_EMPLOYEE_ID = 200;
    const DISABLED_CLASS = "disabled-row";

    const TODAY = "2023-06-15";
    const PAST_DATE = "2023-06-14";
    const FUTURE_DATE = "2023-06-16";

    describe('getCellCssClass', () => {
        test('should return disabled class for past dates even if user is owner', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.getCellCssClass(PAST_DATE, TODAY, EMPLOYEE_ID)).toBe(DISABLED_CLASS);
        });

        test('should return disabled class for past dates for manager', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Manager'] });
            expect(strategy.getCellCssClass(PAST_DATE, TODAY, OTHER_EMPLOYEE_ID)).toBe(DISABLED_CLASS);
        });

        test('should return disabled class for future dates if user is NOT owner (and not privileged)', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.getCellCssClass(FUTURE_DATE, TODAY, OTHER_EMPLOYEE_ID)).toBe(DISABLED_CLASS);
        });

        test('should return null (enabled) for future dates if user IS owner', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Employee'] });
            expect(strategy.getCellCssClass(FUTURE_DATE, TODAY, EMPLOYEE_ID)).toBeNull();
        });

        test('should return null (enabled) for future dates if user is Manager', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Manager'] });
            expect(strategy.getCellCssClass(FUTURE_DATE, TODAY, OTHER_EMPLOYEE_ID)).toBeNull();
        });

        test('should return null (enabled) for future dates if user is Admin', () => {
            const strategy = createPermissionStrategy({ id: EMPLOYEE_ID, roles: ['Admin'] });
            expect(strategy.getCellCssClass(FUTURE_DATE, TODAY, OTHER_EMPLOYEE_ID)).toBeNull();
        });
    });
});
