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
 * Get the roles for a user based on their employee number
 * @param {string} employeeNumber - The employee number
 * @returns {string[]} Array of role names for that user
 */
export function getRolesForUser(employeeNumber) {
    // Map employee numbers to their roles based on the database/test data
    const userRoles = {
        'EMP001': ['Employee'], // Test Employee 1
        'EMP002': ['Employee'], // Test Employee 2
        'MGR001': ['Employee', 'Manager'], // Test Manager
        'APR001': ['Employee', 'Approver'], // Test Approver
        'ADMIN001': ['Employee', 'Admin'] // Administrator
    };
    return userRoles[employeeNumber] || ['Employee'];
}
/**
 * Get the impersonation data to save
 * @returns {{employeeNumber: string, roles: string[]}}
 */
export function getImpersonationData() {
    const employeeNumber = document.getElementById('impersonateUser')?.value || 'EMP001';
    const roles = getRolesForUser(employeeNumber);
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
    }
    else {
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
        }
        else {
            console.error('Failed to apply impersonation:', await response.text());
            alert('Failed to apply impersonation');
        }
    }
    catch (error) {
        console.error('Error applying impersonation:', error);
        alert('Error applying impersonation');
    }
}
/**
 * Load saved impersonation state from localStorage and update UI
 */
export function loadSavedImpersonation() {
    const saved = localStorage.getItem('impersonatedUser');
    if (!saved)
        return;
    try {
        const data = JSON.parse(saved);
        // Update user select
        const userSelect = document.getElementById('impersonateUser');
        if (userSelect && data.employeeNumber) {
            userSelect.value = data.employeeNumber;
        }
    }
    catch (error) {
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
}
// Auto-initialize on DOM content loaded
if (typeof document !== 'undefined') {
    document.addEventListener('DOMContentLoaded', initImpersonationPanel);
}
