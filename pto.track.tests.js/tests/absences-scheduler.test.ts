
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
        }
    })),
    Navigator: jest.fn().mockImplementation(() => ({
        init: jest.fn(),
        update: jest.fn(),
        visibleStart: jest.fn().mockReturnValue("2023-01-01"),
        visibleEnd: jest.fn().mockReturnValue("2023-01-31"),
        select: jest.fn()
    })),
    Date: {
        today: jest.fn().mockReturnValue({
            firstDayOfWeek: jest.fn().mockReturnThis(),
            firstDayOfMonth: jest.fn().mockReturnThis(),
            daysInMonth: jest.fn().mockReturnValue(31),
            addDays: jest.fn().mockReturnThis(),
            addMonths: jest.fn().mockReturnThis()
        })
    },
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
});
