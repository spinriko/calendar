// Mock DayPilot library for testing

declare global {
    interface Window {
        DayPilot: typeof DayPilot;
    }
}

export namespace DayPilot {
    export class Calendar {
        config: any;
        events: {
            list: any[];
            add: (event: any) => void;
            update: (event: any) => void;
        };
        columns: any[];
        startDate: Date;

        constructor(id: string, config: any) {
            this.config = config;
            this.events = {
                list: [],
                add: function (event: any) { this.list.push(event); },
                update: function (event: any) {
                    const index = this.list.findIndex((e: any) => e.id === event.id);
                    if (index >= 0) this.list[index] = event;
                }
            };
            this.columns = [];
            this.startDate = new Date();
        }

        update(options: any) {
            if (options.columns) this.columns = options.columns;
            if (options.events) this.events.list = options.events;
        }

        visibleStart() { return "2025-11-01"; }
        visibleEnd() { return "2025-11-30"; }
        clearSelection() { }
        init() { }
    }

    export class Navigator {
        config: any;
        selectionDay: Date;
        events: { list: any[] };

        constructor(id: string, config: any) {
            this.config = config;
            this.selectionDay = new Date("2025-11-19");
            this.events = { list: [] };
        }

        init() { }
        select(date: Date) { this.selectionDay = date; }
        update(options: any) {
            if (options.events) this.events.list = options.events;
        }
        visibleStart() { return "2025-11-01"; }
        visibleEnd() { return "2025-11-30"; }
    }

    export const Date = {
        today: function () { return new globalThis.Date("2025-11-19"); }
    };

    export const Http = {
        get: function () {
            return Promise.resolve({ data: [] });
        },
        post: function (_: any, data: any) {
            return Promise.resolve({ data: { id: 1, ...data } });
        },
        put: function (_: any, data: any) {
            return Promise.resolve({ data: data });
        }
    };

    export const Modal = {
        form: function (fields: any, data: any) {
            return Promise.resolve({
                canceled: false,
                result: data || {}
            });
        },
        prompt: function () {
            return Promise.resolve({
                canceled: false,
                result: "Test comment"
            });
        },
        alert: function () {
            return Promise.resolve();
        }
    };
}

// Assign to window for global access in tests
(window as any).DayPilot = DayPilot;
