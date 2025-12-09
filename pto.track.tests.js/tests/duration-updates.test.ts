
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
        // Handle "YYYY-MM-DD" format manually for tests if needed, or rely on Date parsing
        if (dateStr.match(/^\d{4}-\d{2}-\d{2}$/)) {
            date = new Date(dateStr + "T00:00:00");
            isoString = dateStr + "T00:00:00";
        } else if (dateStr.match(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/)) {
            date = new Date(dateStr + ":00");
            isoString = dateStr + ":00";
        } else {
            date = new Date(dateStr);
            try {
                isoString = date.toISOString();
            } catch (e) {
                isoString = "";
            }
        }
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

describe('AbsenceSchedulerApp - Duration Updates', () => {
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

        // Initialize elements and handlers
        app.initElements();
        app.initEventHandlers();
    });

    test('Changing End Date input should update duration', () => {
        // Initial State: Jan 1 to Jan 1 (1 day)
        app.elements.inpStartDate!.value = "2023-01-01";
        app.elements.inpEndDate!.value = "2023-01-01";
        app.elements.chkAllDay!.checked = true;

        app.calculateDuration();
        expect(app.elements.durationDisplay!.textContent).toContain("1 day");

        // Change End Date to Jan 3 (should be 3 days inclusive)
        app.elements.inpEndDate!.value = "2023-01-03";

        // Simulate change event
        app.elements.inpEndDate!.dispatchEvent(new Event('change'));

        // Assert
        expect(app.elements.durationDisplay!.textContent).toContain("3 days");
    });

    test('Changing Time inputs should update duration when All Day is unchecked', () => {
        // Initial State: Jan 1 08:00 to Jan 1 12:00 (4 hours)
        app.elements.inpStartDate!.value = "2023-01-01";
        app.elements.inpEndDate!.value = "2023-01-01";
        app.elements.inpStartTime!.value = "08:00";
        app.elements.inpEndTime!.value = "12:00";
        app.elements.chkAllDay!.checked = false;

        app.calculateDuration();
        expect(app.elements.durationDisplay!.textContent).toContain("4 hours");

        // Change End Time to 14:00 (should be 6 hours)
        app.elements.inpEndTime!.value = "14:00";

        // Simulate change event
        app.elements.inpEndTime!.dispatchEvent(new Event('change'));

        // Assert
        expect(app.elements.durationDisplay!.textContent).toContain("6 hours");
    });
});
