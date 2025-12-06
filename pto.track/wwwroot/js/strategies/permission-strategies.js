export class AdminStrategy {
    constructor(currentUserId) {
        this.currentUserId = currentUserId;
    }
    canCreateFor(targetResourceId) {
        return true;
    }
    canEdit(absence) {
        return true;
    }
    canApprove(absence) {
        return absence.status === 'Pending';
    }
    canDelete(absence) {
        return absence.status === 'Pending' || absence.status === 'Cancelled';
    }
    getVisibleFilters() {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }
    getDefaultFilters() {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }
    getCellCssClass(cellStart, today, targetResourceId) {
        if (cellStart < today)
            return "disabled-row";
        return null;
    }
}
export class ManagerStrategy {
    constructor(currentUserId) {
        this.currentUserId = currentUserId;
    }
    canCreateFor(targetResourceId) {
        return true;
    }
    canEdit(absence) {
        // Managers can only edit their own pending requests
        return String(absence.employeeId) === String(this.currentUserId) && absence.status === 'Pending';
    }
    canApprove(absence) {
        return absence.status === 'Pending';
    }
    canDelete(absence) {
        // Managers can only delete their own pending/cancelled requests
        return String(absence.employeeId) === String(this.currentUserId) &&
            (absence.status === 'Pending' || absence.status === 'Cancelled');
    }
    getVisibleFilters() {
        return ["Pending", "Approved"];
    }
    getDefaultFilters() {
        return ["Pending", "Approved"];
    }
    getCellCssClass(cellStart, today, targetResourceId) {
        if (cellStart < today)
            return "disabled-row";
        return null;
    }
}
export class EmployeeStrategy {
    constructor(currentUserId) {
        this.currentUserId = currentUserId;
    }
    canCreateFor(targetResourceId) {
        return String(targetResourceId) === String(this.currentUserId);
    }
    canEdit(absence) {
        return String(absence.employeeId) === String(this.currentUserId) && absence.status === 'Pending';
    }
    canApprove(absence) {
        return false;
    }
    canDelete(absence) {
        return String(absence.employeeId) === String(this.currentUserId) &&
            (absence.status === 'Pending' || absence.status === 'Cancelled');
    }
    getVisibleFilters() {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }
    getDefaultFilters() {
        return ["Pending"];
    }
    getCellCssClass(cellStart, today, targetResourceId) {
        if (cellStart < today)
            return "disabled-row";
        if (String(targetResourceId) !== String(this.currentUserId))
            return "disabled-row";
        return null;
    }
}
export function createPermissionStrategy(user) {
    const userId = user.id || 0;
    // Check for Admin
    if (user.roles?.some((r) => r.toLowerCase() === 'admin')) {
        return new AdminStrategy(userId);
    }
    // Check for Manager/Approver
    if (user.isApprover || user.roles?.some((r) => r.toLowerCase() === 'manager' || r.toLowerCase() === 'approver')) {
        return new ManagerStrategy(userId);
    }
    // Default to Employee
    return new EmployeeStrategy(userId);
}
