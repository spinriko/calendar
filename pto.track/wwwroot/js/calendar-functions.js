// Extracted calendar functions for testing
// These are the pure functions from Absences.cshtml

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

    // Add all selected statuses to the query
    selectedStatuses.forEach(status => {
        url += `&status=${status}`;
    });

    // For employees, only show their own requests
    if (!isManager && !isAdmin) {
        url += `&employeeId=${currentEmployeeId}`;
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
