'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const script = fs.readFileSync(path.join(__dirname, '../../wwwroot/js/vacancy-type-dialog.js'), 'utf8');

function setup() {
    const listeners = {};
    const closeEvents = {};
    const choices = ['Employee', 'Internship'].map(type => ({ dataset: { vacancyType: type }, focus() { this.focused = true; } }));
    const cancel = { addEventListener(name, action) { this[name] = action; } };
    const dialog = {
        open: false,
        showModal() { this.open = true; },
        close() { this.open = false; closeEvents.close(); },
        querySelectorAll(selector) { return selector === '[data-vacancy-type]' ? choices : [cancel]; },
        addEventListener(name, action) { closeEvents[name] = action; }
    };
    const document = {
        getElementById() { return dialog; },
        addEventListener(name, action) { listeners[name] = action; }
    };
    vm.runInNewContext(script, { document, URL, window: { location: { href: 'https://bothfind.example/Employer/Vacancies' } } });
    function click(href, options = {}) {
        const trigger = { href, closest() { return options.parent || null; }, focus() { this.focused = true; } };
        const event = { target: { closest: () => trigger }, button: 0, preventDefault() { this.prevented = true; }, ...options };
        listeners.click(event);
        return { trigger, event };
    }
    return { dialog, choices, cancel, click };
}

test('Create opens a choice dialog before navigation and focuses Employee', () => {
    const ui = setup();
    const { event } = ui.click('/Employer/Vacancies/Create');
    assert.equal(event.prevented, true);
    assert.equal(ui.dialog.open, true);
    assert.equal(ui.choices[0].focused, true);
    assert.equal(new URL(ui.choices[0].href).searchParams.get('vacancyType'), 'Employee');
    assert.equal(new URL(ui.choices[1].href).searchParams.get('vacancyType'), 'Internship');
});

test('Both choices preserve hiring plan and replace any old category', () => {
    const ui = setup();
    ui.click('/Employer/Vacancies/Create?hiringPlanId=37&vacancyType=Employee');
    for (const choice of ui.choices) {
        const url = new URL(choice.href);
        assert.equal(url.pathname, '/Employer/Vacancies/Create');
        assert.equal(url.searchParams.get('hiringPlanId'), '37');
        assert.deepEqual(url.searchParams.getAll('vacancyType'), [choice.dataset.vacancyType]);
    }
});

test('Cancel restores focus and reopening from another entrypoint clears old plan context', () => {
    const ui = setup();
    const { trigger } = ui.click('/Employer/Vacancies/Create?hiringPlanId=37');
    ui.cancel.click();
    assert.equal(ui.dialog.open, false);
    assert.equal(trigger.focused, true);
    ui.click('/Employer/Vacancies/Create');
    assert.equal(new URL(ui.choices[1].href).searchParams.has('hiringPlanId'), false);
});

test('Modified clicks keep native link navigation', () => {
    for (const options of [{ ctrlKey: true }, { metaKey: true }, { button: 1 }]) {
        const ui = setup();
        const { event } = ui.click('/Employer/Vacancies/Create', options);
        assert.equal(event.prevented, undefined);
        assert.equal(ui.dialog.open, false);
    }
});

test('Closing a hiring-plan dialog returns focus to its visible actions button', () => {
    const ui = setup();
    const actions = { focus() { this.focused = true; } };
    ui.click('/Employer/Vacancies/Create?hiringPlanId=37', { parent: { querySelector: () => actions } });
    ui.cancel.click();
    assert.equal(actions.focused, true);
});
