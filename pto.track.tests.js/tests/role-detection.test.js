QUnit.module('Role Detection', function () {

    QUnit.test('determineUserRole returns Admin for user with Admin role', function (assert) {
        const user = { roles: ["Admin", "Employee"] };
        const result = determineUserRole(user);
        assert.equal(result, "Admin", "Should return Admin");
    });

    QUnit.test('determineUserRole returns Manager for user with Manager role', function (assert) {
        const user = { roles: ["Manager", "Employee"] };
        const result = determineUserRole(user);
        assert.equal(result, "Manager", "Should return Manager");
    });

    QUnit.test('determineUserRole returns Approver for user with Approver role', function (assert) {
        const user = { roles: ["Approver", "Employee"] };
        const result = determineUserRole(user);
        assert.equal(result, "Approver", "Should return Approver");
    });

    QUnit.test('determineUserRole returns Employee for user with only Employee role', function (assert) {
        const user = { roles: ["Employee"] };
        const result = determineUserRole(user);
        assert.equal(result, "Employee", "Should return Employee");
    });

    QUnit.test('determineUserRole prioritizes Admin over other roles', function (assert) {
        const user = { roles: ["Employee", "Manager", "Admin"] };
        const result = determineUserRole(user);
        assert.equal(result, "Admin", "Should return Admin when multiple roles present");
    });

    QUnit.test('determineUserRole handles null user', function (assert) {
        const result = determineUserRole(null);
        assert.equal(result, "Employee", "Should return Employee for null user");
    });

    QUnit.test('determineUserRole handles undefined user', function (assert) {
        const result = determineUserRole(undefined);
        assert.equal(result, "Employee", "Should return Employee for undefined user");
    });

    QUnit.test('determineUserRole handles user without roles', function (assert) {
        const user = { id: 1, name: "Test" };
        const result = determineUserRole(user);
        assert.equal(result, "Employee", "Should return Employee when roles missing");
    });

    QUnit.test('getDefaultStatusFilters returns all statuses for Admin', function (assert) {
        const result = getDefaultStatusFilters("Admin");
        assert.deepEqual(result, ["Pending", "Approved", "Rejected", "Cancelled"],
            "Admin should see all statuses by default");
    });

    QUnit.test('getDefaultStatusFilters returns Pending and Approved for Manager', function (assert) {
        const result = getDefaultStatusFilters("Manager");
        assert.deepEqual(result, ["Pending", "Approved"],
            "Manager should see Pending and Approved by default");
    });

    QUnit.test('getDefaultStatusFilters returns Pending and Approved for Approver', function (assert) {
        const result = getDefaultStatusFilters("Approver");
        assert.deepEqual(result, ["Pending", "Approved"],
            "Approver should see Pending and Approved by default");
    });

    QUnit.test('getDefaultStatusFilters returns only Pending for Employee', function (assert) {
        const result = getDefaultStatusFilters("Employee");
        assert.deepEqual(result, ["Pending"],
            "Employee should only see Pending by default");
    });

    QUnit.test('getVisibleFilters returns all filters for Admin', function (assert) {
        const result = getVisibleFilters("Admin");
        assert.deepEqual(result, ["Pending", "Approved", "Rejected", "Cancelled"],
            "Admin should see all filter checkboxes");
    });

    QUnit.test('getVisibleFilters returns all filters for Employee', function (assert) {
        const result = getVisibleFilters("Employee");
        assert.deepEqual(result, ["Pending", "Approved", "Rejected", "Cancelled"],
            "Employee should see all filter checkboxes");
    });

    QUnit.test('getVisibleFilters returns limited filters for Manager', function (assert) {
        const result = getVisibleFilters("Manager");
        assert.deepEqual(result, ["Pending", "Approved"],
            "Manager should only see Pending and Approved checkboxes");
    });

    QUnit.test('isUserManagerOrApprover returns true for user with Manager role', function (assert) {
        const user = { roles: ["Manager", "Employee"] };
        const result = isUserManagerOrApprover(user);
        assert.ok(result, "Should return true for Manager");
    });

    QUnit.test('isUserManagerOrApprover returns true for user with Approver role', function (assert) {
        const user = { roles: ["Approver", "Employee"] };
        const result = isUserManagerOrApprover(user);
        assert.ok(result, "Should return true for Approver");
    });

    QUnit.test('isUserManagerOrApprover returns true when isApprover flag is true', function (assert) {
        const user = { isApprover: true, roles: ["Employee"] };
        const result = isUserManagerOrApprover(user);
        assert.ok(result, "Should return true when isApprover flag is set");
    });

    QUnit.test('isUserManagerOrApprover returns false for regular Employee', function (assert) {
        const user = { roles: ["Employee"] };
        const result = isUserManagerOrApprover(user);
        assert.notOk(result, "Should return false for regular Employee");
    });

    QUnit.test('isUserManagerOrApprover handles case-insensitive role names', function (assert) {
        const user = { roles: ["manager"] };
        const result = isUserManagerOrApprover(user);
        assert.ok(result, "Should handle lowercase role names");
    });
});
