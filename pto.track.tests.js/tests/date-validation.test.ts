import {
    getCellCssClass
} from '../../pto.track/wwwroot/js/calendar-functions';

describe('Date Validation', () => {
    const EMPLOYEE_ID = 100;
    const OTHER_EMPLOYEE_ID = 200;
    const DISABLED_CLASS = "disabled-row";

    // Mock DayPilot.Date behavior using simple numbers or strings for comparison
    // In JS, "2023-01-01" < "2023-01-02" is true
    const TODAY = "2023-06-15";
    const PAST_DATE = "2023-06-14";
    const FUTURE_DATE = "2023-06-16";

    describe('getCellCssClass', () => {
        test('should return disabled class for past dates even if user is owner', () => {
            const css = getCellCssClass(PAST_DATE, TODAY, EMPLOYEE_ID, EMPLOYEE_ID, false, false, false);
            expect(css).toBe(DISABLED_CLASS);
        });

        test('should return disabled class for past dates for manager', () => {
            const css = getCellCssClass(PAST_DATE, TODAY, EMPLOYEE_ID, OTHER_EMPLOYEE_ID, true, false, false);
            expect(css).toBe(DISABLED_CLASS);
        });

        test('should return disabled class for future dates if user is NOT owner (and not privileged)', () => {
            const css = getCellCssClass(FUTURE_DATE, TODAY, EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, false, false);
            expect(css).toBe(DISABLED_CLASS);
        });

        test('should return null (enabled) for future dates if user IS owner', () => {
            const css = getCellCssClass(FUTURE_DATE, TODAY, EMPLOYEE_ID, EMPLOYEE_ID, false, false, false);
            expect(css).toBeNull();
        });

        test('should return null (enabled) for future dates if user is Manager', () => {
            const css = getCellCssClass(FUTURE_DATE, TODAY, EMPLOYEE_ID, OTHER_EMPLOYEE_ID, true, false, false);
            expect(css).toBeNull();
        });

        test('should return null (enabled) for future dates if user is Admin', () => {
            const css = getCellCssClass(FUTURE_DATE, TODAY, EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, true, false);
            expect(css).toBeNull();
        });
    });
});
