/**
 * Impersonation Panel - Allows developers to test different user roles
 * @module impersonation-panel
 */

/**
 * Toggle the visibility of the impersonation panel
 */
export function toggleImpersonationPanel() {
    const panel = document.querySelector('.impersonation-panel');
    if (panel) {
        panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
    }
}

/**
 * Get the selected roles from the checkboxes
 * @returns {string[]} Array of selected role names
 */
export function getSelectedRoles() {
    const roles = [];

    if (document.getElementById('roleEmployee')?.checked) roles.push('Employee');
    if (document.getElementById('roleManager')?.checked) roles.push('Manager');
    if (document.getElementById('roleApprover')?.checked) roles.push('Approver');
    if (document.getElementById('roleAdmin')?.checked) roles.push('Admin');

    return roles;
}

/**
 * Get the impersonation data to save
 * @returns {{employeeNumber: string, roles: string[]}}
 */
export function getImpersonationData() {
    const employeeNumber = document.getElementById('impersonateUser')?.value || 'EMP001';
    const roles = getSelectedRoles();

    return {
        employeeNumber,
        roles
    };
}

/**
 * Reload the page (extracted for testability)
 * @param {Function} reloadFn - Optional reload function for testing
 */
export function reloadPage(reloadFn = null) {
    if (reloadFn) {
        reloadFn();
    } else {
        window.location.reload();
    }
}

/**
 * Apply impersonation by saving to server and reloading
 * @param {Function} reloadFn - Optional reload function for testing
 */
export async function applyImpersonation(reloadFn = null) {
    const data = getImpersonationData();

    try {
        const response = await fetch('/api/impersonation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            // Store in localStorage for UI state persistence
            localStorage.setItem('impersonatedUser', JSON.stringify(data));

            // Reload page to apply impersonation
            reloadPage(reloadFn);
        } else {
            console.error('Failed to apply impersonation:', await response.text());
            alert('Failed to apply impersonation');
        }
    } catch (error) {
        console.error('Error applying impersonation:', error);
        alert('Error applying impersonation');
    }
}

/**
 * Clear impersonation and return to default user
 * @param {Function} reloadFn - Optional reload function for testing
 */
export async function clearImpersonation(reloadFn = null) {
    try {
        const response = await fetch('/api/impersonation', {
            method: 'DELETE'
        });

        if (response.ok) {
            localStorage.removeItem('impersonatedUser');
            reloadPage(reloadFn);
        } else {
            console.error('Failed to clear impersonation:', await response.text());
            alert('Failed to clear impersonation');
        }
    } catch (error) {
        console.error('Error clearing impersonation:', error);
        alert('Error clearing impersonation');
    }
}

/**
 * Load saved impersonation state from localStorage and update UI
 */
export function loadSavedImpersonation() {
    const saved = localStorage.getItem('impersonatedUser');
    if (!saved) return;

    try {
        const data = JSON.parse(saved);

        // Update user select
        const userSelect = document.getElementById('impersonateUser');
        if (userSelect && data.employeeNumber) {
            userSelect.value = data.employeeNumber;
        }

        // Update role checkboxes
        if (data.roles && Array.isArray(data.roles)) {
            const roleEmployee = document.getElementById('roleEmployee');
            const roleManager = document.getElementById('roleManager');
            const roleApprover = document.getElementById('roleApprover');
            const roleAdmin = document.getElementById('roleAdmin');

            if (roleEmployee) roleEmployee.checked = data.roles.includes('Employee');
            if (roleManager) roleManager.checked = data.roles.includes('Manager');
            if (roleApprover) roleApprover.checked = data.roles.includes('Approver');
            if (roleAdmin) roleAdmin.checked = data.roles.includes('Admin');
        }
    } catch (error) {
        console.error('Error loading saved impersonation:', error);
    }
}

/**
 * Initialize the impersonation panel on page load
 */
export function initImpersonationPanel() {
    loadSavedImpersonation();

    // Make functions available globally for onclick handlers
    window.toggleImpersonationPanel = toggleImpersonationPanel;
    window.applyImpersonation = applyImpersonation;
    window.clearImpersonation = clearImpersonation;
}

// Auto-initialize on DOM content loaded
if (typeof document !== 'undefined') {
    document.addEventListener('DOMContentLoaded', initImpersonationPanel);
}
