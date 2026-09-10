(function (root) {
    'use strict';
    const roles = Object.freeze(['Recruiter', 'Hiring Manager', 'HR']);
    const maximumStages = 20;
    const normalizedName = name => String(name ?? '').trim().toLowerCase();
    function copyStages(stages) {
        return (stages ?? []).map(stage => ({
            stageName: String(stage.stageName ?? ''),
            hours: stage.hours ?? 0,
            responsibleRole: stage.responsibleRole ?? 'Recruiter',
            isStandard: false
        }));
    }
    function matches(left, right) {
        return left.length === right.length && left.every((stage, i) =>
            normalizedName(stage.stageName) === normalizedName(right[i].stageName)
            && String(stage.hours).trim() !== ''
            && Number(stage.hours) === Number(right[i].hours)
            && stage.responsibleRole === right[i].responsibleRole);
    }
    function moveStage(stages, index, offset) {
        const copy = copyStages(stages);
        const target = index + offset;
        if (index < 0 || index >= copy.length || target < 0 || target >= copy.length) return copy;
        const [stage] = copy.splice(index, 1);
        copy.splice(target, 0, stage);
        return copy;
    }
    function validateTemplate(template) {
        const name = String(template.name ?? '').trim();
        if (!name || name.length > 120) return 'Name must contain 1–120 characters.';
        if (String(template.description ?? '').trim().length > 1000) return 'Description must not exceed 1,000 characters.';
        const stages = template.stages;
        if (!Array.isArray(stages) || stages.length < 1 || stages.length > maximumStages) return 'Add between 1 and 20 stages.';
        const names = new Set();
        for (const stage of stages) {
            const stageName = normalizedName(stage?.stageName);
            if (!stageName || stageName.length > 100) return 'Stage names must contain 1–100 characters.';
            if (names.has(stageName)) return 'Stage names must be unique.';
            names.add(stageName);
            const hours = Number(stage.hours);
            if (stage.hours == null || String(stage.hours).trim() === '' || !Number.isInteger(hours) || hours < 0 || hours > 8760)
                return 'Allowed time must be a whole number between 0 and 8760 hours.';
            if (!roles.includes(stage.responsibleRole)) return 'Select a valid responsible role.';
        }
        return '';
    }
    const api = Object.freeze({ roles, maximumStages, copyStages, matches, moveStage, validateTemplate });
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else root.BothFindFunnels = api;
})(typeof window !== 'undefined' ? window : this);
