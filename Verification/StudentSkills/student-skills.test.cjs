'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const script = fs.readFileSync(path.join(__dirname, '../../wwwroot/js/student-skills.js'), 'utf8');

// A small event/element fixture exercises async quiz behavior without a browser or real AI.
class Element {
    constructor(tag = 'div') {
        this.tag = tag; this.children = []; this.listeners = {}; this.selectors = {};
        this.dataset = {}; this.style = {}; this.hidden = false; this.disabled = false;
        this.textContent = ''; this.value = ''; this.open = false;
    }
    append(...nodes) { this.children.push(...nodes); }
    replaceChildren(...nodes) { this.children = nodes; }
    addEventListener(name, callback) { (this.listeners[name] ||= []).push(callback); }
    fire(name, extra = {}) {
        return Promise.all((this.listeners[name] || []).map(callback => callback({ target: this, preventDefault() {}, ...extra })));
    }
    querySelector(selector) { return this.selectors[selector] || this.querySelectorAll(selector)[0] || null; }
    querySelectorAll(selector) {
        if (this.selectors[selector]) return [].concat(this.selectors[selector]);
        return this.children.flatMap(child => (child.tag === selector ? [child] : []).concat(child.querySelectorAll?.(selector) || []));
    }
    showModal() { this.open = true; }
    close() { this.open = false; return this.fire('close'); }
    getBoundingClientRect() { return { left: 0, top: 0, right: 500, bottom: 500 }; }
}
function setup(auto = false) {
    const dialog = new Element('dialog');
    const config = new Element();
    config.dataset = { generateUrl: '/Student/Skills/Assessment/Generate', submitUrl: '/Student/Skills/Assessment/Submit', autoSkillId: auto ? '1' : '0', autoSkillName: auto ? 'SQL' : '' };
    config.selectors['input[name="__RequestVerificationToken"]'] = { value: 'csrf-fixture' };
    const names = ['skill-name', 'language', 'loading', 'error', 'error-copy', 'questions', 'progress', 'result', 'score', 'result-copy'];
    const ui = {};
    for (const name of names) dialog.selectors[`[data-student-quiz-${name}]`] = ui[name] = new Element();
    ui.language.value = 'en';
    for (const name of ['retry-quiz', 'submit-quiz']) dialog.selectors[`[data-student-${name}]`] = ui[name] = new Element('button');
    const close = new Element('button');
    dialog.selectors['[data-student-close-quiz]'] = [close];
    const trigger = new Element('button'); trigger.dataset = { skillId: '1', skillName: 'SQL' };
    const nodes = { '#studentSkillAssessment': dialog, '#studentSkillAssessmentConfig': config };
    const body = { style: { overflow: 'auto' } };
    const requests = [];
    let reloads = 0;
    const document = {
        body,
        querySelector(selector) { return nodes[selector] || null; },
        querySelectorAll(selector) { return selector === '[data-student-assess-skill]' ? [trigger] : []; },
        createElement(tag) { return new Element(tag); },
        createTextNode(text) { const node = new Element('#text'); node.textContent = text; return node; }
    };
    const fetch = (url, options) => new Promise(resolve => requests.push({ url, options, resolve }));
    vm.runInNewContext(script, { document, fetch, AbortController, console, window: {
        setTimeout() { return 1; }, clearTimeout() {}, location: { reload() { reloads++; } }
    } });
    return { dialog, ui, close, trigger, requests, body, get reloads() { return reloads; } };
}
const tick = () => new Promise(resolve => setImmediate(resolve));
const questionnaire = (id = 'quiz-one', text = 'Choose a SQL query for your class project.') => ({
    questionnaireId: id,
    questions: [
        { id: 'q1', text, options: [{ id: 'a', label: 'SELECT' }, { id: 'b', label: 'DELETE' }] },
        { id: 'q2', text: 'Which clause filters query results?', options: [{ id: 'c', label: 'WHERE' }, { id: 'd', label: 'ORDER BY' }] }
    ]
});
async function reply(request, data, ok = true) {
    request.resolve({ ok, json: async () => data }); await tick();
}
async function answerAll(ui) {
    const inputs = ui.questions.querySelectorAll('input');
    await inputs[0].fire('change'); await inputs[2].fire('change');
}

test('Auto-quiz sends anti-forgery and only the student skill, then requires every answer', async () => {
    const page = setup(true);
    assert.equal(page.dialog.open, true);
    assert.equal(page.body.style.overflow, 'hidden');
    assert.equal(page.requests.length, 1);
    const request = page.requests[0];
    assert.equal(request.url, '/Student/Skills/Assessment/Generate');
    assert.equal(request.options.headers.RequestVerificationToken, 'csrf-fixture');
    assert.deepEqual(JSON.parse(request.options.body), { skillId: 1, skillName: 'SQL', language: 'en' });
    await reply(request, { success: true, questionnaire: questionnaire() });
    assert.equal(page.ui['submit-quiz'].disabled, true);
    await page.ui.questions.querySelectorAll('input')[0].fire('change');
    assert.equal(page.ui.progress.textContent, '1/2 answered');
    assert.equal(page.ui['submit-quiz'].disabled, true);
    await page.ui.questions.querySelectorAll('input')[2].fire('change');
    assert.equal(page.ui['submit-quiz'].disabled, false);
});

test('Changing language ignores a late response from the old language', async () => {
    const page = setup(true);
    page.ui.language.value = 'az';
    const languageChange = page.ui.language.fire('change');
    assert.equal(page.requests[0].options.signal.aborted, true);
    await reply(page.requests[1], { success: true, questionnaire: questionnaire('az-quiz', 'Yeni sual') });
    await languageChange;
    await reply(page.requests[0], { success: true, questionnaire: questionnaire('old-quiz', 'Stale question') });
    const legend = page.ui.questions.children[0].children[0];
    assert.equal(legend.children[1].textContent, 'Yeni sual');
    await answerAll(page.ui);
    const saving = page.ui['submit-quiz'].fire('click');
    assert.equal(JSON.parse(page.requests[2].options.body).questionnaireId, 'az-quiz');
    await reply(page.requests[2], { success: true, score: 50 });
    await saving;
});

test('A failed save preserves answers for retry and does not show a score or reload', async () => {
    const page = setup(true);
    await reply(page.requests[0], { success: true, questionnaire: questionnaire() });
    await answerAll(page.ui);
    const firstSave = page.ui['submit-quiz'].fire('click');
    assert.equal(page.ui['submit-quiz'].disabled, true);
    await page.close.fire('click');
    assert.equal(page.dialog.open, true, 'Do not dismiss a save in flight');
    await reply(page.requests[1], { success: false, message: 'Please retry saving.' }, false);
    await firstSave;
    assert.equal(page.ui.error.hidden, false);
    assert.equal(page.ui['error-copy'].textContent, 'Please retry saving.');
    assert.equal(page.ui.result.hidden, true);
    assert.equal(page.ui.questions.querySelectorAll('input').length, 4);
    assert.equal(page.ui['submit-quiz'].disabled, false);
    assert.equal(page.reloads, 0);
    const secondSave = page.ui['submit-quiz'].fire('click');
    assert.deepEqual(JSON.parse(page.requests[2].options.body).answers, [
        { questionId: 'q1', selectedOptionIds: ['a'] }, { questionId: 'q2', selectedOptionIds: ['c'] }
    ]);
    await reply(page.requests[2], { success: true, score: 80 });
    await secondSave;
    assert.equal(page.ui.score.textContent, '80');
    assert.equal(page.ui.result.hidden, false);
    await page.close.fire('click');
    assert.equal(page.reloads, 1);
    assert.equal(page.body.style.overflow, 'auto');
});

test('Closing generation aborts it and a late reply cannot populate a new quiz', async () => {
    const page = setup(true);
    await page.close.fire('click');
    assert.equal(page.requests[0].options.signal.aborted, true);
    assert.equal(page.reloads, 0);
    await page.trigger.fire('click');
    await reply(page.requests[0], { success: true, questionnaire: questionnaire('closed') });
    assert.equal(page.ui.questions.children.length, 0);
    await reply(page.requests[1], { success: true, questionnaire: questionnaire('current') });
    assert.equal(page.ui.questions.children.length, 2);
});

test('Incomplete generated quizzes show a retry action and never enable submission', async () => {
    const page = setup(true);
    await reply(page.requests[0], { success: true, questionnaire: { questionnaireId: 'empty', questions: [] } });
    assert.equal(page.ui.error.hidden, false);
    assert.equal(page.ui['retry-quiz'].hidden, false);
    assert.equal(page.ui['submit-quiz'].disabled, true);
    const retry = page.ui['retry-quiz'].fire('click');
    await reply(page.requests[1], { success: true, questionnaire: questionnaire() });
    await retry;
    assert.equal(page.ui.error.hidden, true);
    assert.equal(page.ui.questions.children.length, 2);
});
