const { buildContextMenuItems } = require('../pto.track/wwwroot/js/calendar-functions.js');

describe('Event Presentation - Context Menu', () => {
    const pendingAbsence = {
        id: '1', employeeId: 'emp-123', employeeName: 'John Doe', reason: 'Vacation', status: 'Pending', start: '2024-01-15', end: '2024-01-16', requestedDate: '2024-01-10'
    };
    const approvedAbsence = {
        id: '2', employeeId: 'emp-123', employeeName: 'Jane Smith', reason: 'Sick Leave', status: 'Approved', start: '2024-01-20', end: '2024-01-21', requestedDate: '2024-01-15', approverName: 'Manager Name', approvedDate: '2024-01-16'
    };
    const rejectedAbsence = {
        id: '3', employeeId: 'emp-456', employeeName: 'Bob Johnson', reason: 'Personal', status: 'Rejected', start: '2024-01-25', end: '2024-01-26', requestedDate: '2024-01-20', approverName: 'Manager Name', approvedDate: '2024-01-21', approvalComments: 'Insufficient notice'
    };
    const cancelledAbsence = {
        id: '4', employeeId: 'emp-123', employeeName: 'Alice Williams', reason: 'Conference', status: 'Cancelled', start: '2024-02-01', end: '2024-02-02', requestedDate: '2024-01-25'
    };

    it('View Details is available for all statuses', () => {
        const statuses = ['Pending', 'Approved', 'Rejected', 'Cancelled'];
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: false };
        statuses.forEach(status => {
            const absence = { ...pendingAbsence, status };
            const items = buildContextMenuItems(absence, userContext, {});
            const viewDetails = items.find(item => item.text === 'View Details');
            expect(viewDetails).toBeTruthy();
            expect(typeof viewDetails.onClick).toBe('function');
        });
    });

    it("Employee sees Edit and Delete for own Pending absence", () => {
        const userContext = { currentEmployeeId: 'emp-123', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'Edit Reason')).toBeTruthy();
        expect(items.find(item => item.text === 'Delete')).toBeTruthy();
        expect(items.find(item => item.text === 'Approve')).toBeFalsy();
        expect(items.find(item => item.text === 'Reject')).toBeFalsy();
    });

    it("Employee does NOT see Edit/Delete for other's Pending absence", () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'View Details')).toBeTruthy();
        expect(items.find(item => item.text === 'Edit Reason')).toBeFalsy();
        expect(items.find(item => item.text === 'Delete')).toBeFalsy();
        expect(items.find(item => item.text === 'Approve')).toBeFalsy();
        expect(items.find(item => item.text === 'Reject')).toBeFalsy();
    });

    it('Manager sees Approve/Reject for Pending absence', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: true, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'Approve')).toBeTruthy();
        expect(items.find(item => item.text === 'Reject')).toBeTruthy();
        expect(items.find(item => item.text === 'Edit Reason')).toBeFalsy();
    });

    it('Approver sees Approve/Reject for Pending absence', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: true };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'Approve')).toBeTruthy();
        expect(items.find(item => item.text === 'Reject')).toBeTruthy();
    });

    it('Admin sees all options for Pending absence', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: true, isManager: false, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'View Details')).toBeTruthy();
        expect(items.find(item => item.text === 'Edit Reason')).toBeTruthy();
        expect(items.find(item => item.text === 'Approve')).toBeTruthy();
        expect(items.find(item => item.text === 'Reject')).toBeTruthy();
        expect(items.find(item => item.text === 'Delete')).toBeTruthy();
    });

    it('Owner who is also Manager sees all options for own Pending absence', () => {
        const userContext = { currentEmployeeId: 'emp-123', isAdmin: false, isManager: true, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        expect(items.find(item => item.text === 'Edit Reason')).toBeTruthy();
        expect(items.find(item => item.text === 'Approve')).toBeTruthy();
        expect(items.find(item => item.text === 'Reject')).toBeTruthy();
        expect(items.find(item => item.text === 'Delete')).toBeTruthy();
        const editIndex = items.findIndex(item => item.text === 'Edit Reason');
        const approveIndex = items.findIndex(item => item.text === 'Approve');
        expect(editIndex).toBeLessThan(approveIndex);
        expect(items[editIndex + 1].text).toBe('-');
    });

    it('Employee sees Delete for own Cancelled absence', () => {
        const userContext = { currentEmployeeId: 'emp-123', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(cancelledAbsence, userContext, {});
        expect(items.find(item => item.text === 'View Details')).toBeTruthy();
        expect(items.find(item => item.text === 'Delete')).toBeTruthy();
        expect(items.find(item => item.text === 'Edit Reason')).toBeFalsy();
        expect(items.find(item => item.text === 'Approve')).toBeFalsy();
    });

    it("Employee does NOT see Delete for other's Cancelled absence", () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(cancelledAbsence, userContext, {});
        expect(items.find(item => item.text === 'View Details')).toBeTruthy();
        expect(items.find(item => item.text === 'Delete')).toBeFalsy();
    });

    it('Only View Details for Approved absence (all users)', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(approvedAbsence, userContext, {});
        expect(items.length).toBe(1);
        expect(items[0].text).toBe('View Details');
    });

    it('Only View Details for Rejected absence (all users)', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: false, isApprover: false };
        const items = buildContextMenuItems(rejectedAbsence, userContext, {});
        expect(items.length).toBe(1);
        expect(items[0].text).toBe('View Details');
    });

    it('Separators are correctly placed for Pending with Edit and Approve', () => {
        const userContext = { currentEmployeeId: 'emp-123', isAdmin: false, isManager: true, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        const texts = items.map(item => item.text);
        expect(texts).toEqual([
            'View Details',
            'Edit Reason',
            '-',
            'Approve',
            'Reject',
            '-',
            'Delete'
        ]);
    });

    it('Separators are correctly placed for Pending with Approve only (no Edit)', () => {
        const userContext = { currentEmployeeId: 'emp-999', isAdmin: false, isManager: true, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        const texts = items.map(item => item.text);
        expect(texts).toEqual([
            'View Details',
            'Approve',
            'Reject'
        ]);
    });

    it('All onClick handlers are functions', () => {
        const userContext = { currentEmployeeId: 'emp-123', isAdmin: true, isManager: false, isApprover: false };
        const items = buildContextMenuItems(pendingAbsence, userContext, {});
        items.forEach(item => {
            if (item.text !== '-') {
                expect(typeof item.onClick).toBe('function');
            }
        });
    });

    it('Backward compatibility - no userContext defaults to no permissions', () => {
        const items = buildContextMenuItems(pendingAbsence, null, {});
        expect(items.find(item => item.text === 'View Details')).toBeTruthy();
        expect(items.find(item => item.text === 'Edit Reason')).toBeFalsy();
        expect(items.find(item => item.text === 'Approve')).toBeFalsy();
        expect(items.find(item => item.text === 'Delete')).toBeFalsy();
    });
});
