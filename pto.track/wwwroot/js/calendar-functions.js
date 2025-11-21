/**
 * Calendar Functions Module
 * 
 * Pure functions extracted from Absences.cshtml for testing and reusability.
 * These functions handle status colors, URL building, role determination,
 * filter management, and permission validation for the absence tracking calendar.
 * 
 * @module calendar-functions
 */

/**
 * Returns the color code for a given absence request status.
 * Colors are semi-transparent (alpha channel 'cc' = 80% opacity) for visual layering.
 * 
 * @param {string} status - The absence request status ("Pending", "Approved", "Rejected", "Cancelled")
 * @returns {string} Hex color code with alpha channel (e.g., "#ffa500cc")
 * 
 * @example
 * getStatusColor("Pending")   // returns "#ffa500cc" (orange)
 * getStatusColor("Approved")  // returns "#6aa84fcc" (green)
 * getStatusColor("Unknown")   // returns "#2e78d6cc" (blue, default)
 */
function getStatusColor(status) {
    switch (status) {
        case "Pending": return "#ffa500cc";    // Orange
        case "Approved": return "#6aa84fcc";   // Green
        case "Rejected": return "#cc4125cc";   // Red
        case "Cancelled": return "#999999cc";  // Gray
        default: return "#2e78d6cc";           // Blue (fallback)
    }
}

/**
 * Builds the API URL for fetching absence requests with appropriate filters.
 * Applies role-based filtering: employees see only their own requests,
 * while managers and admins see all requests matching the selected statuses.
 * 
 * Uses array notation (status[]) for ASP.NET Core model binding to List<string>.
 * 
 * @param {string} baseUrl - The base API endpoint URL (e.g., "/api/absences?start=2025-01-01&end=2025-01-31")
 * @param {string[]} selectedStatuses - Array of status values to filter by (e.g., ["Pending", "Approved"])
 * @param {boolean} isManager - Whether the current user is a manager
 * @param {boolean} isAdmin - Whether the current user is an admin
 * @param {number} currentEmployeeId - The ID of the current logged-in employee
 * @returns {string} Complete URL with query parameters
 * 
 * @example
 * // Manager viewing pending and approved requests
 * buildAbsencesUrl("/api/absences?start=2025-01-01&end=2025-01-31", 
 *                  ["Pending", "Approved"], true, false, 123)
 * // returns "/api/absences?start=2025-01-01&end=2025-01-31&status[]=Pending&status[]=Approved"
 * 
 * @example
 * // Employee viewing their own pending requests
 * buildAbsencesUrl("/api/absences?start=2025-01-01&end=2025-01-31", 
 *                  ["Pending"], false, false, 456)
 * // returns "/api/absences?start=2025-01-01&end=2025-01-31&status[]=Pending&employeeId=456"
 */
function buildAbsencesUrl(baseUrl, selectedStatuses, isManager, isAdmin, currentEmployeeId) {
    let url = baseUrl;

    // Add all selected statuses to the query (using array notation for ASP.NET Core)
    selectedStatuses.forEach(status => {
        url += `&status[]=${status}`;
    });

    // Employees have conditional filtering based on status selection
    const isEmployee = !isManager && !isAdmin;
    if (isEmployee) {
        // Check if viewing ONLY approved absences
        const onlyApproved = selectedStatuses.length === 1 &&
            selectedStatuses[0] === "Approved";

        // Only filter by employeeId when viewing non-approved statuses
        // This allows employees to see everyone's approved absences
        if (!onlyApproved) {
            url += `&employeeId=${currentEmployeeId}`;
        }
    }

    return url;
}

/**
 * Determines the primary role of a user based on their role collection.
 * Role precedence: Admin > Manager > Approver > Employee.
 * 
 * @param {Object} user - The user object containing role information
 * @param {string[]} [user.roles] - Array of role names assigned to the user
 * @returns {string} The primary role ("Admin", "Manager", "Approver", or "Employee")
 * 
 * @example
 * determineUserRole({ roles: ["Admin", "Manager"] })  // returns "Admin"
 * determineUserRole({ roles: ["Approver"] })          // returns "Approver"
 * determineUserRole({ roles: [] })                    // returns "Employee"
 * determineUserRole(null)                             // returns "Employee"
 */
function determineUserRole(user) {
    if (!user || !user.roles) return "Employee";

    if (user.roles.includes("Admin")) return "Admin";
    if (user.roles.includes("Manager")) return "Manager";
    if (user.roles.includes("Approver")) return "Approver";
    return "Employee";
}

/**
 * Returns the default status filters that should be selected when the calendar loads.
 * Different roles see different default views to optimize their workflow:
 * - Admins see all statuses (full visibility)
 * - Managers/Approvers see Pending and Approved (actionable items)
 * - Employees see only Pending (their active requests)
 * 
 * @param {string} role - The user's primary role ("Admin", "Manager", "Approver", or "Employee")
 * @returns {string[]} Array of default status filters for the role
 * 
 * @example
 * getDefaultStatusFilters("Admin")     // returns ["Pending", "Approved", "Rejected", "Cancelled"]
 * getDefaultStatusFilters("Manager")   // returns ["Pending", "Approved"]
 * getDefaultStatusFilters("Employee")  // returns ["Pending"]
 */
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

/**
 * Returns which status filter checkboxes should be visible in the UI for a given role.
 * Controls UI visibility based on role permissions:
 * - Admins and Employees see all 4 checkboxes (different default selections)
 * - Managers and Approvers see only Pending and Approved (hide Rejected/Cancelled)
 * 
 * @param {string} role - The user's primary role ("Admin", "Manager", "Approver", or "Employee")
 * @returns {string[]} Array of status values that should have visible checkboxes
 * 
 * @example
 * getVisibleFilters("Admin")      // returns ["Pending", "Approved", "Rejected", "Cancelled"]
 * getVisibleFilters("Manager")    // returns ["Pending", "Approved"]
 * getVisibleFilters("Employee")   // returns ["Pending", "Approved", "Rejected", "Cancelled"]
 */
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

/**
 * Reads the current state of status filter checkboxes and returns selected statuses.
 * Scans all four checkbox elements and builds an array of checked status values.
 * 
 * @param {Object} filterElements - Object containing checkbox DOM elements
 * @param {HTMLInputElement} filterElements.filterPending - The "Pending" status checkbox
 * @param {HTMLInputElement} filterElements.filterApproved - The "Approved" status checkbox
 * @param {HTMLInputElement} filterElements.filterRejected - The "Rejected" status checkbox
 * @param {HTMLInputElement} filterElements.filterCancelled - The "Cancelled" status checkbox
 * @returns {string[]} Array of status values for checked checkboxes
 * 
 * @example
 * const elements = {
 *   filterPending: document.getElementById("filterPending"),
 *   filterApproved: document.getElementById("filterApproved"),
 *   filterRejected: document.getElementById("filterRejected"),
 *   filterCancelled: document.getElementById("filterCancelled")
 * };
 * updateSelectedStatusesFromCheckboxes(elements)  // returns ["Pending", "Approved"] if those are checked
 */
function updateSelectedStatusesFromCheckboxes(filterElements) {
    const statuses = [];
    if (filterElements.filterPending.checked) statuses.push("Pending");
    if (filterElements.filterApproved.checked) statuses.push("Approved");
    if (filterElements.filterRejected.checked) statuses.push("Rejected");
    if (filterElements.filterCancelled.checked) statuses.push("Cancelled");
    return statuses;
}

/**
 * Checks if a user has manager or approver privileges.
 * Used to determine if user can approve/reject absence requests.
 * 
 * Checks both the isApprover flag and the roles array for flexibility
 * in how user permissions are stored.
 * 
 * @param {Object} user - The user object to check
 * @param {boolean} [user.isApprover] - Direct approver flag
 * @param {string[]} [user.roles] - Array of role names (case-insensitive check)
 * @returns {boolean} True if user is a manager or approver, false otherwise
 * 
 * @example
 * isUserManagerOrApprover({ isApprover: true })                    // returns true
 * isUserManagerOrApprover({ roles: ["Manager"] })                  // returns true
 * isUserManagerOrApprover({ roles: ["manager", "Employee"] })      // returns true (case-insensitive)
 * isUserManagerOrApprover({ roles: ["Employee"] })                 // returns false
 * isUserManagerOrApprover(null)                                    // returns false
 */
function isUserManagerOrApprover(user) {
    if (!user) return false;

    return user.isApprover ||
        user.roles?.some(r =>
            r.toLowerCase() === 'manager' ||
            r.toLowerCase() === 'approver'
        );
}

/**
 * Determines if the current user can create an absence request for the target resource (employee).
 * 
 * Permission rules:
 * - Admins can create absences for anyone (full system access)
 * - Managers can create absences for anyone (team management)
 * - Approvers can create absences for anyone (workflow authority)
 * - Regular employees can only create absences for themselves (self-service)
 * 
 * This function enforces role-based access control (RBAC) for absence creation.
 * 
 * @param {number} currentEmployeeId - The ID of the current logged-in employee
 * @param {number} targetResourceId - The ID of the resource/employee being selected on the calendar
 * @param {boolean} isManager - Whether the current user has manager role
 * @param {boolean} isAdmin - Whether the current user has admin role
 * @param {boolean} [isApprover=false] - Whether the current user has approver role (optional)
 * @returns {boolean} True if the user can create an absence for the target resource, false otherwise
 * 
 * @example
 * // Admin creating absence for employee 789
 * canCreateAbsenceForResource(123, 789, false, true, false)  // returns true
 * 
 * @example
 * // Employee trying to create absence for another employee
 * canCreateAbsenceForResource(123, 456, false, false, false)  // returns false
 * 
 * @example
 * // Employee creating absence for themselves
 * canCreateAbsenceForResource(123, 123, false, false, false)  // returns true
 * 
 * @example
 * // Manager creating absence for any employee
 * canCreateAbsenceForResource(123, 789, true, false, false)  // returns true
 */
function canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    // Admins, Managers, and Approvers can create absences for anyone
    if (isAdmin || isManager || isApprover) {
        return true;
    }

    // Regular employees can only create absences for themselves
    return currentEmployeeId === targetResourceId;
}

/**
 * Gets an appropriate error message if the user cannot create an absence for the selected resource.
 * Returns null if the user has permission, allowing the absence creation to proceed.
 * 
 * This function provides user feedback for RBAC violations, helping employees understand
 * why they cannot create absences for other employees.
 * 
 * @param {number} currentEmployeeId - The ID of the current logged-in employee
 * @param {number} targetResourceId - The ID of the resource/employee being selected on the calendar
 * @param {boolean} isManager - Whether the current user has manager role
 * @param {boolean} isAdmin - Whether the current user has admin role
 * @param {boolean} [isApprover=false] - Whether the current user has approver role (optional)
 * @returns {string|null} Error message string if permission denied, null if operation allowed
 * 
 * @example
 * // Employee trying to create absence for another employee
 * getResourceSelectionMessage(123, 456, false, false, false)
 * // returns "You can only create absence requests for yourself. Please select your own row in the calendar."
 * 
 * @example
 * // Employee creating absence for themselves
 * getResourceSelectionMessage(123, 123, false, false, false)  // returns null
 * 
 * @example
 * // Manager creating absence for any employee
 * getResourceSelectionMessage(123, 789, true, false, false)  // returns null
 */
function getResourceSelectionMessage(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover = false) {
    if (canCreateAbsenceForResource(currentEmployeeId, targetResourceId, isManager, isAdmin, isApprover)) {
        return null;
    }

    return "You can only create absence requests for yourself. Please select your own row in the calendar.";
}
