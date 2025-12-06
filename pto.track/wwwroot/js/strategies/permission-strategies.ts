export interface IPermissionStrategy {
    canCreateFor(targetResourceId: number): boolean;
    canEdit(absence: any): boolean;
    canApprove(absence: any): boolean;
    canDelete(absence: any): boolean;
    getVisibleFilters(): string[];
    getDefaultFilters(): string[];
    getCellCssClass(cellStart: any, today: any, targetResourceId: number): string | null;
}

export class AdminStrategy implements IPermissionStrategy {
    constructor(private currentUserId: number) { }

    canCreateFor(targetResourceId: number): boolean {
        return true;
    }

    canEdit(absence: any): boolean {
        return true;
    }

    canApprove(absence: any): boolean {
        return absence.status === 'Pending';
    }

    canDelete(absence: any): boolean {
        return absence.status === 'Pending' || absence.status === 'Cancelled';
    }

    getVisibleFilters(): string[] {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }

    getDefaultFilters(): string[] {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }

    getCellCssClass(cellStart: any, today: any, targetResourceId: number): string | null {
        if (cellStart < today) return "disabled-row";
        return null;
    }
}

export class ManagerStrategy implements IPermissionStrategy {
    constructor(private currentUserId: number) { }

    canCreateFor(targetResourceId: number): boolean {
        return true;
    }

    canEdit(absence: any): boolean {
        // Managers can only edit their own pending requests
        return String(absence.employeeId) === String(this.currentUserId) && absence.status === 'Pending';
    }

    canApprove(absence: any): boolean {
        return absence.status === 'Pending';
    }

    canDelete(absence: any): boolean {
        // Managers can only delete their own pending/cancelled requests
        return String(absence.employeeId) === String(this.currentUserId) &&
            (absence.status === 'Pending' || absence.status === 'Cancelled');
    }

    getVisibleFilters(): string[] {
        return ["Pending", "Approved"];
    }

    getDefaultFilters(): string[] {
        return ["Pending", "Approved"];
    }

    getCellCssClass(cellStart: any, today: any, targetResourceId: number): string | null {
        if (cellStart < today) return "disabled-row";
        return null;
    }
}

export class EmployeeStrategy implements IPermissionStrategy {
    constructor(private currentUserId: number) { }

    canCreateFor(targetResourceId: number): boolean {
        return String(targetResourceId) === String(this.currentUserId);
    }

    canEdit(absence: any): boolean {
        return String(absence.employeeId) === String(this.currentUserId) && absence.status === 'Pending';
    }

    canApprove(absence: any): boolean {
        return false;
    }

    canDelete(absence: any): boolean {
        return String(absence.employeeId) === String(this.currentUserId) &&
            (absence.status === 'Pending' || absence.status === 'Cancelled');
    }

    getVisibleFilters(): string[] {
        return ["Pending", "Approved", "Rejected", "Cancelled"];
    }

    getDefaultFilters(): string[] {
        return ["Pending"];
    }

    getCellCssClass(cellStart: any, today: any, targetResourceId: number): string | null {
        if (cellStart < today) return "disabled-row";
        if (String(targetResourceId) !== String(this.currentUserId)) return "disabled-row";
        return null;
    }
}

export function createPermissionStrategy(user: any): IPermissionStrategy {
    const userId = user.id || 0;

    // Check for Admin
    if (user.roles?.some((r: string) => r.toLowerCase() === 'admin')) {
        return new AdminStrategy(userId);
    }

    // Check for Manager/Approver
    if (user.isApprover || user.roles?.some((r: string) => r.toLowerCase() === 'manager' || r.toLowerCase() === 'approver')) {
        return new ManagerStrategy(userId);
    }

    // Default to Employee
    return new EmployeeStrategy(userId);
}
