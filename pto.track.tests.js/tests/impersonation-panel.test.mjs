/**
 * @jest-environment jsdom
 */

import { jest } from '@jest/globals';
import {
    toggleImpersonationPanel,
    getSelectedRoles,
    getImpersonationData,
    applyImpersonation,
    clearImpersonation,
    loadSavedImpersonation,
    initImpersonationPanel
} from '../../pto.track/wwwroot/js/impersonation-panel.mjs';

describe('Impersonation Panel', () => {
    let mockPanel;

    beforeEach(() => {
        // Set up DOM
        document.body.innerHTML = `
            <div class="impersonation-panel" style="display: block;"></div>
            <select id="impersonateUser">
                <option value="EMP001">Development User</option>
                <option value="EMP002">Test Employee</option>
                <option value="EMP003">Test Manager</option>
            </select>
            <input type="checkbox" id="roleEmployee" checked>
            <input type="checkbox" id="roleManager">
            <input type="checkbox" id="roleApprover">
            <input type="checkbox" id="roleAdmin">
        `;

        mockPanel = document.querySelector('.impersonation-panel');

        // Clear localStorage
        localStorage.clear();
    });

    afterEach(() => {
        localStorage.clear();
    });

    describe('toggleImpersonationPanel', () => {
        test('hides panel when visible', () => {
            mockPanel.style.display = 'block';
            toggleImpersonationPanel();
            expect(mockPanel.style.display).toBe('none');
        });

        test('shows panel when hidden', () => {
            mockPanel.style.display = 'none';
            toggleImpersonationPanel();
            expect(mockPanel.style.display).toBe('block');
        });

        test('handles missing panel gracefully', () => {
            document.body.innerHTML = '';
            expect(() => toggleImpersonationPanel()).not.toThrow();
        });
    });

    describe('getSelectedRoles', () => {
        test('returns checked roles', () => {
            document.getElementById('roleEmployee').checked = true;
            document.getElementById('roleManager').checked = false;
            document.getElementById('roleApprover').checked = true;
            document.getElementById('roleAdmin').checked = false;

            const roles = getSelectedRoles();
            expect(roles).toEqual(['Employee', 'Approver']);
        });

        test('returns empty array when no roles selected', () => {
            document.getElementById('roleEmployee').checked = false;
            document.getElementById('roleManager').checked = false;
            document.getElementById('roleApprover').checked = false;
            document.getElementById('roleAdmin').checked = false;

            const roles = getSelectedRoles();
            expect(roles).toEqual([]);
        });

        test('returns all roles when all selected', () => {
            document.getElementById('roleEmployee').checked = true;
            document.getElementById('roleManager').checked = true;
            document.getElementById('roleApprover').checked = true;
            document.getElementById('roleAdmin').checked = true;

            const roles = getSelectedRoles();
            expect(roles).toEqual(['Employee', 'Manager', 'Approver', 'Admin']);
        });

        test('handles missing checkboxes gracefully', () => {
            document.body.innerHTML = '';
            const roles = getSelectedRoles();
            expect(roles).toEqual([]);
        });
    });

    describe('getImpersonationData', () => {
        test('returns employee number and selected roles', () => {
            document.getElementById('impersonateUser').value = 'EMP002';
            document.getElementById('roleManager').checked = true;

            const data = getImpersonationData();
            expect(data.employeeNumber).toBe('EMP002');
            expect(data.roles).toContain('Employee');
            expect(data.roles).toContain('Manager');
        });

        test('defaults to EMP001 when select missing', () => {
            document.body.innerHTML = `
                <input type="checkbox" id="roleEmployee" checked>
            `;

            const data = getImpersonationData();
            expect(data.employeeNumber).toBe('EMP001');
        });
    });

    describe('applyImpersonation', () => {
        test('saves to server and localStorage, then calls reload', async () => {
            const reloadSpy = jest.fn();
            global.fetch = jest.fn(() =>
                Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve({ message: 'Impersonation applied' })
                })
            );

            document.getElementById('impersonateUser').value = 'EMP003';
            document.getElementById('roleManager').checked = true;
            document.getElementById('roleApprover').checked = true;

            await applyImpersonation(reloadSpy);

            expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    employeeNumber: 'EMP003',
                    roles: ['Employee', 'Manager', 'Approver']
                })
            });

            const saved = JSON.parse(localStorage.getItem('impersonatedUser'));
            expect(saved.employeeNumber).toBe('EMP003');
            expect(saved.roles).toContain('Employee');
            expect(saved.roles).toContain('Manager');
            expect(saved.roles).toContain('Approver');
            expect(reloadSpy).toHaveBeenCalled();
        });

        test('handles API error gracefully', async () => {
            const reloadSpy = jest.fn();
            global.fetch = jest.fn(() =>
                Promise.resolve({
                    ok: false,
                    text: () => Promise.resolve('Error message')
                })
            );
            global.alert = jest.fn();
            const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

            await applyImpersonation(reloadSpy);

            expect(reloadSpy).not.toHaveBeenCalled();
            expect(global.alert).toHaveBeenCalledWith('Failed to apply impersonation');

            consoleErrorSpy.mockRestore();
        });
    });

    describe('clearImpersonation', () => {
        test('clears server and localStorage, then calls reload', async () => {
            const reloadSpy = jest.fn();
            global.fetch = jest.fn(() =>
                Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve({ message: 'Impersonation cleared' })
                })
            );

            localStorage.setItem('impersonatedUser', JSON.stringify({
                employeeNumber: 'EMP002',
                roles: ['Employee']
            }));

            await clearImpersonation(reloadSpy);

            expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', {
                method: 'DELETE'
            });
            expect(localStorage.getItem('impersonatedUser')).toBeNull();
            expect(reloadSpy).toHaveBeenCalled();
        });

        test('handles API error gracefully', async () => {
            const reloadSpy = jest.fn();
            global.fetch = jest.fn(() =>
                Promise.resolve({
                    ok: false,
                    text: () => Promise.resolve('Error message')
                })
            );
            global.alert = jest.fn();
            const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation();

            await clearImpersonation(reloadSpy);

            expect(reloadSpy).not.toHaveBeenCalled();
            expect(global.alert).toHaveBeenCalledWith('Failed to clear impersonation');

            consoleErrorSpy.mockRestore();
        });
    });

    describe('loadSavedImpersonation', () => {
        test('loads saved data and updates UI', () => {
            localStorage.setItem('impersonatedUser', JSON.stringify({
                employeeNumber: 'EMP002',
                roles: ['Manager', 'Admin']
            }));

            loadSavedImpersonation();

            expect(document.getElementById('impersonateUser').value).toBe('EMP002');
            expect(document.getElementById('roleEmployee').checked).toBe(false);
            expect(document.getElementById('roleManager').checked).toBe(true);
            expect(document.getElementById('roleApprover').checked).toBe(false);
            expect(document.getElementById('roleAdmin').checked).toBe(true);
        });

        test('does nothing when no saved data', () => {
            loadSavedImpersonation();

            // Should not throw and original values should remain
            expect(document.getElementById('roleEmployee').checked).toBe(true);
        });

        test('handles corrupted localStorage data', () => {
            localStorage.setItem('impersonatedUser', 'invalid json');

            // Should not throw
            expect(() => loadSavedImpersonation()).not.toThrow();
        });

        test('handles missing roles array', () => {
            localStorage.setItem('impersonatedUser', JSON.stringify({
                employeeNumber: 'EMP002'
            }));

            expect(() => loadSavedImpersonation()).not.toThrow();
        });
    });

    describe('initImpersonationPanel', () => {
        test('loads saved data and sets global functions', () => {
            localStorage.setItem('impersonatedUser', JSON.stringify({
                employeeNumber: 'EMP002',
                roles: ['Manager']
            }));

            initImpersonationPanel();

            expect(document.getElementById('impersonateUser').value).toBe('EMP002');
            expect(window.toggleImpersonationPanel).toBe(toggleImpersonationPanel);
            expect(window.applyImpersonation).toBe(applyImpersonation);
            expect(window.clearImpersonation).toBe(clearImpersonation);
        });
    });
});
