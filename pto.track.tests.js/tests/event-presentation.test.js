QUnit.module("Event Presentation - Context Menu", function () {

    const pendingAbsence = {
        id: "1",
        employeeId: "emp-123",
        employeeName: "John Doe",
        reason: "Vacation",
        status: "Pending",
        start: "2024-01-15",
        end: "2024-01-16",
        requestedDate: "2024-01-10"
    };

    const approvedAbsence = {
        id: "2",
        employeeId: "emp-123",
        employeeName: "Jane Smith",
        reason: "Sick Leave",
        status: "Approved",
        start: "2024-01-20",
        end: "2024-01-21",
        requestedDate: "2024-01-15",
        approverName: "Manager Name",
        approvedDate: "2024-01-16"
    };

    const rejectedAbsence = {
        id: "3",
        employeeId: "emp-456",
        employeeName: "Bob Johnson",
        reason: "Personal",
        status: "Rejected",
        start: "2024-01-25",
        end: "2024-01-26",
        requestedDate: "2024-01-20",
        approverName: "Manager Name",
        approvedDate: "2024-01-21",
        approvalComments: "Insufficient notice"
    };

    const cancelledAbsence = {
        id: "4",
        employeeId: "emp-123",
        employeeName: "Alice Williams",
        reason: "Conference",
        status: "Cancelled",
        start: "2024-02-01",
        end: "2024-02-02",
        requestedDate: "2024-01-25"
    };

    QUnit.test("View Details is available for all statuses", function (assert) {
        const statuses = ["Pending", "Approved", "Rejected", "Cancelled"];
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        statuses.forEach(status => {
            const absence = { ...pendingAbsence, status };
            const items = buildContextMenuItems(absence, userContext, {});
            const viewDetails = items.find(item => item.text === "View Details");

            assert.ok(viewDetails, `View Details available for ${status}`);
            assert.equal(typeof viewDetails.onClick, "function", `onClick is a function for ${status}`);
        });
    });

    QUnit.test("Employee sees Edit and Delete for own Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-123",
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "Edit Reason"), "Edit Reason available");
        assert.ok(items.find(item => item.text === "Delete"), "Delete available");
        assert.notOk(items.find(item => item.text === "Approve"), "Approve NOT available");
        assert.notOk(items.find(item => item.text === "Reject"), "Reject NOT available");
    });

    QUnit.test("Employee does NOT see Edit/Delete for other employee's Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999", // Different employee
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "View Details"), "View Details available");
        assert.notOk(items.find(item => item.text === "Edit Reason"), "Edit Reason NOT available");
        assert.notOk(items.find(item => item.text === "Delete"), "Delete NOT available");
        assert.notOk(items.find(item => item.text === "Approve"), "Approve NOT available");
        assert.notOk(items.find(item => item.text === "Reject"), "Reject NOT available");
    });

    QUnit.test("Manager sees Approve/Reject for Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: false,
            isManager: true,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "Approve"), "Approve available");
        assert.ok(items.find(item => item.text === "Reject"), "Reject available");
        assert.notOk(items.find(item => item.text === "Edit Reason"), "Edit Reason NOT available for other's absence");
    });

    QUnit.test("Approver sees Approve/Reject for Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: false,
            isManager: false,
            isApprover: true
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "Approve"), "Approve available");
        assert.ok(items.find(item => item.text === "Reject"), "Reject available");
    });

    QUnit.test("Admin sees all options for Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: true,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "View Details"), "View Details available");
        assert.ok(items.find(item => item.text === "Edit Reason"), "Edit Reason available");
        assert.ok(items.find(item => item.text === "Approve"), "Approve available");
        assert.ok(items.find(item => item.text === "Reject"), "Reject available");
        assert.ok(items.find(item => item.text === "Delete"), "Delete available");
    });

    QUnit.test("Owner who is also Manager sees all options for own Pending absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-123",
            isAdmin: false,
            isManager: true,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "Edit Reason"), "Edit Reason available");
        assert.ok(items.find(item => item.text === "Approve"), "Approve available");
        assert.ok(items.find(item => item.text === "Reject"), "Reject available");
        assert.ok(items.find(item => item.text === "Delete"), "Delete available");

        // Check for separator after Edit (since both Edit and Approve/Reject are present)
        const editIndex = items.findIndex(item => item.text === "Edit Reason");
        const approveIndex = items.findIndex(item => item.text === "Approve");
        assert.ok(editIndex < approveIndex, "Edit comes before Approve");
        assert.equal(items[editIndex + 1].text, "-", "Separator after Edit Reason");
    });

    QUnit.test("Employee sees Delete for own Cancelled absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-123",
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(cancelledAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "View Details"), "View Details available");
        assert.ok(items.find(item => item.text === "Delete"), "Delete available");
        assert.notOk(items.find(item => item.text === "Edit Reason"), "Edit Reason NOT available");
        assert.notOk(items.find(item => item.text === "Approve"), "Approve NOT available");
    });

    QUnit.test("Employee does NOT see Delete for other's Cancelled absence", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999", // Different employee
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(cancelledAbsence, userContext, {});

        assert.ok(items.find(item => item.text === "View Details"), "View Details available");
        assert.notOk(items.find(item => item.text === "Delete"), "Delete NOT available");
    });

    QUnit.test("Only View Details for Approved absence (all users)", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(approvedAbsence, userContext, {});

        assert.equal(items.length, 1, "Only one menu item");
        assert.equal(items[0].text, "View Details", "Only View Details available");
    });

    QUnit.test("Only View Details for Rejected absence (all users)", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999",
            isAdmin: false,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(rejectedAbsence, userContext, {});

        assert.equal(items.length, 1, "Only one menu item");
        assert.equal(items[0].text, "View Details", "Only View Details available");
    });

    QUnit.test("Separators are correctly placed for Pending with Edit and Approve", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-123",
            isAdmin: false,
            isManager: true,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        const texts = items.map(item => item.text);
        assert.deepEqual(texts, [
            "View Details",
            "Edit Reason",
            "-",
            "Approve",
            "Reject",
            "-",
            "Delete"
        ], "Separator after Edit and before Delete");
    });

    QUnit.test("Separators are correctly placed for Pending with Approve only (no Edit)", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-999", // Not the owner
            isAdmin: false,
            isManager: true,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        const texts = items.map(item => item.text);
        assert.deepEqual(texts, [
            "View Details",
            "Approve",
            "Reject"
        ], "No Edit, no Delete, no separators for non-owner Manager");
    });

    QUnit.test("All onClick handlers are functions", function (assert) {
        const userContext = {
            currentEmployeeId: "emp-123",
            isAdmin: true,
            isManager: false,
            isApprover: false
        };

        const items = buildContextMenuItems(pendingAbsence, userContext, {});

        items.forEach(item => {
            if (item.text !== "-") {
                assert.equal(typeof item.onClick, "function", `onClick is function for ${item.text}`);
            }
        });
    });

    QUnit.test("Backward compatibility - no userContext defaults to no permissions", function (assert) {
        const items = buildContextMenuItems(pendingAbsence, null, {});

        assert.ok(items.find(item => item.text === "View Details"), "View Details available");
        assert.notOk(items.find(item => item.text === "Edit Reason"), "Edit NOT available without context");
        assert.notOk(items.find(item => item.text === "Approve"), "Approve NOT available without context");
        assert.notOk(items.find(item => item.text === "Delete"), "Delete NOT available without context");
    });
});
