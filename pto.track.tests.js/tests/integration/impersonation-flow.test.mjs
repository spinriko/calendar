/**
 * @jest-environment jsdom
 */

import { jest } from '@jest/globals';
import {
    applyImpersonation,
    getRolesForUser
} from '../../../pto.track/wwwroot/js/impersonation-panel.mjs';

describe('Impersonation Flow Integration', () => {
    beforeEach(() => {
        // Mock fetch
        global.fetch = jest.fn(() =>
            Promise.resolve({
                ok: true,
                text: () => Promise.resolve('OK')
            })
        );
    }); afterEach(() => {
        jest.clearAllMocks();
    });

    test('Role Mapping: Verifies all mock users have correct roles', () => {
        expect(getRolesForUser('EMP001')).toEqual(['Employee']);
        expect(getRolesForUser('EMP002')).toEqual(['Employee']);
        expect(getRolesForUser('MGR001')).toEqual(['Employee', 'Manager']);
        expect(getRolesForUser('APR001')).toEqual(['Employee', 'Approver']);
        expect(getRolesForUser('ADMIN001')).toEqual(['Employee', 'Admin']);
    });

    test('Full flow: Select Manager -> Apply -> API Call -> Reload', async () => {
        // 1. Setup DOM
        document.body.innerHTML = `
            <select id="impersonateUser">
                <option value="MGR001">Manager</option>
            </select>
        `;
        const select = document.getElementById('impersonateUser');
        select.value = 'MGR001';

        // 2. Apply impersonation
        // Pass a mock reload function to avoid window.location.reload issues if any
        const mockReload = jest.fn();
        await applyImpersonation(mockReload);

        // 3. Verify API call
        expect(global.fetch).toHaveBeenCalledTimes(1);
        expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                employeeNumber: 'MGR001',
                roles: ['Employee', 'Manager']
            })
        });

        // 4. Verify Reload
        expect(mockReload).toHaveBeenCalled();
    });

    test('Full flow: Select Admin -> Apply -> API Call -> Reload', async () => {
        // 1. Setup DOM
        document.body.innerHTML = `
            <select id="impersonateUser">
                <option value="ADMIN001">Admin</option>
            </select>
        `;
        const select = document.getElementById('impersonateUser');
        select.value = 'ADMIN001';

        // 2. Apply impersonation
        const mockReload = jest.fn();
        await applyImpersonation(mockReload);

        // 3. Verify API call
        expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', expect.objectContaining({
            body: JSON.stringify({
                employeeNumber: 'ADMIN001',
                roles: ['Employee', 'Admin']
            })
        }));

        // 4. Verify Reload
        expect(mockReload).toHaveBeenCalled();
    });

    test('Full flow: Select Employee -> Apply -> API Call -> Reload', async () => {
        // 1. Setup DOM
        document.body.innerHTML = `
            <select id="impersonateUser">
                <option value="EMP002">Employee 2</option>
            </select>
        `;
        const select = document.getElementById('impersonateUser');
        select.value = 'EMP002';

        const mockReload = jest.fn();
        await applyImpersonation(mockReload);

        expect(global.fetch).toHaveBeenCalledWith('/api/impersonation', expect.objectContaining({
            body: JSON.stringify({
                employeeNumber: 'EMP002',
                roles: ['Employee']
            })
        }));
        expect(mockReload).toHaveBeenCalled();
    });
});
