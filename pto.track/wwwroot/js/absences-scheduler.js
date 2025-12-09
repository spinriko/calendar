import { getStatusColor, buildContextMenuItems, shouldAllowSelection, getCellCssClass, updateViewButtons } from './calendar-functions.js';
export class AbsenceSchedulerApp {
    constructor(dayPilot, schedulerId, datepickerId) {
        this.DayPilot = dayPilot;
        this.schedulerId = schedulerId;
        this.datepickerId = datepickerId;
        this.scheduler = null;
        this.datepicker = null;
        this.state = {
            selectedStatuses: ["Pending", "Approved", "Rejected", "Cancelled"],
            currentEmployeeId: null,
            currentView: "Week",
            isManager: false,
            currentUser: null,
            isMockMode: false
        };
        this.elements = {};
    }
    init() {
        this.initElements();
        this.initScheduler();
        this.initDatepicker();
        return this.loadCurrentUser().then(() => {
            this.initializeCheckboxes();
            this.loadSchedulerData();
            this.loadDatePickerData();
            this.initEventHandlers();
        });
    }
    initElements() {
        this.elements = {
            previous: document.getElementById("previous"),
            today: document.getElementById("today"),
            next: document.getElementById("next"),
            filterPending: document.getElementById("filterPending"),
            filterApproved: document.getElementById("filterApproved"),
            filterRejected: document.getElementById("filterRejected"),
            filterCancelled: document.getElementById("filterCancelled"),
            // Modal elements
            modal: new bootstrap.Modal(document.getElementById('absenceModal')),
            modalEl: document.getElementById('absenceModal'),
            modalTitle: document.getElementById('absenceModalLabel'),
            modalSaveBtn: document.getElementById('saveAbsenceBtn'),
            inpStartDate: document.getElementById('absenceStart'),
            inpStartTime: document.getElementById('absenceStartTime'),
            inpEndDate: document.getElementById('absenceEnd'),
            inpEndTime: document.getElementById('absenceEndTime'),
            chkAllDay: document.getElementById('absenceAllDay'),
            inpReason: document.getElementById('absenceReason'),
            durationDisplay: document.getElementById('durationDisplay'),
            timeSelectionRow: document.getElementById('timeSelectionRow')
        };
    }
    initScheduler() {
        this.scheduler = new this.DayPilot.Scheduler(this.schedulerId, {
            startDate: this.DayPilot.Date.today().firstDayOfWeek(1),
            days: 5,
            scale: "Day",
            width: "100%",
            height: 500,
            rowHeaderWidth: 200,
            cellWidth: 200,
            timeHeaders: [
                { groupBy: "Month", format: "MMMM yyyy" },
                { groupBy: "Day", format: "ddd M/d" }
            ],
            eventMoveHandling: "Disabled",
            eventResizeHandling: "Disabled",
            treeEnabled: true,
            timeRangeSelectedHandling: "Enabled",
            onBeforeTimeHeaderRender: args => {
                if (this.state.currentView === "Month" && args.headerLevel === 1) {
                    const start = new this.DayPilot.Date(args.start);
                    args.html = `<span style='font-weight:normal'>Week of ${start.toString("MMM d")}</span>`;
                }
            },
            onBeforeRowHeaderRender: args => this.handleBeforeRowHeaderRender(args),
            onBeforeCellRender: args => this.handleBeforeCellRender(args),
            onTimeRangeSelected: args => this.handleTimeRangeSelected(args),
            onEventClick: async () => {
                // Disabled - using context menu instead
                return;
            },
            onBeforeEventRender: args => this.handleBeforeEventRender(args),
        });
        this.scheduler.init();
    }
    handleBeforeRowHeaderRender(args) {
        if (!this.state.currentUser)
            return;
        const isAdmin = this.state.currentUser?.roles?.includes("Admin") || false;
        const isManager = this.state.isManager || false;
        const isApprover = this.state.currentUser?.isApprover || false;
        if (!shouldAllowSelection(this.state.currentEmployeeId, args.row.id, isManager, isAdmin, isApprover)) {
            args.row.cssClass = "disabled-row";
        }
    }
    handleBeforeCellRender(args) {
        if (!this.state.currentUser)
            return;
        const isAdmin = this.state.currentUser?.roles?.includes("Admin") || false;
        const isManager = this.state.isManager || false;
        const isApprover = this.state.currentUser?.isApprover || false;
        const cssClass = getCellCssClass(args.cell.start, this.DayPilot.Date.today(), this.state.currentEmployeeId, args.cell.resource, isManager, isAdmin, isApprover);
        if (cssClass) {
            args.cell.cssClass = cssClass;
        }
    }
    async handleTimeRangeSelected(args) {
        if (!this.validateSelection(args)) {
            this.scheduler.clearSelection();
            return;
        }
        // Store the selected resource ID for saving later
        this.state.selectedResourceId = args.resource;
        const start = new this.DayPilot.Date(args.start);
        const end = new this.DayPilot.Date(args.end);
        // Populate Modal
        this.elements.inpStartDate.value = start.toString("yyyy-MM-dd");
        this.elements.inpStartTime.value = start.toString("HH:mm");
        this.elements.inpEndDate.value = end.toString("yyyy-MM-dd");
        this.elements.inpEndTime.value = end.toString("HH:mm");
        this.elements.inpReason.value = "";
        // Default to All Day if the selection covers full days (e.g. Month view or full day selection)
        // Or if start/end times are 00:00
        const isAllDay = (start.toString("HH:mm") === "00:00" && end.toString("HH:mm") === "00:00");
        this.elements.chkAllDay.checked = isAllDay;
        // Trigger change event to update UI state (disable time inputs if all day)
        this.elements.chkAllDay.dispatchEvent(new Event('change'));
        // Calculate initial duration
        this.calculateDuration();
        // Show Modal
        this.elements.modal.show();
        // Clear selection in scheduler
        this.scheduler.clearSelection();
    }
    calculateDuration() {
        const startDate = this.elements.inpStartDate.value;
        const startTime = this.elements.inpStartTime.value;
        const endDate = this.elements.inpEndDate.value;
        const endTime = this.elements.inpEndTime.value;
        const isAllDay = this.elements.chkAllDay.checked;
        if (!startDate || !endDate) {
            this.elements.durationDisplay.textContent = "Please select dates";
            this.elements.durationDisplay.className = "alert alert-warning";
            return;
        }
        let start, end;
        if (isAllDay) {
            start = new this.DayPilot.Date(startDate);
            end = new this.DayPilot.Date(endDate).addDays(1);
        }
        else {
            if (!startTime || !endTime) {
                this.elements.durationDisplay.textContent = "Please select times";
                this.elements.durationDisplay.className = "alert alert-warning";
                return;
            }
            start = new this.DayPilot.Date(`${startDate}T${startTime}`);
            end = new this.DayPilot.Date(`${endDate}T${endTime}`);
        }
        if (end <= start) {
            this.elements.durationDisplay.textContent = "End time must be after start time";
            this.elements.durationDisplay.className = "alert alert-danger";
            return;
        }
        const diff = end.getTime() - start.getTime();
        const totalMinutes = Math.floor(diff / (1000 * 60));
        const days = Math.floor(totalMinutes / (60 * 24));
        const hours = Math.floor((totalMinutes % (60 * 24)) / 60);
        const minutes = totalMinutes % 60;
        let durationText = "";
        if (days > 0)
            durationText += `${days} day${days > 1 ? 's' : ''} `;
        if (hours > 0)
            durationText += `${hours} hour${hours > 1 ? 's' : ''} `;
        if (minutes > 0)
            durationText += `${minutes} min${minutes > 1 ? 's' : ''}`;
        if (durationText === "")
            durationText = "0 mins";
        this.elements.durationDisplay.textContent = `Duration: ${durationText}`;
        this.elements.durationDisplay.className = "alert alert-info";
    }
    async saveAbsence() {
        const startDate = this.elements.inpStartDate.value;
        const startTime = this.elements.inpStartTime.value;
        const endDate = this.elements.inpEndDate.value;
        const endTime = this.elements.inpEndTime.value;
        const isAllDay = this.elements.chkAllDay.checked;
        const reason = this.elements.inpReason.value;
        if (!reason) {
            alert("Please enter a reason");
            return;
        }
        let start, end;
        if (isAllDay) {
            start = `${startDate}T00:00:00`;
            // For all day, end date is usually exclusive in DayPilot/Backend logic if it represents a range
            // If user selects Mon-Mon, they mean all day Monday.
            // Start: Mon 00:00
            // End: Tue 00:00
            end = new this.DayPilot.Date(endDate).addDays(1).toString("yyyy-MM-ddTHH:mm:ss");
        }
        else {
            start = `${startDate}T${startTime}:00`;
            end = `${endDate}T${endTime}:00`;
        }
        const absence = {
            employeeId: parseInt(`${this.state.selectedResourceId}`, 10),
            start: start,
            end: end,
            reason: reason
        };
        await this.submitAbsence(absence);
        this.elements.modal.hide();
    }
    validateSelection(args) {
        // Prevent selection of past dates
        if (args.start < this.DayPilot.Date.today()) {
            return false;
        }
        // Check if employee can create absence for this resource
        const isAdmin = this.state.currentUser?.roles?.includes("Admin") || false;
        const isManager = this.state.isManager || false;
        const isApprover = this.state.currentUser?.isApprover || false;
        return shouldAllowSelection(this.state.currentEmployeeId, args.resource, isManager, isAdmin, isApprover);
    }
    getAbsenceFormConfig(startDate, endDate) {
        // Generate time slots for working hours (08:00 - 18:00)
        const timeSlots = [];
        for (let h = 8; h <= 18; h++) {
            const hour = h < 10 ? `0${h}` : `${h}`;
            timeSlots.push({ name: `${hour}:00`, id: `${hour}:00` });
            if (h < 18) {
                timeSlots.push({ name: `${hour}:30`, id: `${hour}:30` });
            }
        }
        const form = [
            { name: "Start Date", id: "start", type: "date", dateFormat: "M/d/yyyy" },
            { name: "End Date", id: "end", type: "date", dateFormat: "M/d/yyyy" },
            { name: "Start Time (first day)", id: "startTime", type: "select", options: timeSlots },
            { name: "End Time (last day)", id: "endTime", type: "select", options: timeSlots },
            { name: "Reason", id: "reason", type: "textarea" }
        ];
        const data = {
            start: startDate,
            end: endDate.addDays(-1),
            startTime: "08:00",
            endTime: "17:00",
            reason: ""
        };
        return { form, data };
    }
    createAbsenceFromForm(result, resourceId) {
        // Combine date with time
        const [startHour, startMinute] = result.startTime.split(':');
        const [endHour, endMinute] = result.endTime.split(':');
        const modalStart = new this.DayPilot.Date(result.start);
        const modalEnd = new this.DayPilot.Date(result.end);
        const absenceStart = modalStart.addHours(parseInt(startHour)).addMinutes(parseInt(startMinute));
        const absenceEnd = modalEnd.addHours(parseInt(endHour)).addMinutes(parseInt(endMinute));
        return {
            start: absenceStart.toString(),
            end: absenceEnd.toString(),
            employeeId: parseInt(resourceId, 10),
            reason: result.reason
        };
    }
    async submitAbsence(absence) {
        console.log("Sending absence request:", absence);
        try {
            const { data } = await this.DayPilot.Http.post(`/api/absences`, absence);
            this.scheduler.events.add({
                start: data.start,
                end: data.end,
                id: data.id,
                text: data.reason,
                resource: data.employeeId,
                barColor: getStatusColor(data.status),
                data: data
            });
        }
        catch (error) {
            console.error("Error creating absence:", error);
            const errorMsg = error.request?.responseText || error.message || "Unknown error";
            console.error("Error details:", errorMsg);
            await this.DayPilot.Modal.alert(`Failed to create absence: ${errorMsg}`);
        }
    }
    handleBeforeEventRender(args) {
        const status = args.data.status || args.data.data?.status;
        args.data.backColor = getStatusColor(status);
        args.data.borderColor = "darker";
        args.data.fontColor = "#ffffff";
        // Add ellipsis menu icon
        args.data.areas = [];
        args.data.areas.push({
            right: 2,
            top: 2,
            width: 24,
            height: 24,
            html: "<div style='color: white; font-weight: bold; font-size: 18px; line-height: 24px; text-align: center; cursor: pointer; padding: 3px;'>⋯</div>",
            toolTip: "Actions",
            onClick: async (args) => {
                const e = args.source;
                const absence = e.data.data || e.data;
                const userContext = {
                    currentEmployeeId: this.state.currentEmployeeId,
                    isAdmin: this.state.currentUser?.role?.toLowerCase() === 'admin' ||
                        this.state.currentUser?.roles?.some(r => r.toLowerCase() === 'admin') || false,
                    isManager: this.state.currentUser?.role?.toLowerCase() === 'manager' ||
                        this.state.currentUser?.roles?.some(r => r.toLowerCase() === 'manager') || false,
                    isApprover: this.state.currentUser?.isApprover ||
                        this.state.currentUser?.roles?.some(r => r.toLowerCase() === 'approver') || false
                };
                // Build menu items and wire up action handlers
                const menuItems = buildContextMenuItems(absence, userContext, e);
                menuItems.forEach(item => {
                    if (item.onClick) {
                        const originalOnClick = item.onClick;
                        item.onClick = async (args) => {
                            const result = originalOnClick();
                            if (result && result.action) {
                                await this.handleMenuAction(result.action, result.absence, e);
                            }
                        };
                    }
                });
                const menu = new this.DayPilot.Menu({
                    items: menuItems
                });
                menu.show(e);
                args.preventDefault();
                args.stopPropagation();
                return false;
            }
        });
    }
    initDatepicker() {
        const self = this;
        this.datepicker = new this.DayPilot.Navigator(this.datepickerId, {
            selectMode: "Day", // Select by day
            showMonths: 3,
            skipMonths: 3,
            weekStarts: 1, // Start on Monday
            onTimeRangeSelected: args => {
                let startDate = args.start;
                if (self.state.currentView) {
                    switch (self.state.currentView) {
                        case "Week":
                            startDate = startDate.firstDayOfWeek(1);
                            break;
                        case "Month":
                            startDate = startDate.firstDayOfMonth();
                            self.scheduler.update({
                                days: startDate.daysInMonth()
                            });
                            break;
                    }
                }
                self.scheduler.update({
                    startDate: startDate,
                });
                self.loadSchedulerData();
            },
            onVisibleRangeChanged: args => {
                self.loadDatePickerData();
            }
        });
        this.datepicker.init();
    }
    async loadCurrentUser() {
        try {
            const response = await this.DayPilot.Http.get("/api/currentuser");
            if (response.data) {
                this.state.currentUser = response.data;
                this.state.currentEmployeeId = response.data.id;
                this.state.isMockMode = response.data.isMockMode || false;
                this.state.isManager = response.data.isApprover ||
                    response.data.roles?.some(r => r.toLowerCase() === 'manager' ||
                        r.toLowerCase() === 'approver');
                console.log("Current user:", this.state.currentUser);
                console.log("Is manager/approver:", this.state.isManager);
                console.log("Mock mode:", this.state.isMockMode);
            }
        }
        catch (error) {
            console.warn("Could not load current user, using defaults:", error);
            // Fallback for development without authentication
            this.state.currentEmployeeId = 1;
        }
    }
    initializeCheckboxes() {
        // Determine which checkboxes should be available based on role
        const isAdmin = this.state.currentUser?.roles?.includes("Admin");
        const isManagerOrApprover = this.state.isManager;
        const isEmployee = !isAdmin && !isManagerOrApprover;
        // First, ensure all checkboxes are visible (reset state)
        this.elements.filterPending.parentElement.style.display = "flex";
        this.elements.filterApproved.parentElement.style.display = "flex";
        this.elements.filterRejected.parentElement.style.display = "flex";
        this.elements.filterCancelled.parentElement.style.display = "flex";
        // Hide/show checkboxes based on role
        if (isEmployee) {
            // Employees can see ALL approved absences, but only their own pending/rejected/cancelled
            this.elements.filterPending.checked = false;
            this.elements.filterApproved.checked = true;
            this.elements.filterRejected.checked = false;
            this.elements.filterCancelled.checked = false;
            this.state.selectedStatuses = ["Approved"];
        }
        else if (isManagerOrApprover && !isAdmin) {
            // Managers/Approvers mainly care about Pending (to approve) and Approved (to verify)
            this.elements.filterPending.checked = true;
            this.elements.filterApproved.checked = true;
            this.elements.filterRejected.checked = false;
            this.elements.filterCancelled.checked = false;
            // Hide rejected and cancelled for managers/approvers
            this.elements.filterRejected.parentElement.style.display = "none";
            this.elements.filterCancelled.parentElement.style.display = "none";
            this.state.selectedStatuses = ["Pending", "Approved"];
        }
        else if (isAdmin) {
            // Admins see everything by default
            this.elements.filterPending.checked = true;
            this.elements.filterApproved.checked = true;
            this.elements.filterRejected.checked = true;
            this.elements.filterCancelled.checked = true;
            this.state.selectedStatuses = ["Pending", "Approved", "Rejected", "Cancelled"];
            console.log("initializeCheckboxes - Admin mode, selectedStatuses:", this.state.selectedStatuses);
        }
    }
    async loadSchedulerData() {
        const start = this.scheduler.visibleStart();
        const end = this.scheduler.visibleEnd();
        console.log("loadSchedulerData - selectedStatuses:", this.state.selectedStatuses);
        let absencesUrl = `/api/absences?start=${start}&end=${end}`;
        // Add all selected statuses to the query (using array notation for ASP.NET Core)
        this.state.selectedStatuses.forEach(status => {
            absencesUrl += `&status[]=${status}`;
        });
        console.log("loadSchedulerData - URL:", absencesUrl);
        // For employees: Show ALL approved absences, but only their own pending/rejected/cancelled
        const isEmployee = !this.state.isManager && !this.state.currentUser?.roles?.includes("Admin");
        if (isEmployee) {
            // Check if user is viewing ONLY approved absences
            const onlyApproved = this.state.selectedStatuses.length === 1 &&
                this.state.selectedStatuses[0] === "Approved";
            // If viewing any non-approved status, filter to employee's own requests
            if (!onlyApproved) {
                absencesUrl += `&employeeId=${this.state.currentEmployeeId}`;
            }
            // If viewing ONLY approved, don't add employeeId filter (show everyone's)
        }
        const promiseAbsences = this.DayPilot.Http.get(absencesUrl);
        const promiseResources = this.DayPilot.Http.get("/api/resources");
        try {
            const [{ data: resources }, { data: absences }] = await Promise.all([promiseResources, promiseAbsences]);
            console.log("loadSchedulerData - API returned", absences.length, "absences");
            console.log("loadSchedulerData - Absence statuses:", absences.map(a => a.status));
            // Map absences to scheduler events
            const events = absences.map(a => ({
                id: a.id,
                start: a.start,
                end: a.end,
                text: a.reason,
                resource: a.employeeId,
                barColor: getStatusColor(a.status),
                data: a
            }));
            console.log("loadSchedulerData - Updating scheduler with", events.length, "events");
            this.scheduler.update({
                resources,
                events
            });
        }
        catch (error) {
            console.error("Error loading scheduler data:", error);
            await this.DayPilot.Modal.alert("Failed to load data. Please check the console for details.");
        }
    }
    async loadDatePickerData() {
        const start = this.datepicker.visibleStart();
        const end = this.datepicker.visibleEnd();
        let absencesUrl = `/api/absences?start=${start}&end=${end}`;
        // Add all selected statuses to the query (using array notation for ASP.NET Core)
        this.state.selectedStatuses.forEach(status => {
            absencesUrl += `&status[]=${status}`;
        });
        // For employees: Show ALL approved absences, but only their own pending/rejected/cancelled
        const isEmployee = !this.state.isManager && !this.state.currentUser?.roles?.includes("Admin");
        if (isEmployee) {
            // Check if user is viewing ONLY approved absences
            const onlyApproved = this.state.selectedStatuses.length === 1 &&
                this.state.selectedStatuses[0] === "Approved";
            // If viewing any non-approved status, filter to employee's own requests
            if (!onlyApproved) {
                absencesUrl += `&employeeId=${this.state.currentEmployeeId}`;
            }
            // If viewing ONLY approved, don't add employeeId filter (show everyone's)
        }
        const { data } = await this.DayPilot.Http.get(absencesUrl);
        const events = data.map(a => ({
            start: a.start,
            end: a.end,
            text: a.reason,
            barColor: getStatusColor(a.status)
        }));
        this.datepicker.update({ events });
    }
    async handleMenuAction(action, absence, event) {
        switch (action) {
            case 'viewDetails':
                await this.viewDetails(absence);
                break;
            case 'editReason':
                await this.editReason(absence, event);
                break;
            case 'approve':
                await this.approveAbsence(absence);
                break;
            case 'reject':
                await this.rejectAbsence(absence);
                break;
            case 'delete':
                await this.deleteAbsence(absence, event);
                break;
        }
    }
    async viewDetails(absence) {
        const form = [
            { name: "Employee", id: "employeeName", disabled: true },
            { name: "Start", id: "start", type: "datetime", disabled: true },
            { name: "End", id: "end", type: "datetime", disabled: true },
            { name: "Reason", id: "reason", type: "textarea", disabled: true },
            { name: "Status", id: "status", disabled: true },
            { name: "Requested", id: "requestedDate", type: "date", disabled: true },
        ];
        if (absence.approverName) {
            form.push({ name: "Approver", id: "approverName", disabled: true });
            form.push({ name: "Approved Date", id: "approvedDate", type: "date", disabled: true });
        }
        if (absence.approvalComments) {
            form.push({ name: "Comments", id: "approvalComments", type: "textarea", disabled: true });
        }
        const formData = {
            employeeName: absence.employeeName,
            start: absence.start,
            end: absence.end,
            reason: absence.reason,
            status: absence.status,
            requestedDate: absence.requestedDate ? absence.requestedDate.split('T')[0] : null,
            approverName: absence.approverName,
            approvedDate: absence.approvedDate ? absence.approvedDate.split('T')[0] : null,
            approvalComments: absence.approvalComments
        };
        await this.DayPilot.Modal.form(form, formData);
    }
    async editReason(absence, event) {
        const modal = await this.DayPilot.Modal.prompt("Edit reason:", absence.reason);
        if (modal.canceled)
            return;
        const updateData = {
            start: absence.start,
            end: absence.end,
            reason: modal.result
        };
        await this.DayPilot.Http.put(`/api/absences/${absence.id}`, updateData);
        this.scheduler.events.update({
            ...event.data,
            reason: modal.result,
            text: modal.result
        });
        await this.loadSchedulerData();
    }
    async approveAbsence(absence) {
        const modal = await this.DayPilot.Modal.prompt("Approve this absence request? (Optional comment):");
        if (modal.canceled)
            return;
        const approvalData = {
            approverId: this.state.currentEmployeeId,
            comments: modal.result
        };
        await this.DayPilot.Http.post(`/api/absences/${absence.id}/approve`, approvalData);
        await this.loadSchedulerData();
    }
    async rejectAbsence(absence) {
        const modal = await this.DayPilot.Modal.prompt("Rejection reason:");
        if (modal.canceled || !modal.result)
            return;
        const rejectionData = {
            approverId: this.state.currentEmployeeId,
            reason: modal.result
        };
        await this.DayPilot.Http.post(`/api/absences/${absence.id}/reject`, rejectionData);
        await this.loadSchedulerData();
    }
    async deleteAbsence(absence, event) {
        const modal = await this.DayPilot.Modal.confirm("Delete this absence request?");
        if (modal.canceled)
            return;
        await this.DayPilot.Http.delete(`/api/absences/${absence.id}`);
        this.scheduler.events.remove(event);
        await this.loadSchedulerData();
    }
    initEventHandlers() {
        // View Switcher Handlers
        document.getElementById("viewDay").addEventListener("click", () => this.switchView("Day"));
        document.getElementById("viewWeek").addEventListener("click", () => this.switchView("Week"));
        document.getElementById("viewMonth").addEventListener("click", () => this.switchView("Month"));
        // Navigation Handlers
        this.elements.previous.addEventListener("click", () => this.handleNavigation("previous"));
        this.elements.next.addEventListener("click", () => this.handleNavigation("next"));
        this.elements.today.addEventListener("click", () => this.handleNavigation("today"));
        // Checkbox filter handlers
        const updateFilters = () => this.handleFilterChange();
        this.elements.filterPending.addEventListener("change", updateFilters);
        this.elements.filterApproved.addEventListener("change", updateFilters);
        this.elements.filterRejected.addEventListener("change", updateFilters);
        this.elements.filterCancelled.addEventListener("change", updateFilters);
        // Modal Handlers
        if (this.elements.chkAllDay) {
            this.elements.chkAllDay.addEventListener('change', () => {
                const isAllDay = this.elements.chkAllDay.checked;
                if (this.elements.timeSelectionRow) {
                    this.elements.timeSelectionRow.style.display = isAllDay ? 'none' : 'flex';
                }
                this.calculateDuration();
            });
            const dateInputs = [
                this.elements.inpStartDate,
                this.elements.inpStartTime,
                this.elements.inpEndDate,
                this.elements.inpEndTime
            ];
            dateInputs.forEach(input => {
                if (input)
                    input.addEventListener('change', () => this.calculateDuration());
            });
            // Smart Date Logic
            this.elements.inpStartDate.addEventListener('change', () => {
                if (!this.elements.inpEndDate.value || this.elements.inpEndDate.value < this.elements.inpStartDate.value) {
                    this.elements.inpEndDate.value = this.elements.inpStartDate.value;
                }
                this.calculateDuration();
            });
            this.elements.modalSaveBtn.addEventListener('click', () => this.saveAbsence());
        }
    }
    switchView(view) {
        this.state.currentView = view;
        // Update buttons style
        updateViewButtons(Array.from(document.querySelectorAll('.view-btn')), view);
        let startDate = this.scheduler.startDate;
        switch (view) {
            case "Day":
                console.log("Switching to Day view");
                this.scheduler.update({
                    days: 1,
                    scale: "CellDuration",
                    cellDuration: 60,
                    timeHeaders: [
                        { groupBy: "Day", format: "dddd, MMMM d, yyyy" },
                        { groupBy: "Hour" }
                    ],
                    businessBeginsHour: 8,
                    businessEndsHour: 18,
                    showNonBusiness: false,
                    cellWidth: 150
                });
                break;
            case "Week":
                console.log("Switching to Week view");
                // Ensure we start on the first day of the week (Monday)
                startDate = startDate.firstDayOfWeek(1);
                this.scheduler.update({
                    startDate: startDate,
                    days: 5,
                    scale: "Day",
                    timeHeaders: [
                        { groupBy: "Month", format: "MMMM yyyy" },
                        { groupBy: "Day", format: "ddd M/d" }
                    ],
                    showNonBusiness: true,
                    cellWidth: 200
                });
                break;
            case "Month":
                console.log("Switching to Month view");
                // Ensure we start on the first day of the month
                startDate = startDate.firstDayOfMonth();
                this.scheduler.update({
                    startDate: startDate,
                    days: startDate.daysInMonth(),
                    scale: "Week",
                    timeHeaders: [
                        { groupBy: "Month", format: "MMMM yyyy" },
                        { groupBy: "Week" } // Format handled by onBeforeTimeHeaderRender
                    ],
                    showNonBusiness: true,
                    cellWidth: 200
                });
                break;
        }
        this.loadSchedulerData();
    }
    handleNavigation(direction) {
        const currentStart = this.scheduler.startDate;
        if (direction === "today") {
            const today = this.DayPilot.Date.today();
            switch (this.state.currentView) {
                case "Day":
                    this.scheduler.update({ startDate: today });
                    break;
                case "Week":
                    this.scheduler.update({ startDate: today.firstDayOfWeek(1) });
                    break;
                case "Month":
                    this.scheduler.update({
                        startDate: today.firstDayOfMonth(),
                        days: today.daysInMonth()
                    });
                    break;
            }
        }
        else {
            const isNext = direction === "next";
            const multiplier = isNext ? 1 : -1;
            switch (this.state.currentView) {
                case "Day":
                    this.scheduler.update({ startDate: currentStart.addDays(1 * multiplier) });
                    break;
                case "Week":
                    this.scheduler.update({ startDate: currentStart.addDays(7 * multiplier) });
                    break;
                case "Month":
                    const newMonth = currentStart.addMonths(1 * multiplier);
                    this.scheduler.update({
                        startDate: newMonth,
                        days: newMonth.daysInMonth()
                    });
                    break;
            }
        }
        this.loadSchedulerData();
    }
    handleFilterChange() {
        console.log("[handleFilterChange] Checkbox states:", {
            pending: this.elements.filterPending.checked,
            approved: this.elements.filterApproved.checked,
            rejected: this.elements.filterRejected.checked,
            cancelled: this.elements.filterCancelled.checked
        });
        this.state.selectedStatuses = [];
        if (this.elements.filterPending.checked)
            this.state.selectedStatuses.push("Pending");
        if (this.elements.filterApproved.checked)
            this.state.selectedStatuses.push("Approved");
        if (this.elements.filterRejected.checked)
            this.state.selectedStatuses.push("Rejected");
        if (this.elements.filterCancelled.checked)
            this.state.selectedStatuses.push("Cancelled");
        console.log("[handleFilterChange] selectedStatuses:", this.state.selectedStatuses);
        this.loadSchedulerData();
        this.loadDatePickerData();
    }
}
