import { createPermissionStrategy } from "../../../pto.track/wwwroot/js/strategies/permission-strategies";
import { buildAbsencesUrl } from "../../../pto.track/wwwroot/js/calendar-functions";

describe('Integration/Cross-Function Tests', () => {
    describe('Complete workflow: Strategy → Filters → URL', () => {
        it('Admin user workflow', () => {
            const user = { id: 1, roles: ['Admin', 'Employee'] };
            const strategy = createPermissionStrategy(user);

            const defaultFilters = strategy.getDefaultFilters();
            expect(defaultFilters).toEqual(['Pending', 'Approved', 'Rejected', 'Cancelled']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, false, true, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Approved&status[]=Rejected&status[]=Cancelled');
        });

        it('Manager user workflow', () => {
            const user = { id: 1, roles: ['Manager'] };
            const strategy = createPermissionStrategy(user);

            const defaultFilters = strategy.getDefaultFilters();
            expect(defaultFilters).toEqual(['Pending', 'Approved']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, true, false, 1);
            expect(url).toBe('/api/absences?&status[]=Pending&status[]=Approved');
        });

        it('Employee user workflow', () => {
            const user = { id: 42, roles: ['Employee'] };
            const strategy = createPermissionStrategy(user);

            const defaultFilters = strategy.getDefaultFilters();
            expect(defaultFilters).toEqual(['Pending']);

            const url = buildAbsencesUrl('/api/absences?', defaultFilters, false, false, 42);
            expect(url).toBe('/api/absences?&status[]=Pending&employeeId=42');
        });
    });
});

