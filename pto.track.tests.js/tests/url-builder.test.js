QUnit.module('URL Builder', function () {

    QUnit.test('buildAbsencesUrl adds status parameters for selected statuses', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending", "Approved"],
            true,
            false,
            1
        );

        assert.ok(url.includes("&status[]=Pending"), "URL should include Pending status with array notation");
        assert.ok(url.includes("&status[]=Approved"), "URL should include Approved status with array notation");
    });

    QUnit.test('buildAbsencesUrl adds employeeId for regular employees viewing non-approved', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending"],
            false,
            false,
            5
        );

        assert.ok(url.includes("&employeeId=5"), "URL should include employeeId for non-manager viewing Pending");
    });

    QUnit.test('buildAbsencesUrl does NOT add employeeId for employees viewing only Approved', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Approved"],
            false,
            false,
            5
        );

        assert.notOk(url.includes("employeeId"), "URL should NOT include employeeId for employee viewing only Approved");
    });

    QUnit.test('buildAbsencesUrl does not add employeeId for managers', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending"],
            true,
            false,
            5
        );

        assert.notOk(url.includes("employeeId"), "URL should not include employeeId for managers");
    });

    QUnit.test('buildAbsencesUrl does not add employeeId for admins', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending"],
            false,
            true,
            5
        );

        assert.notOk(url.includes("employeeId"), "URL should not include employeeId for admins");
    });

    QUnit.test('buildAbsencesUrl handles multiple status filters', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            ["Pending", "Approved", "Rejected", "Cancelled"],
            true,
            false,
            1
        );

        assert.ok(url.includes("&status[]=Pending"), "URL should include Pending with array notation");
        assert.ok(url.includes("&status[]=Approved"), "URL should include Approved with array notation");
        assert.ok(url.includes("&status[]=Rejected"), "URL should include Rejected with array notation");
        assert.ok(url.includes("&status[]=Cancelled"), "URL should include Cancelled with array notation");
    });

    QUnit.test('buildAbsencesUrl handles empty status array', function (assert) {
        const url = buildAbsencesUrl(
            "/api/absences?start=2025-11-01&end=2025-11-30",
            [],
            true,
            false,
            1
        );

        assert.notOk(url.includes("status"), "URL should not include any status parameters");
    });
});
