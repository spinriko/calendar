QUnit.module('Status Colors', function () {

    QUnit.test('getStatusColor returns correct color for Pending', function (assert) {
        const result = getStatusColor("Pending");
        assert.equal(result, "#ffa500cc", "Pending status should be orange");
    });

    QUnit.test('getStatusColor returns correct color for Approved', function (assert) {
        const result = getStatusColor("Approved");
        assert.equal(result, "#6aa84fcc", "Approved status should be green");
    });

    QUnit.test('getStatusColor returns correct color for Rejected', function (assert) {
        const result = getStatusColor("Rejected");
        assert.equal(result, "#cc4125cc", "Rejected status should be red");
    });

    QUnit.test('getStatusColor returns correct color for Cancelled', function (assert) {
        const result = getStatusColor("Cancelled");
        assert.equal(result, "#999999cc", "Cancelled status should be gray");
    });

    QUnit.test('getStatusColor returns default color for unknown status', function (assert) {
        const result = getStatusColor("Unknown");
        assert.equal(result, "#2e78d6cc", "Unknown status should return default blue");
    });

    QUnit.test('getStatusColor handles null status', function (assert) {
        const result = getStatusColor(null);
        assert.equal(result, "#2e78d6cc", "Null status should return default blue");
    });

    QUnit.test('getStatusColor handles undefined status', function (assert) {
        const result = getStatusColor(undefined);
        assert.equal(result, "#2e78d6cc", "Undefined status should return default blue");
    });

    QUnit.test('getStatusColor is case-sensitive', function (assert) {
        const result = getStatusColor("pending");
        assert.equal(result, "#2e78d6cc", "Lowercase 'pending' should not match and return default");
    });
});
