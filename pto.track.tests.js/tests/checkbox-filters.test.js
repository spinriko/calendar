QUnit.module('Checkbox Filters', function () {

    QUnit.test('updateSelectedStatusesFromCheckboxes returns all checked statuses', function (assert) {
        const mockElements = {
            filterPending: { checked: true },
            filterApproved: { checked: true },
            filterRejected: { checked: false },
            filterCancelled: { checked: false }
        };

        const result = updateSelectedStatusesFromCheckboxes(mockElements);

        assert.equal(result.length, 2, "Should return 2 statuses");
        assert.ok(result.includes("Pending"), "Should include Pending");
        assert.ok(result.includes("Approved"), "Should include Approved");
    });

    QUnit.test('updateSelectedStatusesFromCheckboxes returns empty array when none checked', function (assert) {
        const mockElements = {
            filterPending: { checked: false },
            filterApproved: { checked: false },
            filterRejected: { checked: false },
            filterCancelled: { checked: false }
        };

        const result = updateSelectedStatusesFromCheckboxes(mockElements);

        assert.equal(result.length, 0, "Should return empty array");
    });

    QUnit.test('updateSelectedStatusesFromCheckboxes returns all statuses when all checked', function (assert) {
        const mockElements = {
            filterPending: { checked: true },
            filterApproved: { checked: true },
            filterRejected: { checked: true },
            filterCancelled: { checked: true }
        };

        const result = updateSelectedStatusesFromCheckboxes(mockElements);

        assert.equal(result.length, 4, "Should return 4 statuses");
        assert.deepEqual(result, ["Pending", "Approved", "Rejected", "Cancelled"],
            "Should return all statuses in order");
    });

    QUnit.test('updateSelectedStatusesFromCheckboxes handles only Rejected checked', function (assert) {
        const mockElements = {
            filterPending: { checked: false },
            filterApproved: { checked: false },
            filterRejected: { checked: true },
            filterCancelled: { checked: false }
        };

        const result = updateSelectedStatusesFromCheckboxes(mockElements);

        assert.equal(result.length, 1, "Should return 1 status");
        assert.equal(result[0], "Rejected", "Should only include Rejected");
    });
});
