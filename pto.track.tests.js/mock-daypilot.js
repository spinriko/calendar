// Mock DayPilot library for testing
window.DayPilot = {
    Calendar: function (id, config) {
        this.config = config;
        this.events = {
            list: [],
            add: function (event) { this.list.push(event); },
            update: function (event) {
                const index = this.list.findIndex(e => e.id === event.id);
                if (index >= 0) this.list[index] = event;
            }
        };
        this.columns = [];
        this.startDate = new Date();

        this.update = function (options) {
            if (options.columns) this.columns = options.columns;
            if (options.events) this.events.list = options.events;
        };

        this.visibleStart = function () { return "2025-11-01"; };
        this.visibleEnd = function () { return "2025-11-30"; };

        this.clearSelection = function () { };

        return this;
    },

    Navigator: function (id, config) {
        this.config = config;
        this.selectionDay = new Date("2025-11-19");
        this.events = { list: [] };

        this.init = function () { };
        this.select = function (date) { this.selectionDay = date; };
        this.update = function (options) {
            if (options.events) this.events.list = options.events;
        };
        this.visibleStart = function () { return "2025-11-01"; };
        this.visibleEnd = function () { return "2025-11-30"; };

        return this;
    },

    Date: {
        today: function () { return new Date("2025-11-19"); }
    },

    Http: {
        get: function () {
            return Promise.resolve({ data: [] });
        },
        post: function (_, data) {
            return Promise.resolve({ data: { id: 1, ...data } });
        },
        put: function (_, data) {
            return Promise.resolve({ data: data });
        }
    },

    Modal: {
        form: function (fields, data) {
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
    }
};
