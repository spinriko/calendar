/**
 * Calendar Functions Module
 * 
 * Pure functions extracted from Absences.cshtml for testing and reusability.
 * These functions handle status colors, URL building, and view updates.
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
    updateViewButtons
};
