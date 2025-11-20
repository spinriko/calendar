/**
 * Tests for employee restrictions on creating absence requests
 */

QUnit.module('Employee Restrictions', function () {

    QUnit.test('canCreateAbsenceForResource - Employee can create for self', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 5;
        const isManager = false;
        const isAdmin = false;

        const result = canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.true(result, 'Employee should be able to create absence for themselves');
    });

    QUnit.test('canCreateAbsenceForResource - Employee cannot create for others', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = false;
        const isAdmin = false;

        const result = canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.false(result, 'Employee should NOT be able to create absence for other employees');
    });

    QUnit.test('canCreateAbsenceForResource - Manager can create for anyone', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = true;
        const isAdmin = false;

        const result = canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.true(result, 'Manager should be able to create absence for any employee');
    });

    QUnit.test('canCreateAbsenceForResource - Admin can create for anyone', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = false;
        const isAdmin = true;

        const result = canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.true(result, 'Admin should be able to create absence for any employee');
    });

    QUnit.test('canCreateAbsenceForResource - Approver can create for anyone', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = false;
        const isAdmin = false;
        const isApprover = true;

        const result = canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover);

        assert.true(result, 'Approver should be able to create absence for any employee');
    });

    QUnit.test('getResourceSelectionMessage - Employee selecting other employee', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = false;
        const isAdmin = false;

        const message = getResourceSelectionMessage(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.ok(message.includes('only create'), 'Should return message about only creating for self');
        assert.ok(message.includes('yourself'), 'Message should mention creating for yourself');
    });

    QUnit.test('getResourceSelectionMessage - Employee selecting self', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 5;
        const isManager = false;
        const isAdmin = false;

        const message = getResourceSelectionMessage(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.strictEqual(message, null, 'Should return null when employee selects themselves');
    });

    QUnit.test('getResourceSelectionMessage - Manager can select anyone', function (assert) {
        const currentEmployeeId = 5;
        const targetResourceId = 3;
        const isManager = true;
        const isAdmin = false;

        const message = getResourceSelectionMessage(currentEmployeeId, targetResourceId, isManager, isAdmin);

        assert.strictEqual(message, null, 'Manager should have no restriction message');
    });

});
