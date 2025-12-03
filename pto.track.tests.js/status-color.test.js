const { getStatusColor } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Status Colors', () => {
    test('getStatusColor returns correct color for Pending', () => {
        const result = getStatusColor('Pending');
        expect(result).toBe('#ffa500cc');
    });

    test('getStatusColor returns correct color for Approved', () => {
        const result = getStatusColor('Approved');
        expect(result).toBe('#6aa84fcc');
    });

    test('getStatusColor returns correct color for Rejected', () => {
        const result = getStatusColor('Rejected');
        expect(result).toBe('#cc4125cc');
    });

    test('getStatusColor returns correct color for Cancelled', () => {
        const result = getStatusColor('Cancelled');
        expect(result).toBe('#999999cc');
    });

    test('getStatusColor returns default color for unknown status', () => {
        const result = getStatusColor('Unknown');
        expect(result).toBe('#2e78d6cc');
    });

    test('getStatusColor handles null status', () => {
        const result = getStatusColor(null);
        expect(result).toBe('#2e78d6cc');
    });

    test('getStatusColor handles undefined status', () => {
        const result = getStatusColor(undefined);
        expect(result).toBe('#2e78d6cc');
    });

    test('getStatusColor is case-sensitive', () => {
        const result = getStatusColor('pending');
        expect(result).toBe('#2e78d6cc');
    });
});