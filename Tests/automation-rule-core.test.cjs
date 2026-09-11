const test = require('node:test');
const assert = require('node:assert/strict');
const rules = require('../wwwroot/js/automation-rule-core.js');
const candidateLetter = { id: 'candidate-letter', audience: 'Candidate' };
const managerLetter = { id: 'manager-letter', audience: 'Hiring Manager' };
const valid = () => ({ eventType: 'StageAdvanced', targetStageName: 'Interview', letterTemplateId: candidateLetter.id, conditions: [] });
const check = rule => rules.validate('Interview invitation', rule, [candidateLetter, managerLetter]);
test('the three supported events allow a rule without optional candidate conditions', () => {
    for (const eventType of rules.events) {
        const rule = valid(); rule.eventType = eventType;
        if (eventType !== 'StageAdvanced') rule.targetStageName = '';
        assert.equal(check(rule), '');
    }
});
test('destination stage is required even if no candidate conditions remain', () => {
    for (const stage of ['', '  ', null, undefined]) {
        const rule = valid(); rule.targetStageName = stage;
        assert.match(check(rule), /destination stage/i);
    }
});
test('a stale destination cannot silently change the meaning of hired or closed events', () => {
    for (const eventType of ['CandidateHired', 'VacancyClosed']) {
        assert.match(check({ ...valid(), eventType }), /only applies/i);
        assert.match(rules.mandatory(eventType), eventType === 'CandidateHired' ? /Hired/ : /Closed/);
    }
});
test('only an existing candidate letter can be selected', () => {
    for (const letterTemplateId of ['', 'deleted-letter', managerLetter.id]) {
        assert.match(check({ ...valid(), letterTemplateId }), /candidate letter/i);
    }
});
test('all four metrics and five comparison operators can be configured', () => {
    for (const field of Object.keys(rules.fields)) for (const operator of Object.keys(rules.operators)) {
        assert.equal(check({ ...valid(), conditions: [{ field, operator, value: 5.5 }] }), '');
    }
});
test('blank, nonfinite and out of range values cannot become an accidental zero', () => {
    for (const value of ['', ' ', null, undefined, NaN, Infinity, -1, 101, 'abc']) {
        assert.notEqual(check({ ...valid(), conditions: [{ field: 'Score', operator: 'GreaterThan', value }] }), '');
    }
    assert.equal(check({ ...valid(), conditions: [{ field: 'Score', operator: 'Equal', value: 0 }] }), '');
    assert.equal(check({ ...valid(), conditions: [{ field: 'Age', operator: 'LessThan', value: 120 }] }), '');
});
test('unrecognized fields, prototype properties, operators and events are rejected', () => {
    for (const field of ['Email', '__proto__', 'constructor']) assert.notEqual(check({ ...valid(), conditions: [{ field, operator: 'Equal', value: 1 }] }), '');
    for (const operator of ['Like', '__proto__']) assert.notEqual(check({ ...valid(), conditions: [{ field: 'Age', operator, value: 1 }] }), '');
    assert.notEqual(check({ ...valid(), eventType: 'Applied' }), '');
});
test('editing or cancelling a rule does not change the displayed saved rule', () => {
    const original = { ...valid(), conditions: [{ field: 'Score', operator: 'GreaterThan', value: 70 }] };
    const copy = rules.copyRule(original); copy.conditions[0].value = 0; copy.targetStageName = 'Offer';
    assert.equal(original.conditions[0].value, 70); assert.equal(original.targetStageName, 'Interview');
});
test('name and condition count limits are enforced', () => {
    for (const name of [' ', 'x'.repeat(121)]) assert.notEqual(rules.validate(name, valid(), [candidateLetter]), '');
    const conditions = Array.from({ length: 11 }, () => ({ field: 'Score', operator: 'GreaterThan', value: 50 }));
    assert.notEqual(check({ ...valid(), conditions }), '');
    assert.equal(check({ ...valid(), conditions: conditions.slice(0, 10) }), '');
});
