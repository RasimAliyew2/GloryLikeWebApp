(() => {
    'use strict';
    const core = window.BothFindAutomationRules;
    const data = JSON.parse(document.getElementById('companyAutomationsData').textContent);
    const canManage = data.canManageTemplates;
    const letters = data.letters.filter(l => l.audience === 'Candidate');
    const dialog = document.getElementById('automationDialog');
    const form = document.getElementById('automationForm');
    const name = document.getElementById('automationName');
    const enabled = document.getElementById('automationEnabled');
    const eventSelect = document.getElementById('automationEvent');
    const letterSelect = document.getElementById('automationLetter');
    const conditionList = document.getElementById('candidateConditions');
    const add = document.getElementById('addAutomationCondition');
    const remove = document.getElementById('deleteAutomation');
    const error = document.getElementById('automationError');
    const save = document.getElementById('saveAutomation');
    let id = null, rule = core.copyRule(), busy = false;
    function message(text) { error.textContent = text; error.hidden = !text; }
    function renderMandatory() {
        const container = document.getElementById('eventCondition'); container.replaceChildren();
        const label = document.createElement('label'); label.className = 'mandatory-condition';
        const text = document.createElement('span'); text.textContent = '🔒 ' + (rule.eventType === 'StageAdvanced' ? 'Destination stage is' : core.mandatory(rule.eventType));
        label.append(text);
        if (rule.eventType === 'StageAdvanced') {
            const stage = document.createElement('select'); stage.name = 'Rule.TargetStageName'; stage.required = true;
            stage.setAttribute('aria-label', 'Required destination stage'); stage.add(new Option('Select destination stage…', ''));
            const names = [...new Set([...data.stageNames, ...(rule.targetStageName ? [rule.targetStageName] : [])])];
            names.forEach(value => stage.add(new Option(value, value))); stage.value = rule.targetStageName; stage.disabled = !canManage;
            stage.addEventListener('change', () => { rule.targetStageName = stage.value; }); label.append(stage);
        }
        container.append(label);
        if (rule.eventType) {
            const help = document.createElement('small'); help.textContent = 'Required by the selected event. This condition cannot be removed.'; container.append(help);
        }
    }
    function renderConditions() {
        conditionList.replaceChildren();
        rule.conditions.forEach((condition, index) => {
            const row = document.createElement('div'); row.className = 'automation-condition-row';
            function select(options, value, label, change) {
                const node = document.createElement('select'); node.setAttribute('aria-label', `${label} ${index + 1}`); node.disabled = !canManage;
                Object.entries(options).forEach(([key, text]) => node.add(new Option(text, key)));
                node.value = value; node.addEventListener('change', () => change(node.value)); return node;
            }
            const field = select(core.fields, condition.field, 'Candidate field', value => { condition.field = value; input.max = value === 'Age' ? '120' : '100'; });
            const op = select(core.operators, condition.operator, 'Comparison', value => { condition.operator = value; });
            const input = document.createElement('input'); input.type = 'number'; input.required = true; input.min = '0'; input.max = condition.field === 'Age' ? '120' : '100'; input.step = '.01';
            input.value = condition.value; input.disabled = !canManage; input.setAttribute('aria-label', `Condition value ${index + 1}`);
            input.addEventListener('input', () => { condition.value = input.value; });
            const del = document.createElement('button'); del.type = 'button'; del.textContent = '×'; del.disabled = !canManage;
            del.setAttribute('aria-label', `Remove candidate condition ${index + 1}`);
            del.addEventListener('click', () => { rule.conditions.splice(index, 1); renderConditions(); });
            row.append(field, op, input, del); conditionList.append(row);
        });
        add.disabled = !canManage || rule.conditions.length >= 10;
    }
    function renderLetter() {
        const preview = document.getElementById('automationLetterPreview'); preview.replaceChildren();
        const letter = letters.find(l => l.id === rule.letterTemplateId); preview.hidden = !letter;
        if (!letter) return;
        const subject = document.createElement('strong'); subject.textContent = letter.subject;
        const body = document.createElement('p'); body.textContent = letter.body; preview.append(subject, body);
    }
    function open(template) {
        id = template?.id ?? null; rule = core.copyRule(template?.rule); name.value = template?.name ?? ''; enabled.checked = template?.isEnabled ?? true;
        eventSelect.value = rule.eventType; letterSelect.replaceChildren(new Option('Select a candidate letter…', ''));
        letters.forEach(l => letterSelect.add(new Option(l.name, l.id)));
        if (rule.letterTemplateId && !letters.some(l => l.id === rule.letterTemplateId)) {
            const missing = new Option('Letter unavailable — choose another', rule.letterTemplateId); missing.disabled = true; letterSelect.add(missing);
        }
        letterSelect.value = rule.letterTemplateId;
        [name, enabled, eventSelect, letterSelect].forEach(element => element.disabled = !canManage);
        remove.hidden = !id || !canManage; save.hidden = add.hidden = !canManage;
        document.getElementById('automationTitle').textContent = template ? 'Automation template' : 'New automation';
        message(''); renderMandatory(); renderConditions(); renderLetter(); dialog.showModal(); document.body.classList.add('template-modal-open'); name.focus();
    }
    document.querySelectorAll('[data-new-automation]').forEach(button => button.addEventListener('click', () => open(null)));
    document.querySelectorAll('[data-automation-id]').forEach(button => button.addEventListener('click', () => open(data.templates.find(t => t.id === button.dataset.automationId))));
    document.querySelectorAll('[data-close-automation]').forEach(button => button.addEventListener('click', () => { if (!busy) dialog.close(); }));
    dialog.addEventListener('cancel', e => { if (busy) e.preventDefault(); });
    dialog.addEventListener('close', () => document.body.classList.remove('template-modal-open'));
    eventSelect.addEventListener('change', () => { rule.eventType = eventSelect.value; rule.targetStageName = ''; renderMandatory(); });
    letterSelect.addEventListener('change', () => { rule.letterTemplateId = letterSelect.value; renderLetter(); });
    add.addEventListener('click', () => { if (rule.conditions.length < 10) { rule.conditions.push({ field: 'Age', operator: 'GreaterThan', value: '' }); renderConditions(); } });
    const token = form.querySelector('[name="__RequestVerificationToken"]').value;
    async function post(url, body) {
        body.set('__RequestVerificationToken', token);
        const response = await fetch(url, { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
        let result;
        try { result = await response.json(); } catch { throw new Error('Your session may have expired. Reload and try again.'); }
        if (!response.ok || !result.success) throw new Error(result.message || 'The request could not be completed.');
        return result;
    }
    async function persist(deleting) {
        if (busy || !canManage) return;
        if (!deleting) {
            if (!form.reportValidity()) return;
            const failure = core.validate(name.value, rule, letters); if (failure) { message(failure); return; }
        }
        const body = new FormData();
        body.set('Name', name.value.trim()); body.set('IsEnabled', String(enabled.checked));
        body.set('Rule.EventType', rule.eventType); body.set('Rule.TargetStageName', rule.targetStageName || ''); body.set('Rule.LetterTemplateId', rule.letterTemplateId);
        rule.conditions.forEach((c, index) => Object.entries(c).forEach(([key, value]) => body.set(`Rule.Conditions[${index}].${key}`, String(value))));
        busy = true; message(''); const controls = [...form.querySelectorAll('button,input,select')]; const disabled = controls.map(c => c.disabled); controls.forEach(c => c.disabled = true);
        try { await post('/Employer/Company/Templates/Automations' + (id ? `/${id}/${deleting ? 'Delete' : 'Update'}` : ''), body); location.reload(); }
        catch (ex) { message(ex.message); }
        finally { busy = false; controls.forEach((c, i) => c.disabled = disabled[i]); }
    }
    form.addEventListener('submit', e => { e.preventDefault(); persist(false); });
    remove.addEventListener('click', () => { if (confirm('Delete this template? Existing vacancy copies will keep working.')) persist(true); });
    document.getElementById('refreshDeliveries')?.addEventListener('click', () => location.reload());
    document.querySelectorAll('[data-retry-id]').forEach(button => button.addEventListener('click', async () => {
        const warning = button.dataset.retryStatus === 'Uncertain' ? 'The previous email may already have been sent. Check the mailbox first. Send another attempt?' : 'Retry sending this email to the candidate?';
        if (!confirm(warning)) return; button.disabled = true;
        try { await post(`/Employer/Company/Templates/Automations/Deliveries/${button.dataset.retryId}/Retry`, new FormData()); location.reload(); }
        catch (ex) { const status = document.getElementById('automationPageMessage'); status.textContent = ex.message; status.hidden = false; button.disabled = false; }
    }));
})();
