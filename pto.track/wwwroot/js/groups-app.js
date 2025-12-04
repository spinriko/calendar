// groups-app.js: Extracted from Groups.cshtml for testability and linting
window.groupsApp = {
    currentUser: null,
    currentGroupId: null,
    groupModal: null,
    deleteModal: null,

    escapeHtml(str) {
        return str.replace(/[&<>'"]/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '\'': '&#39;', '"': '&quot;' }[c];
        });
    },

    async fetchResourcesForGroup() {
        const groupId = document.getElementById('groupSelect').value;
        if (!groupId) {
            document.getElementById('resourcesPanel').style.display = 'none';
            return;
        }
        const response = await fetch(`/api/groups/${groupId}/resources`);
        if (!response.ok) {
            document.getElementById('resourcesPanel').style.display = 'none';
            return;
        }
        const resources = await response.json();
        const tbody = document.getElementById('resourcesTableBody');
        tbody.innerHTML = '';
        if (resources.length === 0) {
            document.getElementById('noResources').style.display = 'block';
        } else {
            document.getElementById('noResources').style.display = 'none';
            resources.forEach(resource => {
                const roles = [];
                if (resource.isActive) roles.push('<span class="badge bg-success">Active</span>');
                if (resource.isApprover) roles.push('<span class="badge bg-info">Approver</span>');
                if (resource.role === 'Admin') roles.push('<span class="badge bg-primary">Admin</span>');
                if (resource.role === 'Manager') roles.push('<span class="badge bg-warning text-dark">Manager</span>');
                if (resource.role === 'Employee') roles.push('<span class="badge bg-secondary">Employee</span>');
                tbody.innerHTML += `<tr><td>${groupsApp.escapeHtml(resource.name)}</td><td>${roles.join(' ')}</td></tr>`;
            });
        }
        document.getElementById('resourcesPanel').style.display = 'block';
    },

    async loadGroups() {
        try {
            document.getElementById('loadingSpinner').style.display = 'block';
            document.getElementById('groupsTableContainer').style.display = 'none';

            const response = await fetch('/api/groups');
            if (!response.ok) throw new Error('Failed to load groups');

            const groups = await response.json();
            this.renderGroups(groups);

            // Populate groupSelect dropdown
            const select = document.getElementById('groupSelect');
            select.innerHTML = '<option value="">-- Select Group --</option>';
            groups.forEach(group => {
                select.innerHTML += `<option value="${group.groupId}">${groupsApp.escapeHtml(group.name)}</option>`;
            });
        } catch (error) {
            console.error('Error loading groups:', error);
            alert('Error loading groups: ' + error.message);
        } finally {
            document.getElementById('loadingSpinner').style.display = 'none';
        }
    },

    async init() {
        this.groupModal = new bootstrap.Modal(document.getElementById('groupModal'));
        this.deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));

        // Check if user is admin
        await this.checkAccess();

        // Set up event listeners
        document.getElementById('btnCreateGroup').addEventListener('click', () => this.openCreateModal());
        document.getElementById('btnSaveGroup').addEventListener('click', () => this.saveGroup());
        document.getElementById('btnConfirmDelete').addEventListener('click', () => this.confirmDelete());
    },

    async checkAccess() {
        try {
            const response = await fetch('/api/CurrentUser');
            if (!response.ok) {
                this.showAccessDenied();
                return;
            }

            this.currentUser = await response.json();
            const isAdmin = this.currentUser.roles && this.currentUser.roles.includes('Admin');

            if (isAdmin) {
                document.getElementById('adminContent').style.display = 'block';
                await this.loadGroups();
            } else {
                this.showAccessDenied();
            }
        } catch (error) {
            console.error('Error checking access:', error);
            this.showAccessDenied();
        }
    },

    showAccessDenied() {
        document.getElementById('accessDenied').style.display = 'block';
    },

    renderGroups(groups) {
        const tbody = document.getElementById('groupsTableBody');
        tbody.innerHTML = '';

        if (groups.length === 0) {
            document.getElementById('noGroups').style.display = 'block';
            document.getElementById('groupsTable').style.display = 'none';
        } else {
            document.getElementById('noGroups').style.display = 'none';
            document.getElementById('groupsTable').style.display = 'table';

            groups.forEach(group => {
                const row = document.createElement('tr');
                row.innerHTML = `
                                <td>${group.groupId}</td>
                                <td>${this.escapeHtml(group.name)}</td>
                                <td class="text-end">
                                    <button class="btn btn-sm btn-outline-primary me-1" onclick="groupsApp.openEditModal(${group.groupId}, '${this.escapeHtml(group.name)}')">
                                        Edit
                                    </button>
                                    <button class="btn btn-sm btn-outline-danger" onclick="groupsApp.openDeleteModal(${group.groupId}, '${this.escapeHtml(group.name)}')">
                                        Delete
                                    </button>
                                </td>
                            `;
                tbody.appendChild(row);
            });
        }

        document.getElementById('groupsTableContainer').style.display = 'block';
    },

    openCreateModal() {
        this.currentGroupId = null;
        document.getElementById('groupModalTitle').textContent = 'Create Group';
        document.getElementById('groupName').value = '';
        document.getElementById('groupName').classList.remove('is-invalid');
        this.groupModal.show();
    },

    openEditModal(groupId, groupName) {
        this.currentGroupId = groupId;
        document.getElementById('groupModalTitle').textContent = 'Edit Group';
        document.getElementById('groupName').value = groupName;
        document.getElementById('groupName').classList.remove('is-invalid');
        this.groupModal.show();
    },

    async saveGroup() {
        const nameInput = document.getElementById('groupName');
        const name = nameInput.value.trim();

        if (!name) {
            nameInput.classList.add('is-invalid');
            return;
        }
        nameInput.classList.remove('is-invalid');

        try {
            let response;
            if (this.currentGroupId) {
                // Update existing group
                response = await fetch(`/api/groups/${this.currentGroupId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name })
                });
            } else {
                // Create new group
                response = await fetch('/api/groups', {
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
        } catch (error) {
            console.error('Error saving group:', error);
            alert('Error saving group: ' + error.message);
        }
    },

    openDeleteModal(groupId, groupName) {
        this.currentGroupId = groupId;
        document.getElementById('deleteGroupName').textContent = groupName;
        this.deleteModal.show();
    },

    async confirmDelete() {
        if (!this.currentGroupId) return;

        try {
            const response = await fetch(`/api/groups/${this.currentGroupId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to delete group');
            }

            this.deleteModal.hide();
            await this.loadGroups();
        } catch (error) {
            console.error('Error deleting group:', error);
            alert('Error deleting group: ' + error.message);
        }
    },

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

document.addEventListener('DOMContentLoaded', () => window.groupsApp.init());
