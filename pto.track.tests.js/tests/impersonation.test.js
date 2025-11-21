QUnit.module('Impersonation', function () {

    QUnit.test('Admin impersonation should show all checkboxes', function (assert) {
        const visibleFilters = getVisibleFilters("Admin");
        const defaultFilters = getDefaultStatusFilters("Admin");

        assert.equal(visibleFilters.length, 4, "Admin should see all 4 checkboxes");
        assert.equal(defaultFilters.length, 4, "Admin should have all 4 statuses selected by default");
    });

    QUnit.test('Manager impersonation should show limited checkboxes', function (assert) {
        const visibleFilters = getVisibleFilters("Manager");
        const defaultFilters = getDefaultStatusFilters("Manager");

        assert.equal(visibleFilters.length, 2, "Manager should see only 2 checkboxes");
        assert.equal(defaultFilters.length, 2, "Manager should have 2 statuses selected by default");
        assert.ok(visibleFilters.includes("Pending"), "Manager should see Pending checkbox");
        assert.ok(visibleFilters.includes("Approved"), "Manager should see Approved checkbox");
    });

    QUnit.test('Employee impersonation should show all checkboxes but select only Pending', function (assert) {
        const visibleFilters = getVisibleFilters("Employee");
        const defaultFilters = getDefaultStatusFilters("Employee");

        assert.equal(visibleFilters.length, 4, "Employee should see all 4 checkboxes");
        assert.equal(defaultFilters.length, 1, "Employee should have only 1 status selected by default");
        assert.deepEqual(defaultFilters, ["Pending"], "Employee should only have Pending selected");
    });

    QUnit.test('URL should include employeeId only for Employee role', function (assert) {
        const employeeUrl = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending"],
            false, // not manager
            false, // not admin
            5
        );

        const managerUrl = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending"],
            true, // is manager
            false,
            5
        );

        assert.ok(employeeUrl.includes("employeeId=5"), "Employee URL should include employeeId");
        assert.notOk(managerUrl.includes("employeeId"), "Manager URL should not include employeeId");
    });

    QUnit.test('Switching roles should change visible filters', function (assert) {
        let role = "Employee";
        let filters = getVisibleFilters(role);
        assert.equal(filters.length, 4, "Employee should see 4 filters");

        role = "Manager";
        filters = getVisibleFilters(role);
        assert.equal(filters.length, 2, "Manager should see 2 filters");

        role = "Admin";
        filters = getVisibleFilters(role);
        assert.equal(filters.length, 4, "Admin should see 4 filters");
    });
});
