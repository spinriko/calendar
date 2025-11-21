QUnit.module("Checkbox Visibility Tests", function () {
    QUnit.test("Admin sees all 4 checkboxes", function (assert) {
        // Admin role should see all checkboxes
        const visibleFilters = getVisibleFilters("Admin");

        assert.equal(visibleFilters.length, 4, "Admin should see 4 checkboxes");
        assert.ok(visibleFilters.includes("Pending"), "Admin should see Pending");
        assert.ok(visibleFilters.includes("Approved"), "Admin should see Approved");
        assert.ok(visibleFilters.includes("Rejected"), "Admin should see Rejected");
        assert.ok(visibleFilters.includes("Cancelled"), "Admin should see Cancelled");
    });

    QUnit.test("Manager sees only Pending and Approved checkboxes", function (assert) {
        const visibleFilters = getVisibleFilters("Manager");

        assert.equal(visibleFilters.length, 2, "Manager should see only 2 checkboxes");
        assert.ok(visibleFilters.includes("Pending"), "Manager should see Pending");
        assert.ok(visibleFilters.includes("Approved"), "Manager should see Approved");
        assert.notOk(visibleFilters.includes("Rejected"), "Manager should NOT see Rejected");
        assert.notOk(visibleFilters.includes("Cancelled"), "Manager should NOT see Cancelled");
    });

    QUnit.test("Approver sees only Pending and Approved checkboxes", function (assert) {
        const visibleFilters = getVisibleFilters("Approver");

        assert.equal(visibleFilters.length, 2, "Approver should see only 2 checkboxes");
        assert.ok(visibleFilters.includes("Pending"), "Approver should see Pending");
        assert.ok(visibleFilters.includes("Approved"), "Approver should see Approved");
        assert.notOk(visibleFilters.includes("Rejected"), "Approver should NOT see Rejected");
        assert.notOk(visibleFilters.includes("Cancelled"), "Approver should NOT see Cancelled");
    });

    QUnit.test("Employee sees all 4 checkboxes", function (assert) {
        const visibleFilters = getVisibleFilters("Employee");

        assert.equal(visibleFilters.length, 4, "Employee should see 4 checkboxes");
        assert.ok(visibleFilters.includes("Pending"), "Employee should see Pending");
        assert.ok(visibleFilters.includes("Approved"), "Employee should see Approved");
        assert.ok(visibleFilters.includes("Rejected"), "Employee should see Rejected");
        assert.ok(visibleFilters.includes("Cancelled"), "Employee should see Cancelled");
    });
});

QUnit.module("Default Filter Tests", function () {
    QUnit.test("Admin defaults to all 4 statuses selected", function (assert) {
        const defaultFilters = getDefaultStatusFilters("Admin");

        assert.equal(defaultFilters.length, 4, "Admin should default to 4 statuses");
        assert.deepEqual(defaultFilters, ["Pending", "Approved", "Rejected", "Cancelled"],
            "Admin should default to all statuses");
    });

    QUnit.test("Manager defaults to Pending and Approved", function (assert) {
        const defaultFilters = getDefaultStatusFilters("Manager");

        assert.equal(defaultFilters.length, 2, "Manager should default to 2 statuses");
        assert.deepEqual(defaultFilters, ["Pending", "Approved"],
            "Manager should default to Pending and Approved");
    });

    QUnit.test("Approver defaults to Pending and Approved", function (assert) {
        const defaultFilters = getDefaultStatusFilters("Approver");

        assert.equal(defaultFilters.length, 2, "Approver should default to 2 statuses");
        assert.deepEqual(defaultFilters, ["Pending", "Approved"],
            "Approver should default to Pending and Approved");
    });

    QUnit.test("Employee defaults to only Pending", function (assert) {
        const defaultFilters = getDefaultStatusFilters("Employee");

        assert.equal(defaultFilters.length, 1, "Employee should default to 1 status");
        assert.deepEqual(defaultFilters, ["Pending"],
            "Employee should default to Pending only");
    });
});
