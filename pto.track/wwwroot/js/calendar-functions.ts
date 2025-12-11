/**
 * Calendar Functions Module
 * 
 * Pure functions extracted from Absences.cshtml for testing and reusability.
 * These functions handle status colors, URL building, and view updates.
 * 
 * @module calendar-functions
 */

function getStatusColor(status: string | null | undefined) {
    switch (status) {
        case "Pending": return "#ffa500cc";
        case "Approved": return "#6aa84fcc";
        case "Rejected": return "#cc4125cc";
        case "Cancelled": return "#999999cc";
        default: return "#2e78d6cc";
    }
}

function buildAbsencesUrl(baseUrl: string, selectedStatuses: string[], isManager: boolean, isAdmin: boolean, currentEmployeeId: any) {
    let url = baseUrl;
    selectedStatuses.forEach((status: string) => {
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

type ViewButton = { id?: string; style?: { fontWeight?: string; backgroundColor?: string } };

function updateViewButtons(buttons: ArrayLike<any>, activeView: string) {
    const arr = Array.from(buttons as any[]);
    arr.forEach((btn: any) => {
        const style = btn.style || (btn instanceof HTMLElement ? btn.style : undefined);
        if (style) {
            style.fontWeight = 'normal';
            style.backgroundColor = '';
        }
    });
    const activeBtn = arr.find((b: any) => (b.id === `view${activeView}`) || (b.getAttribute && b.getAttribute('id') === `view${activeView}`));
    if (activeBtn) {
        const style = activeBtn.style || (activeBtn instanceof HTMLElement ? activeBtn.style : undefined);
        if (style) {
            style.fontWeight = 'bold';
            style.backgroundColor = '#ddd';
        }
    }
}

export {
    getStatusColor,
    buildAbsencesUrl,
    updateViewButtons
};
