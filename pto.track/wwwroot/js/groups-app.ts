import { } from './calendar-functions.js'; // Ensure module treatment if needed, or just rely on tsc

declare var bootstrap: any;

interface Group {
    groupId: number;
    name: string;
}

interface Resource {
    name: string;
    isActive: boolean;
    isApprover: boolean;
    role: string;
}

interface CurrentUser {
    roles: string[];
}

interface GroupsApp {
    currentUser: CurrentUser | null;
    currentGroupId: number | null;
    groupModal: any;
    deleteModal: any;
    baseUrl: string;
    escapeHtml(str: string): string;
    fetchResourcesForGroup(): Promise<void>;
    loadGroups(): Promise<void>;
    init(baseUrl?: string): Promise<void>;
    checkAccess(): Promise<void>;
    showAccessDenied(): void;
    renderGroups(groups: Group[]): void;
    openCreateModal(): void;
    openEditModal(groupId: number, groupName: string): void;
    saveGroup(): Promise<void>;
    openDeleteModal(groupId: number, groupName: string): void;
    confirmDelete(): Promise<void>;
}

// Remove the global augmentation block and just declare the property on Window interface
// This works because this file is treated as a script (no imports/exports)
interface Window {
    groupsApp: GroupsApp;
}

window.groupsApp = {
    currentUser: null,
    currentGroupId: null,
    groupModal: null,
    deleteModal: null,
    baseUrl: '/',

    escapeHtml(str: string): string {
        return str.replace(/[&<>'"]/g, function (c) {
            const map: { [key: string]: string } = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '\'': '&#39;', '"': '&quot;' };
            return map[c];
        });
    },

    async fetchResourcesForGroup() {
        const select = document.getElementById('groupSelect') as HTMLSelectElement;
        const groupId = select.value;
        const resourcesPanel = document.getElementById('resourcesPanel') as HTMLElement;

        if (!groupId) {
            resourcesPanel.style.display = 'none';
            return;
        }
        const response = await fetch(`${this.baseUrl}api/groups/${groupId}/resources`);
        if (!response.ok) {
            resourcesPanel.style.display = 'none';
            return;
        }
        const resources: Resource[] = await response.json();
        const tbody = document.getElementById('resourcesTableBody') as HTMLElement;
        tbody.innerHTML = '';

        const noResources = document.getElementById('noResources') as HTMLElement;
        if (resources.length === 0) {
            noResources.style.display = 'block';
        } else {
            noResources.style.display = 'none';
            resources.forEach(resource => {
                const roles: string[] = [];
                if (resource.isActive) roles.push('<span class="badge bg-success">Active</span>');
                if (resource.isApprover) roles.push('<span class="badge bg-info">Approver</span>');
                if (resource.role === 'Admin') roles.push('<span class="badge bg-primary">Admin</span>');
                if (resource.role === 'Manager') roles.push('<span class="badge bg-warning text-dark">Manager</span>');
                if (resource.role === 'Employee') roles.push('<span class="badge bg-secondary">Employee</span>');
                tbody.innerHTML += `<tr><td>${this.escapeHtml(resource.name)}</td><td>${roles.join(' ')}</td></tr>`;
            });
        }
        resourcesPanel.style.display = 'block';
    },

    async loadGroups() {
        try {
            const loadingSpinner = document.getElementById('loadingSpinner') as HTMLElement;
            const groupsTableContainer = document.getElementById('groupsTableContainer') as HTMLElement;

            loadingSpinner.style.display = 'block';
            groupsTableContainer.style.display = 'none';

            const response = await fetch(`${this.baseUrl}api/groups`);
            if (!response.ok) throw new Error('Failed to load groups');

            const groups: Group[] = await response.json();
            this.renderGroups(groups);

            // Populate groupSelect dropdown
            const select = document.getElementById('groupSelect') as HTMLElement;
            if (select) {
                select.innerHTML = '<option value="">-- Select Group --</option>';
                groups.forEach(group => {
                    select.innerHTML += `<option value="${group.groupId}">${this.escapeHtml(group.name)}</option>`;
                });
            }
        } catch (error: any) {
            console.error('Error loading groups:', error);
            alert('Error loading groups: ' + error.message);
        } finally {
            const loadingSpinner = document.getElementById('loadingSpinner') as HTMLElement;
            if (loadingSpinner) loadingSpinner.style.display = 'none';
        }
    },

    async init(baseUrl: string = '/') {
        this.baseUrl = baseUrl;
        this.groupModal = new bootstrap.Modal(document.getElementById('groupModal'));
        this.deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

        document.getElementById('btnCreateGroup')?.addEventListener('click', () => this.openCreateModal());
        document.getElementById('btnSaveGroup')?.addEventListener('click', () => this.saveGroup());
        document.getElementById('btnConfirmDelete')?.addEventListener('click', () => this.confirmDelete());

        await this.checkAccess();
    },

    async checkAccess() {
        try {
            const response = await fetch(`${this.baseUrl}api/currentuser`);
            if (response.ok) {
                this.currentUser = await response.json();
                if (this.currentUser?.roles.includes('Admin')) {
                    const adminContent = document.getElementById('adminContent');
                    if (adminContent) adminContent.style.display = 'block';
                    this.loadGroups();
                } else {
                    this.showAccessDenied();
                }
            } else {
                this.showAccessDenied();
            }
        } catch (error) {
            console.error('Error checking access:', error);
            this.showAccessDenied();
        }
    },

    showAccessDenied() {
        const accessDenied = document.getElementById('accessDenied');
        if (accessDenied) accessDenied.style.display = 'block';
        const adminContent = document.getElementById('adminContent');
        if (adminContent) adminContent.style.display = 'none';
    },

    renderGroups(groups: Group[]) {
        const tbody = document.getElementById('groupsTableBody') as HTMLElement;
        const container = document.getElementById('groupsTableContainer') as HTMLElement;
        tbody.innerHTML = '';

        groups.forEach(group => {
            tbody.innerHTML += `
                <tr>
                    <td>${this.escapeHtml(group.name)}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary me-2" onclick="groupsApp.openEditModal(${group.groupId}, '${this.escapeHtml(group.name)}')">
                            Edit
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="groupsApp.openDeleteModal(${group.groupId}, '${this.escapeHtml(group.name)}')">
                            Delete
                        </button>
                    </td>
                </tr>
            `;
        });
        container.style.display = 'block';
    },

    openCreateModal() {
        this.currentGroupId = null;
        const title = document.getElementById('groupModalLabel');
        const input = document.getElementById('groupName') as HTMLInputElement;
        if (title) title.textContent = 'Create New Group';
        if (input) input.value = '';
        this.groupModal.show();
    },

    openEditModal(groupId: number, groupName: string) {
        this.currentGroupId = groupId;
        const title = document.getElementById('groupModalLabel');
        const input = document.getElementById('groupName') as HTMLInputElement;
        if (title) title.textContent = 'Edit Group';
        if (input) input.value = groupName;
        this.groupModal.show();
    },

    async saveGroup() {
        const nameInput = document.getElementById('groupName') as HTMLInputElement;
        const name = nameInput.value.trim();
        if (!name) {
            alert('Please enter a group name');
            return;
        }

        try {
            let response;
            if (this.currentGroupId) {
                // Update existing group
                response = await fetch(`${this.baseUrl}api/groups/${this.currentGroupId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name })
                });
            } else {
                // Create new group
                response = await fetch(`${this.baseUrl}api/groups`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name })
                });
            }

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to save group');
            }

            this.groupModal.hide();
            await this.loadGroups();
        } catch (error: any) {
            console.error('Error saving group:', error);
            alert('Error saving group: ' + error.message);
        }
    },

    openDeleteModal(groupId: number, groupName: string) {
        this.currentGroupId = groupId;
        const deleteGroupName = document.getElementById('deleteGroupName');
        if (deleteGroupName) deleteGroupName.textContent = groupName;
        this.deleteModal.show();
    },

    async confirmDelete() {
        if (!this.currentGroupId) return;

        try {
            const response = await fetch(`${this.baseUrl}api/groups/${this.currentGroupId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to delete group');
            }

            this.deleteModal.hide();
            await this.loadGroups();
        } catch (error: any) {
            console.error('Error deleting group:', error);
            alert('Error deleting group: ' + error.message);
        }
    }
};
