import { buildContextMenuItems } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";

describe('buildContextMenuItems', () => {
    describe('Pending status', () => {
        it('returns all menu items for pending status (manager, owner)', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(true);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(true);
        });

        it('returns menu items for pending status (non-owner employee)', () => {
            const absence = { status: 'Pending', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Approve')).toBe(false);
            expect(items.some(i => i.text === 'Reject')).toBe(false);
            expect(items.some(i => i.text === 'Delete')).toBe(false);
        });

        it('returns menu items for pending status (approver, not owner)', () => {
            const absence = { status: 'Pending', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: true };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(false);
        });

        it('returns menu items for pending status (manager, not owner)', () => {
            const absence = { status: 'Pending', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Edit Reason')).toBe(false);
            expect(items.some(i => i.text === 'Approve')).toBe(true);
            expect(items.some(i => i.text === 'Reject')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(false);
        });
    });

    describe('Approved status', () => {
        it('returns only View Details for approved status (admin)', () => {
            const absence = { status: 'Approved', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('returns only View Details for approved status (manager)', () => {
            const absence = { status: 'Approved', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('returns only View Details for approved status (employee-owner)', () => {
            const absence = { status: 'Approved', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('returns only View Details for approved status (employee-non-owner)', () => {
            const absence = { status: 'Approved', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });
    });

    describe('Rejected status', () => {
        it('returns only View Details for rejected status (admin)', () => {
            const absence = { status: 'Rejected', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('returns only View Details for rejected status (manager)', () => {
            const absence = { status: 'Rejected', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('returns only View Details for rejected status (employee-owner)', () => {
            const absence = { status: 'Rejected', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });
    });

    describe('Cancelled status', () => {
        it('returns View Details and Delete for cancelled status (admin, owner)', () => {
            const absence = { status: 'Cancelled', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === 'View Details')).toBe(true);
            expect(items.some(i => i.text === 'Delete')).toBe(true);
        });

        it('returns only View Details for cancelled status (non-owner)', () => {
            const absence = { status: 'Cancelled', employeeId: '2' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });
    });

    describe('Separator logic', () => {
        it('includes separator when edit and approve are both available', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items.some(i => i.text === '-')).toBe(true);
        });

        it('includes separator before delete when other actions exist', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const separatorCount = items.filter(i => i.text === '-').length;
            expect(separatorCount).toBeGreaterThan(0);
        });
    });

    describe('No user context', () => {
        it('uses default context when userContext is not provided', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const items = buildContextMenuItems(absence, null, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });

        it('uses default context when userContext is undefined', () => {
            const absence = { status: 'Approved', employeeId: '1' };
            const items = buildContextMenuItems(absence, undefined, {});
            expect(items.length).toBe(1);
            expect(items[0].text).toBe('View Details');
        });
    });

    describe('onClick action validation', () => {
        it('returns correct action for View Details', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const viewItem = items.find(i => i.text === 'View Details');
            const result = viewItem.onClick();
            expect(result.action).toBe('viewDetails');
            expect(result.absence).toBe(absence);
        });

        it('returns correct action for Edit Reason', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const editItem = items.find(i => i.text === 'Edit Reason');
            const result = editItem.onClick();
            expect(result.action).toBe('editReason');
        });

        it('returns correct action for Approve', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const approveItem = items.find(i => i.text === 'Approve');
            const result = approveItem.onClick();
            expect(result.action).toBe('approve');
        });

        it('returns correct action for Reject', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const rejectItem = items.find(i => i.text === 'Reject');
            const result = rejectItem.onClick();
            expect(result.action).toBe('reject');
        });

        it('returns correct action for Delete', () => {
            const absence = { status: 'Cancelled', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const deleteItem = items.find(i => i.text === 'Delete');
            const result = deleteItem.onClick();
            expect(result.action).toBe('delete');
        });
    });

    describe('Menu item order validation', () => {
        it('maintains correct order: View Details first', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: false, isManager: true, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            expect(items[0].text).toBe('View Details');
        });

        it('maintains correct order for pending with all actions', () => {
            const absence = { status: 'Pending', employeeId: '1' };
            const userContext = { currentEmployeeId: '1', isAdmin: true, isManager: false, isApprover: false };
            const items = buildContextMenuItems(absence, userContext, {});
            const textItems = items.filter(i => i.text !== '-').map(i => i.text);
            expect(textItems[0]).toBe('View Details');
            expect(textItems[1]).toBe('Edit Reason');
            expect(textItems).toContain('Approve');
            expect(textItems).toContain('Reject');
            expect(textItems[textItems.length - 1]).toBe('Delete');
        });
    });
});


