const { buildAbsencesUrl } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Absences URL Builder', () => {
    test('Employee: includes employeeId for non-approved', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Pending'],
            false, // isManager
            false, // isAdmin
            123
        );
        expect(url).toMatch(/employeeId=123/);
        expect(url).toMatch(/status\[]=Pending/);
    });

    test('Employee: does not include employeeId for only Approved', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Approved'],
            false,
            false,
            123
        );
        expect(url).not.toMatch(/employeeId/);
        expect(url).toMatch(/status\[]=Approved/);
    });

    test('Manager: does not include employeeId', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Pending', 'Approved'],
            true, // isManager
            false,
            123
        );
        expect(url).not.toMatch(/employeeId/);
        expect(url).toMatch(/status\[]=Pending/);
        expect(url).toMatch(/status\[]=Approved/);
    });

    test('Admin: does not include employeeId', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Pending', 'Approved'],
            false,
            true, // isAdmin
            123
        );
        expect(url).not.toMatch(/employeeId/);
        expect(url).toMatch(/status\[]=Pending/);
        expect(url).toMatch(/status\[]=Approved/);
    });

    test('Multiple statuses: all are included', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Pending', 'Approved', 'Rejected'],
            false,
            false,
            123
        );
        expect(url).toMatch(/status\[]=Pending/);
        expect(url).toMatch(/status\[]=Approved/);
        expect(url).toMatch(/status\[]=Rejected/);
    });

    test('Base URL is preserved', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            ['Pending'],
            false,
            false,
            123
        );
        expect(url.startsWith('/api/absences?start=2025-01-01&end=2025-01-31')).toBe(true);
    });

    test('No statuses: does not add status[]', () => {
        const url = buildAbsencesUrl(
            '/api/absences?start=2025-01-01&end=2025-01-31',
            [],
            false,
            false,
            123
        );
        expect(url).not.toMatch(/status\[]=/);
    });
});