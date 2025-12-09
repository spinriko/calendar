
import { jest } from '@jest/globals';
import { AbsenceSchedulerApp } from '../../pto.track/wwwroot/js/absences-scheduler';

// Mock bootstrap
(global as any).bootstrap = {
    Modal: jest.fn().mockImplementation(() => ({
        show: jest.fn(),
        hide: jest.fn()
    }))
};

// Mock DayPilot
const MockDate = jest.fn().mockImplementation((dateStr: any) => ({
    toString: jest.fn().mockImplementation((format: any) => {
        if (!dateStr) return "";
        if (format === "yyyy-MM-dd") return dateStr.split('T')[0];
        if (format === "HH:mm") return dateStr.split('T')[1] ? dateStr.split('T')[1].substring(0, 5) : "00:00";
        return dateStr;
    }),
    addDays: jest.fn().mockReturnThis(),
    getTime: jest.fn().mockReturnValue(new Date(dateStr).getTime())
}));
(MockDate as any).today = jest.fn().mockReturnValue({
    firstDayOfWeek: jest.fn().mockReturnThis(),
    firstDayOfMonth: jest.fn().mockReturnThis(),
    daysInMonth: jest.fn().mockReturnValue(31),
    addDays: jest.fn().mockReturnThis(),
    addMonths: jest.fn().mockReturnThis()
});

const mockDayPilot = {
    Scheduler: jest.fn().mockImplementation(() => ({
        init: jest.fn(),
        update: jest.fn(),
        visibleStart: jest.fn().mockReturnValue("2023-01-01"),
        visibleEnd: jest.fn().mockReturnValue("2023-01-07"),
        startDate: {
            firstDayOfWeek: jest.fn().mockReturnThis(),
            firstDayOfMonth: jest.fn().mockReturnThis(),
            daysInMonth: jest.fn().mockReturnValue(31),
            addDays: jest.fn().mockReturnThis(),
            addMonths: jest.fn().mockReturnThis()
        },
        events: {
            add: jest.fn(),
            update: jest.fn(),
            remove: jest.fn()
        },
        clearSelection: jest.fn()
    })),
    Navigator: jest.fn().mockImplementation(() => ({
        init: jest.fn(),
        update: jest.fn(),
        visibleStart: jest.fn().mockReturnValue("2023-01-01"),
        visibleEnd: jest.fn().mockReturnValue("2023-01-31"),
        select: jest.fn()
    })),
    Date: MockDate,
    Http: {
        get: jest.fn<() => Promise<any>>().mockResolvedValue({ data: [] }),
        post: jest.fn<() => Promise<any>>().mockResolvedValue({ data: {} }),
        put: jest.fn<() => Promise<any>>().mockResolvedValue({ data: {} }),
        delete: jest.fn<() => Promise<any>>().mockResolvedValue({ data: {} })
    },
    Modal: {
        alert: jest.fn(),
        confirm: jest.fn(),
        prompt: jest.fn(),
        form: jest.fn()
    },
    Menu: jest.fn().mockImplementation(() => ({
        show: jest.fn()
    }))
};

describe('AbsenceSchedulerApp', () => {
    let app: any;
    let documentBody;

    beforeEach(() => {
        // Setup DOM
        document.body.innerHTML = `
            <div id="scheduler"></div>
            <div id="datepicker"></div>
            <button id="previous"></button>
            <button id="today"></button>
            <button id="next"></button>
            <button id="viewDay" class="view-btn"></button>
            <button id="viewWeek" class="view-btn"></button>
            <button id="viewMonth" class="view-btn"></button>
            <div><input type="checkbox" id="filterPending" checked /></div>
            <div><input type="checkbox" id="filterApproved" checked /></div>
            <div><input type="checkbox" id="filterRejected" checked /></div>
            <div><input type="checkbox" id="filterCancelled" checked /></div>
            
            <!-- Modal Elements -->
            <div id="absenceModal"></div>
            <h5 id="absenceModalLabel"></h5>
            <button id="saveAbsenceBtn"></button>
            <input id="absenceStart" />
            <input id="absenceStartTime" />
            <input id="absenceEnd" />
            <input id="absenceEndTime" />
            <input type="checkbox" id="absenceAllDay" />
            <textarea id="absenceReason"></textarea>
            <div id="durationDisplay"></div>
            <div id="timeSelectionRow"></div>
        `;

        app = new AbsenceSchedulerApp(mockDayPilot, "scheduler", "datepicker");
    });

    afterEach(() => {
        jest.clearAllMocks();
        document.body.innerHTML = '';
    });

    test('should initialize correctly', async () => {
        await app.init();

        expect(mockDayPilot.Scheduler).toHaveBeenCalledWith("scheduler", expect.objectContaining({
            onBeforeTimeHeaderRender: expect.any(Function)
        }));
        expect(mockDayPilot.Navigator).toHaveBeenCalledWith("datepicker", expect.any(Object));
        expect(mockDayPilot.Http.get).toHaveBeenCalledWith("/api/currentuser");

        // Check if checkboxes are initialized
        expect(document.getElementById('filterPending')).not.toBeNull();
    });

    test('should load scheduler data', async () => {
        await app.init();

        // Verify API calls for data
        expect(mockDayPilot.Http.get).toHaveBeenCalledWith(expect.stringContaining("/api/absences"));
        expect(mockDayPilot.Http.get).toHaveBeenCalledWith("/api/resources");
    });

    test('should handle view switching', async () => {
        await app.init();

        // Simulate clicking Day view
        const dayBtn = document.getElementById('viewDay');
        dayBtn?.click();

        expect(app.state.currentView).toBe("Day");
        expect(app.scheduler.update).toHaveBeenCalledWith(expect.objectContaining({
            days: 1,
            scale: "CellDuration"
        }));

        // Simulate clicking Month view
        const monthBtn = document.getElementById('viewMonth');
        monthBtn?.click();

        expect(app.state.currentView).toBe("Month");
        expect(app.scheduler.update).toHaveBeenCalledWith(expect.objectContaining({
            scale: "Week"
        }));
    });

    test('should open modal on time range selected', async () => {
        await app.init();

        // Mock validation to return true
        jest.spyOn(app, 'validateSelection').mockReturnValue(true);

        const args = {
            start: "2023-01-01T09:00:00",
            end: "2023-01-01T11:00:00",
            resource: "1"
        };

        await app.handleTimeRangeSelected(args);

        // Check if modal was shown
        expect(app.elements.modal.show).toHaveBeenCalled();

        // Check if inputs were populated
        expect((document.getElementById('absenceStart') as HTMLInputElement).value).toBe("2023-01-01");
        expect((document.getElementById('absenceStartTime') as HTMLInputElement).value).toBe("09:00");
        expect((document.getElementById('absenceEnd') as HTMLInputElement).value).toBe("2023-01-01");
        expect((document.getElementById('absenceEndTime') as HTMLInputElement).value).toBe("11:00");
    });
});
