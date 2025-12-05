/**
 * @jest-environment jsdom
 */

import { jest } from '@jest/globals';
import {
    toggleImpersonationPanel,
    getRolesForUser,
    getImpersonationData,
    applyImpersonation,
    loadSavedImpersonation,
    initImpersonationPanel
} from '../../pto.track/wwwroot/js/impersonation-panel.mjs';

describe('Impersonation Panel', () => {
    let mockPanel: any;

    beforeEach(() => {
        // Set up DOM
        document.body.innerHTML = `
            <div class="impersonation-panel" style="display: none;"></div>
            <select id="impersonateUser">
                <option value="EMP001">Test Employee 1</option>
                <option value="EMP002">Test Employee 2</option>
                <option value="MGR001">Test Manager</option>
                <option value="APR001">Test Approver</option>
                <option value="ADMIN001">Administrator</option>
            </select>
        `;

        mockPanel = document.querySelector('.impersonation-panel');

        // Clear localStorage
        localStorage.clear();

        // Mock fetch
        global.fetch = jest.fn(() =>
            Promise.resolve({
                ok: true,
                text: () => Promise.resolve('OK')
            } as Response)
        );
    });

    afterEach(() => {
        localStorage.clear();
        jest.clearAllMocks();
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
    });

    describe('getRolesForUser', () => {
        test('returns correct roles for Employee', () => {
            expect(getRolesForUser('EMP001')).toEqual(['Employee']);
        });

        test('returns correct roles for Manager', () => {
            expect(getRolesForUser('MGR001')).toEqual(['Employee', 'Manager']);
        });

        test('returns correct roles for Approver', () => {
            expect(getRolesForUser('APR001')).toEqual(['Employee', 'Approver']);
        });

        test('returns correct roles for Admin', () => {
            expect(getRolesForUser('ADMIN001')).toEqual(['Employee', 'Admin']);
        });

        test('returns default Employee role for unknown user', () => {
            expect(getRolesForUser('UNKNOWN')).toEqual(['Employee']);
        });
    });

    describe('getImpersonationData', () => {
        test('returns data for selected user', () => {
            (document.getElementById('impersonateUser') as HTMLInputElement).value = 'MGR001';

            const data = getImpersonationData();

            expect(data).toEqual({
                employeeNumber: 'MGR001',
                roles: ['Employee', 'Manager']
            });
        });

        test('defaults to EMP001 if no selection', () => {
            // Simulate missing element or value
            document.body.innerHTML = '';

            const data = getImpersonationData();

            expect(data.employeeNumber).toBe('EMP001');
            expect(data.roles).toEqual(['Employee']);
        });
    });

    describe('applyImpersonation', () => {
        test('sends correct data to API and reloads', async () => {
            (document.getElementById('impersonateUser') as HTMLInputElement).value = 'MGR001';
            const reloadMock = jest.fn();

            await applyImpersonation(reloadMock);

            // Verify API call
            expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', expect.objectContaining({
                method: 'POST',
                body: JSON.stringify({
                    employeeNumber: 'MGR001',
                    roles: ['Employee', 'Manager']
                })
            }));

            // Verify localStorage update
            const saved = JSON.parse(localStorage.getItem('impersonatedUser') || '{}');
            expect(saved.employeeNumber).toBe('MGR001');

            // Verify reload
            expect(reloadMock).toHaveBeenCalled();
        });

        test('handles API failure', async () => {
            (global.fetch as jest.Mock).mockImplementationOnce(() =>
                Promise.resolve({
                    ok: false,
                    text: () => Promise.resolve('Error')
                } as Response)
            );

            const alertMock = jest.spyOn(window, 'alert').mockImplementation(() => { });
            const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => { });
            const reloadMock = jest.fn();

            await applyImpersonation(reloadMock);

            expect(alertMock).toHaveBeenCalledWith('Failed to apply impersonation');
            expect(consoleSpy).toHaveBeenCalled();
            expect(reloadMock).not.toHaveBeenCalled();

            consoleSpy.mockRestore();
        });
    });

    describe('loadSavedImpersonation', () => {
        test('restores selection from localStorage', () => {
            const savedData = {
                employeeNumber: 'MGR001',
                roles: ['Employee', 'Manager']
            };
            localStorage.setItem('impersonatedUser', JSON.stringify(savedData));

            loadSavedImpersonation();

            const select = document.getElementById('impersonateUser') as HTMLInputElement;
            expect(select.value).toBe('MGR001');
        });

        test('does nothing if no saved data', () => {
            (document.getElementById('impersonateUser') as HTMLInputElement).value = 'EMP001';

            loadSavedImpersonation();

            const select = document.getElementById('impersonateUser') as HTMLInputElement;
            expect(select.value).toBe('EMP001');
        });
    });
});
