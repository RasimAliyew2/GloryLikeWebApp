'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const root = path.resolve(__dirname, '../..');
const homeCode = fs.readFileSync(path.join(root, 'wwwroot/js/student-home.js'), 'utf8');
const registrationCode = fs.readFileSync(path.join(root, 'wwwroot/js/registration.js'), 'utf8');
const { animatedValue } = require(path.join(root, 'wwwroot/js/student-home.js'));

class Element {
    constructor(value = '') { this.value = value; this.dataset = {}; this.style = {}; this.attributes = {}; this.events = {}; this.textContent = ''; this.checked = false; this.disabled = false; this.required = false; this.hidden = false; const classes = new Set(); this.classList = { toggle: (key, on) => on ? classes.add(key) : classes.delete(key), remove: key => classes.delete(key), contains: key => classes.has(key) }; }
    addEventListener(name, fn) { (this.events[name] ||= []).push(fn); }
    async emit(name, event = {}) { for (const fn of this.events[name] || []) await fn(event); }
    setAttribute(key, value) { this.attributes[key] = value; }
    checkValidity() { return this.disabled || !this.required || this.value.length > 0; }
    focus() { this.focused = true; }
}
function home(reduced = false) {
    const number = new Element(); number.dataset.stNumber = '65';
    const ring = new Element(); ring.dataset.stRing = '65';
    const bar = new Element(); bar.dataset.stBar = '80';
    const form = new Element(); form.action = '/Student/Education'; form.reportValidity = () => true;
    const submit = new Element(); form.querySelector = () => submit;
    const error = new Element();
    const ids = { studentEducationForm: form, studentEducationError: error, studentMenuButton: new Element(), studentBackdrop: new Element() };
    const selectors = { '[data-st-number]': [number], '[data-st-ring]': [ring], '[data-st-bar]': [bar] };
    const document = { querySelectorAll: key => selectors[key] || [], getElementById: key => ids[key] || null, body: new Element(), addEventListener() {} };
    const frames = [];
    const location = { hash: '', reloads: 0, reload() { this.reloads++; } };
    const sandbox = { document, performance: { now: () => 0 }, requestAnimationFrame: fn => frames.push(fn), location, FormData: class { constructor(input) { this.form = input; } }, fetch: () => Promise.resolve({ ok: true, json: async () => ({ success: true }) }) };
    sandbox.window = { matchMedia: query => ({ matches: query.includes('reduced-motion') && reduced, addEventListener() {} }) };
    vm.runInNewContext(homeCode, sandbox);
    return { sandbox, number, ring, bar, frames, form, submit, error, location, ids };
}
function registration(type = 'employer', invitation = false) {
    const ids = {};
    ['quickRegistrationForm', 'accountType', 'profileNameLabel', 'profileName', 'registrationEmail', 'registrationPassword', 'industry', 'acceptTerms', 'registrationSubmit', 'registrationNote', 'registrationFeedback', 'studentRegistrationNote'].forEach(id => ids[id] = new Element());
    ids.accountType.value = type;
    ids.quickRegistrationForm.dataset.teamInvitation = String(invitation);
    const section = new Element();
    const fields = [new Element('University'), new Element('Computer Science'), new Element('2')];
    const buttons = ['candidate', 'student', 'employer'].map(type => { const el = new Element(); el.dataset.accountType = type; return el; });
    const links = ['google', 'apple'].map(provider => ({ href: 'https://bothfind.com/SignIn/External/' + provider + '?returnUrl=%2Fdashboard' }));
    const selectors = { '[data-account-type]': buttons, '.student-only-fields input': fields, '.social-buttons a': links };
    const window = { addEventListener() {} };
    const document = { getElementById: id => ids[id] || null, querySelector: key => key === '.student-only-fields' ? section : null, querySelectorAll: key => selectors[key] || [] };
    vm.runInNewContext(registrationCode, { document, window, URL, location: { origin: 'https://bothfind.com' } });
    return { ids, fields, links, buttons, section };
}

test('SSI counts and rings reach the same value after 500ms', () => {
    assert.equal(animatedValue(65, 0), 0);
    assert.equal(animatedValue(65, 250), 32.5);
    assert.equal(animatedValue(65, 500), 65);
    assert.equal(animatedValue(65, 900), 65);
    const ui = home();
    assert.equal(ui.number.textContent, '0');
    assert.equal(ui.ring.attributes['stroke-dashoffset'], '100');
    ui.frames.shift()(250);
    assert.equal(ui.number.textContent, '33');
    assert.equal(ui.ring.attributes['stroke-dashoffset'], '67.5');
    assert.equal(ui.bar.style.width, '40%');
    ui.frames.shift()(500);
    assert.equal(ui.number.textContent, '65');
    assert.equal(ui.ring.attributes['stroke-dashoffset'], '35');
    assert.equal(ui.bar.style.width, '80%');
    assert.equal(ui.frames.length, 0);
});
test('Reduced motion immediately shows final values without animation', () => {
    const ui = home(true);
    assert.equal(ui.frames.length, 0);
    assert.equal(ui.number.textContent, '65');
    assert.equal(ui.ring.attributes['stroke-dashoffset'], '35');
});
test('Education submission includes same-origin credentials, prevents double submit, and reports failure', async () => {
    const ui = home(); let complete; let count = 0;
    ui.sandbox.fetch = (url, options) => { count++; assert.equal(url, '/Student/Education'); assert.equal(options.credentials, 'same-origin'); assert.equal(options.body.form, ui.form); return new Promise(resolve => complete = resolve); };
    const saving = ui.form.emit('submit', { preventDefault() {} });
    await ui.form.emit('submit', { preventDefault() {} });
    assert.equal(count, 1); assert.equal(ui.submit.disabled, true);
    complete({ ok: false, json: async () => ({ success: false, message: 'Please try again.' }) });
    await saving;
    assert.equal(ui.error.hidden, false); assert.equal(ui.error.textContent, 'Please try again.');
    assert.equal(ui.submit.disabled, false); assert.equal(ui.location.reloads, 0);
});
test('Successful education update reloads the Student education section', async () => {
    const ui = home(); await ui.form.emit('submit', { preventDefault() {} });
    assert.equal(ui.location.hash, 'education'); assert.equal(ui.location.reloads, 1);
});
test('Student registration persists selection in both OAuth links and enables only Student fields', async () => {
    const ui = registration('student');
    assert.equal(ui.ids.accountType.value, 'student'); assert.equal(ui.section.hidden, false);
    assert.ok(ui.fields.every(field => field.required && !field.disabled));
    assert.equal(ui.ids.industry.required, false);
    assert.equal(ui.ids.registrationSubmit.textContent, 'Create a Student Profile');
    ui.links.forEach(link => { const url = new URL(link.href); assert.equal(url.searchParams.get('accountType'), 'student'); assert.equal(url.searchParams.get('returnUrl'), '/dashboard'); });
    await ui.buttons[0].emit('click');
    assert.equal(ui.ids.accountType.value, 'candidate'); assert.equal(ui.section.hidden, true);
    assert.ok(ui.fields.every(field => !field.required && field.disabled));
    ui.links.forEach(link => assert.equal(new URL(link.href).searchParams.get('accountType'), 'candidate'));
    await ui.buttons[2].emit('click');
    assert.equal(ui.ids.industry.required, true);
});
test('Team invitations keep employer role despite Student query selection', () => {
    const ui = registration('student', true);
    assert.equal(ui.ids.accountType.value, 'employer');
    assert.equal(ui.section.hidden, true); assert.equal(ui.ids.registrationSubmit.textContent, 'Accept Invitation');
});
test('Incomplete Student education blocks registration', async () => {
    const ui = registration('student'); ui.fields[0].value = ''; let prevented = false;
    await ui.ids.quickRegistrationForm.emit('submit', { preventDefault() { prevented = true; } });
    assert.equal(prevented, true); assert.equal(ui.ids.registrationFeedback.hidden, false);
    assert.equal(ui.fields[0].focused, true);
});
