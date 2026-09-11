(function(root) {
    'use strict';
    const events = Object.freeze(['StageAdvanced', 'CandidateHired', 'VacancyClosed']);
    const fields = Object.freeze({ Age: 'Age', ExperienceYears: 'Experience (years)', Score: 'Score (skill signals)', MatchScore: 'Match score' });
    const operators = Object.freeze({ GreaterThan: '>', LessThan: '<', Equal: '=', GreaterOrEqual: '≥', LessOrEqual: '≤' });
    function mandatory(eventType, stage) {
        if (eventType === 'StageAdvanced') return `Destination stage is ${stage || '…'}`;
        if (eventType === 'CandidateHired') return 'Candidate status is Hired';
        if (eventType === 'VacancyClosed') return 'Vacancy status is Closed';
        return 'Select an event first.';
    }
    function copyRule(rule) {
        return { eventType: rule?.eventType ?? '', targetStageName: rule?.targetStageName ?? '',
            letterTemplateId: rule?.letterTemplateId ?? '', conditions: (rule?.conditions ?? []).map(c => ({ ...c })) };
    }
    function validate(name, rule, letters) {
        if (!name.trim() || name.trim().length > 120) return 'Name must contain 1–120 characters.';
        if (!events.includes(rule.eventType)) return 'Select an event.';
        if (rule.eventType === 'StageAdvanced' && !rule.targetStageName?.trim()) return 'Select the destination stage. This condition is required.';
        if (rule.eventType !== 'StageAdvanced' && rule.targetStageName) return 'Destination stage only applies to stage advancement.';
        if (rule.conditions.length > 10) return 'Use at most 10 candidate conditions.';
        for (const c of rule.conditions) {
            if (!Object.hasOwn(fields, c.field) || !Object.hasOwn(operators, c.operator)) return 'Select a valid candidate condition.';
            const value = Number(c.value);
            if (c.value == null || String(c.value).trim() === '' || !Number.isFinite(value) || value < 0 || value > (c.field === 'Age' ? 120 : 100)) return 'Enter a condition value within the supported range.';
        }
        if (!letters.some(l => l.id === rule.letterTemplateId && l.audience === 'Candidate')) return 'Select an available candidate letter from Letters.';
        return '';
    }
    const api = Object.freeze({ events, fields, operators, mandatory, copyRule, validate });
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else root.BothFindAutomationRules = api;
})(typeof window !== 'undefined' ? window : this);
