/**
 * Calendar Functions Module
 * 
 * Pure functions extracted from Absences.cshtml for testing and reusability.
 * These functions handle status colors, URL building, role determination,
 * filter management, and permission validation for the absence tracking calendar.
 * 
 * @module calendar-functions
 */

function getStatusColor(status) {
    switch (status) {
        case "Pending": return "#ffa500cc";
        case "Approved": return "#6aa84fcc";
        case "Rejected": return "#cc4125cc";
        case "Cancelled": return "#999999cc";
        default: return "#2e78d6cc";
    }
}

function buildAbsencesUrl(baseUrl, selectedStatuses, isManager, isAdmin, currentEmployeeId) {
    let url = baseUrl;
    selectedStatuses.forEach(status => {
        url += `&status[]=${status}`;
    });
    const isEmployee = !isManager && !isAdmin;
    if (isEmployee) {
        const onlyApproved = selectedStatuses.length === 1 &&
            selectedStatuses[0] === "Approved";
        if (!onlyApproved) {
            url += `&employeeId=${currentEmployeeId}`;
        }
    }
    return url;
}

function determineUserRole(user) {
    if (!user || !user.roles) return "Employee";
    if (user.roles.includes("Admin")) return "Admin";
    if (user.roles.includes("Manager")) return "Manager";
    if (user.roles.includes("Approver")) return "Approver";
    return "Employee";
}

function getDefaultStatusFilters(role) {
    switch (role) {
        case "Admin":
            return ["Pending", "Approved", "Rejected", "Cancelled"];
        case "Manager":
        case "Approver":
            return ["Pending", "Approved"];
        case "Employee":
        default:
            return ["Pending"];
    }
}

function getVisibleFilters(role) {
    switch (role) {
        case "Admin":
        case "Employee":
            return ["Pending", "Approved", "Rejected", "Cancelled"];
        case "Manager":
        case "Approver":
            return ["Pending", "Approved"];
        default:
            return ["Pending", "Approved"];
    }
}

function updateSelectedStatusesFromCheckboxes(filterElements) {
    const statuses = [];
    if (filterElements.filterPending.checked) statuses.push("Pending");
    if (filterElements.filterApproved.checked) statuses.push("Approved");
    if (filterElements.filterRejected.checked) statuses.push("Rejected");
    if (filterElements.filterCancelled.checked) statuses.push("Cancelled");
    return statuses;
}

function isUserManagerOrApprover(user) {
    if (!user) return false;
    return user.isApprover ||
        user.roles?.some(r =>
            r.toLowerCase() === 'manager' ||
            r.toLowerCase() === 'approver'
        );
}

function canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    if (isAdmin || isManager || isApprover) {
        return true;
    }
    // Use loose equality to handle string/number differences
    return currentEmployeeId == targetResourceId;
}

function getResourceSelectionMessage(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    if (canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover)) {
        return null;
    }
    return "You can only create absence requests for yourself. Please select your own row in the calendar.";
}

function buildContextMenuItems(absence, userContext, event) {
    const items = [];
    const status = absence.status;
    const context = userContext || {
        currentEmployeeId: null,
        isAdmin: false,
        isManager: false,
        isApprover: false
    };
    const isAdmin = context.isAdmin;
    const isManager = context.isManager;
    const isApprover = context.isApprover;
    const isOwner = String(absence.employeeId) === String(context.currentEmployeeId);
    const canApprove = isAdmin || isManager || isApprover;
    const canEdit = isAdmin || isOwner;
    const canDelete = isAdmin || isOwner;
    items.push({
        text: "View Details",
        onClick: function () {
            return { action: 'viewDetails', absence };
        }
    });
    if (status === "Pending" && canEdit) {
        items.push({
            text: "Edit Reason",
            onClick: function () {
                return { action: 'editReason', absence };
            }
        });
    }
    if (status === "Pending" && canApprove) {
        if (canEdit) {
            items.push({ text: "-" });
        }
        items.push({
            text: "Approve",
            onClick: function () {
                return { action: 'approve', absence };
            }
        });
        items.push({
            text: "Reject",
            onClick: function () {
                return { action: 'reject', absence };
            }
        });
    }
    if ((status === "Pending" || status === "Cancelled") && canDelete) {
        if (canEdit || canApprove) {
            items.push({ text: "-" });
        }
        items.push({
            text: "Delete",
            onClick: function () {
                return { action: 'delete', absence };
            }
        });
    }
    return items;
}

function getSchedulerRowColor(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    if (!canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover)) {
        return "#eeeeee";
    }
    return null; // Default color
}

function shouldAllowSelection(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    return canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover);
}

function getCellCssClass(cellStart, today, currentEmployeeId, resourceId, isManager, isAdmin, isApprover) {
    // Check for past dates
    if (cellStart < today) {
        return "disabled-row";
    }
    // Check for permissions
    if (!canCreateAbsenceForResource(currentEmployeeId, resourceId, isManager, isAdmin, isApprover)) {
        return "disabled-row";
    }
    return null;
}

function updateViewButtons(buttons, activeView) {
    buttons.forEach(btn => {
        btn.style.fontWeight = 'normal';
        btn.style.backgroundColor = '';
    });
    const activeBtn = buttons.find(b => b.id === `view${activeView}`);
    if (activeBtn) {
        activeBtn.style.fontWeight = 'bold';
        activeBtn.style.backgroundColor = '#ddd';
    }
}

export {
    getStatusColor,
    buildAbsencesUrl,
    determineUserRole,
    getDefaultStatusFilters,
    getVisibleFilters,
    updateSelectedStatusesFromCheckboxes,
    isUserManagerOrApprover,
    canCreateAbsenceForResource,
    getResourceSelectionMessage,
    buildContextMenuItems,
    getSchedulerRowColor,
    shouldAllowSelection,
    getCellCssClass,
    updateViewButtons
};
