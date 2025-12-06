
import { jest } from '@jest/globals';
import { AbsenceSchedulerApp } from '../../pto.track/wwwroot/js/absences-scheduler';

// Mock DayPilot
(global as any).bootstrap = {
    Modal: jest.fn().mockImplementation(() => ({
        show: jest.fn(),
        hide: jest.fn()
    }))
};

const MockDate = jest.fn().mockImplementation((dateStr: any) => {
    let date: Date;
    let isoString: string;

    if (typeof dateStr === 'string') {
        date = new Date(dateStr);
        isoString = dateStr;
    } else if (dateStr && typeof dateStr.getTime === 'function') {
        date = new Date(dateStr.getTime());
        isoString = date.toISOString();
    } else {
        date = new Date(dateStr);
        try {
            isoString = date.toISOString();
        } catch (e) {
            isoString = "";
        }
    }

    return {
        toString: jest.fn().mockImplementation((format: any) => {
            if (!isoString) return "";
            if (format === "yyyy-MM-dd") return isoString.split('T')[0];
            if (format === "HH:mm") {
                if (isoString.includes('T')) {
                    return isoString.split('T')[1].substring(0, 5);
                }
                return "00:00";
            }
            return isoString;
        }),
        addDays: jest.fn().mockImplementation((days: any) => {
            const newDate = new Date(date);
            newDate.setDate(newDate.getDate() + days);
            return new (MockDate as any)(newDate.toISOString().split('.')[0]);
        }),
        getTime: jest.fn().mockReturnValue(date.getTime())
    };
});
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
        events: {
            load: jest.fn(),
            add: jest.fn(),
            update: jest.fn(),
            remove: jest.fn()
        },
        clearSelection: jest.fn(),
        visibleStart: jest.fn().mockReturnValue("2023-01-01"),
        visibleEnd: jest.fn().mockReturnValue("2023-01-31")
    })),
    Navigator: jest.fn().mockImplementation(() => ({
        init: jest.fn(),
        select: jest.fn(),
        update: jest.fn(),
        visibleStart: jest.fn().mockReturnValue(new (MockDate as any)("2023-01-01")),
        visibleEnd: jest.fn().mockReturnValue(new (MockDate as any)("2023-01-31"))
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
    }
};

describe('AbsenceSchedulerApp - Month View Selection', () => {
    let app: AbsenceSchedulerApp;

    beforeEach(() => {
        // Setup DOM
        document.body.innerHTML = `
            <div id="scheduler"></div>
            <div id="datepicker"></div>
            <div id="previous"></div>
            <div id="today"></div>
            <div id="next"></div>
            <div id="durationDisplay"></div>
            
            <!-- View Switchers -->
            <button id="viewDay"></button>
            <button id="viewWeek"></button>
            <button id="viewMonth"></button>
            
            <!-- Filters -->
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
            <div id="timeSelectionRow"></div>
        `;

        app = new AbsenceSchedulerApp(mockDayPilot, "scheduler", "datepicker");

        // Mock modal
        app.elements.modal = {
            show: jest.fn(),
            hide: jest.fn()
        };
    });

    test('Selecting single day in Month View (Jan 15) should default to All Day, 1 day duration', async () => {
        await app.init();
        jest.spyOn(app, 'validateSelection').mockReturnValue(true);

        // Simulate selecting Jan 15 (Start: Jan 15 00:00, End: Jan 16 00:00)
        const args = {
            start: new MockDate("2023-01-15T00:00:00Z"),
            end: new MockDate("2023-01-16T00:00:00Z"),
            resource: "1"
        };

        await app.handleTimeRangeSelected(args);

        // 1. Check All Day is checked
        const chkAllDay = document.getElementById('absenceAllDay') as HTMLInputElement;
        expect(chkAllDay.checked).toBe(true);

        // 2. Check Dates
        // Start should be Jan 15
        expect((document.getElementById('absenceStart') as HTMLInputElement).value).toBe("2023-01-15");
        // End should be Jan 15 (Inclusive) - NOT Jan 16
        expect((document.getElementById('absenceEnd') as HTMLInputElement).value).toBe("2023-01-15");

        // 3. Check Duration
        const durationDisplay = document.getElementById('durationDisplay') as HTMLElement;
        expect(durationDisplay.textContent).toContain("1 day");
    });

    test('Selecting multiple days in Month View (Jan 15-17) should default to All Day, 3 days duration', async () => {
        await app.init();
        jest.spyOn(app, 'validateSelection').mockReturnValue(true);

        // Simulate selecting Jan 15 to Jan 17 (Start: Jan 15 00:00, End: Jan 18 00:00)
        const args = {
            start: new MockDate("2023-01-15T00:00:00Z"),
            end: new MockDate("2023-01-18T00:00:00Z"),
            resource: "1"
        };

        await app.handleTimeRangeSelected(args);

        // 1. Check All Day is checked
        const chkAllDay = document.getElementById('absenceAllDay') as HTMLInputElement;
        expect(chkAllDay.checked).toBe(true);

        // 2. Check Dates
        // Start should be Jan 15
        expect((document.getElementById('absenceStart') as HTMLInputElement).value).toBe("2023-01-15");
        // End should be Jan 17 (Inclusive) - NOT Jan 18
        expect((document.getElementById('absenceEnd') as HTMLInputElement).value).toBe("2023-01-17");

        // 3. Check Duration
        const durationDisplay = document.getElementById('durationDisplay') as HTMLElement;
        expect(durationDisplay.textContent).toContain("3 days");
    });
});
