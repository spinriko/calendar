import { updateViewButtons } from "../../../../pto.track/wwwroot/js/calendar-functions.mjs";

describe('updateViewButtons', () => {
    let buttons;

    beforeEach(() => {
        // Mock buttons
        buttons = [
            { id: 'viewDay', style: { fontWeight: 'normal', backgroundColor: '' } },
            { id: 'viewWeek', style: { fontWeight: 'bold', backgroundColor: '#ddd' } }, // Initially active
            { id: 'viewMonth', style: { fontWeight: 'normal', backgroundColor: '' } }
        ];
    });

    it('activates Day view and deactivates others', () => {
        updateViewButtons(buttons, 'Day');

        expect(buttons[0].style.fontWeight).toBe('bold');
        expect(buttons[0].style.backgroundColor).toBe('#ddd');

        expect(buttons[1].style.fontWeight).toBe('normal');
        expect(buttons[1].style.backgroundColor).toBe('');

        expect(buttons[2].style.fontWeight).toBe('normal');
        expect(buttons[2].style.backgroundColor).toBe('');
    });

    it('activates Week view and deactivates others', () => {
        // Reset to all normal first
        buttons.forEach(b => { b.style.fontWeight = 'normal'; b.style.backgroundColor = ''; });

        updateViewButtons(buttons, 'Week');

        expect(buttons[0].style.fontWeight).toBe('normal');
        expect(buttons[1].style.fontWeight).toBe('bold');
        expect(buttons[1].style.backgroundColor).toBe('#ddd');
        expect(buttons[2].style.fontWeight).toBe('normal');
    });

    it('activates Month view and deactivates others', () => {
        updateViewButtons(buttons, 'Month');

        expect(buttons[0].style.fontWeight).toBe('normal');
        expect(buttons[1].style.fontWeight).toBe('normal');
        expect(buttons[2].style.fontWeight).toBe('bold');
        expect(buttons[2].style.backgroundColor).toBe('#ddd');
    });

    it('handles unknown view gracefully (deactivates all)', () => {
        updateViewButtons(buttons, 'Year');

        expect(buttons[0].style.fontWeight).toBe('normal');
        expect(buttons[1].style.fontWeight).toBe('normal');
        expect(buttons[2].style.fontWeight).toBe('normal');
    });
});
