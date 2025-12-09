import {
    getSchedulerRowColor,
    shouldAllowSelection
} from '../../pto.track/wwwroot/js/calendar-functions';

describe('Scheduler Permissions', () => {
    const EMPLOYEE_ID = 100;
    const OTHER_EMPLOYEE_ID = 200;
    const GRAY_COLOR = "#eeeeee";

    describe('getSchedulerRowColor', () => {
        test('should return null (default color) for own row', () => {
            const color = getSchedulerRowColor(EMPLOYEE_ID, EMPLOYEE_ID, false, false, false);
            expect(color).toBeNull();
        });

        test('should return gray color for other employee row when regular employee', () => {
            const color = getSchedulerRowColor(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, false, false);
            expect(color).toBe(GRAY_COLOR);
        });

        test('should return null for other employee row when Manager', () => {
            const color = getSchedulerRowColor(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, true, false, false);
            expect(color).toBeNull();
        });

        test('should return null for other employee row when Admin', () => {
            const color = getSchedulerRowColor(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, true, false);
            expect(color).toBeNull();
        });

        test('should return null for other employee row when Approver', () => {
            const color = getSchedulerRowColor(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, false, true);
            expect(color).toBeNull();
        });
    });

    describe('shouldAllowSelection', () => {
        test('should allow selection for own row', () => {
            const allowed = shouldAllowSelection(EMPLOYEE_ID, EMPLOYEE_ID, false, false, false);
            expect(allowed).toBe(true);
        });

        test('should NOT allow selection for other employee row when regular employee', () => {
            const allowed = shouldAllowSelection(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, false, false);
            expect(allowed).toBe(false);
        });

        test('should allow selection for other employee row when Manager', () => {
            const allowed = shouldAllowSelection(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, true, false, false);
            expect(allowed).toBe(true);
        });

        test('should allow selection for other employee row when Admin', () => {
            const allowed = shouldAllowSelection(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, true, false);
            expect(allowed).toBe(true);
        });

        test('should allow selection for other employee row when Approver', () => {
            const allowed = shouldAllowSelection(EMPLOYEE_ID, OTHER_EMPLOYEE_ID, false, false, true);
            expect(allowed).toBe(true);
        });
    });
});
