(() => {
    'use strict';
    const core = window.BothFindFunnels;
    const templates = JSON.parse(document.getElementById('companyFunnelsData').textContent);
    const { canManageTemplates: canManage } = JSON.parse(document.getElementById('companyFunnelsContext').textContent);
    const dialog = document.getElementById('funnelDialog');
    const form = document.getElementById('funnelTemplateForm');
    const list = document.getElementById('templateStageList');
    const name = document.getElementById('funnelName');
    const description = document.getElementById('funnelDescription');
    const add = document.getElementById('addTemplateStage');
    const save = document.getElementById('saveFunnel');
    const remove = document.getElementById('deleteFunnel');
    const error = document.getElementById('funnelEditorError');
    let templateId = null, stages = [], busy = false;
    const showError = message => { error.textContent = message; error.hidden = !message; };
    const action = (text, label, onClick, disabled = false) => {
        const button = document.createElement('button');
        button.type = 'button'; button.textContent = text; button.title = label;
        button.setAttribute('aria-label', label); button.disabled = disabled || !canManage;
        button.addEventListener('click', onClick); return button;
    };
    function render(focusIndex) {
        list.replaceChildren();
        stages.forEach((stage, index) => {
            const row = document.createElement('div'); row.className = 'funnel-editor-row';
            const moves = document.createElement('div'); moves.className = 'funnel-editor-moves';
            const move = offset => {
                stages = core.moveStage(stages, index, offset);
                render(index + offset);
            };
            moves.append(action('↑', 'Move stage up', () => move(-1), index === 0),
                action('↓', 'Move stage down', () => move(1), index === stages.length - 1));
            row.append(moves);
            function field(label, key, type) {
                const wrapper = document.createElement('label'); const caption = document.createElement('span');
                caption.textContent = label; const input = document.createElement('input');
                input.type = type; input.value = stage[key]; input.required = true; input.disabled = !canManage;
                input.setAttribute('aria-label', `Stage ${index + 1}: ${label}`);
                if (type === 'number') { input.min = '0'; input.max = '8760'; input.step = '1'; }
                else input.maxLength = 100;
                input.addEventListener('input', () => { stage[key] = input.value; });
                wrapper.append(caption, input); row.append(wrapper);
            }
            field('Stage name', 'stageName', 'text'); field('Hours', 'hours', 'number');
            const wrapper = document.createElement('label'); const caption = document.createElement('span');
            caption.textContent = 'Responsible role'; const role = document.createElement('select');
            role.disabled = !canManage; role.setAttribute('aria-label', `Stage ${index + 1}: Responsible role`);
            core.roles.forEach(value => role.add(new Option(value, value)));
            role.value = stage.responsibleRole;
            role.addEventListener('change', () => { stage.responsibleRole = role.value; });
            wrapper.append(caption, role); row.append(wrapper);
            row.append(action('×', `Remove stage ${index + 1}`, () => { stages.splice(index, 1); render(); }));
            list.append(row);
        });
        add.disabled = stages.length >= 20 || !canManage;
        if (Number.isInteger(focusIndex)) list.children[focusIndex]?.querySelector('input')?.focus();
    }
    function open(template) {
        templateId = template?.id ?? null;
        // Copy every stage: cancel and unsaved edits cannot mutate the library.
        stages = template ? core.copyStages(template.stages) : [{ stageName: 'Applied', hours: 48, responsibleRole: 'Recruiter' }];
        name.value = template?.name ?? ''; description.value = template?.description ?? '';
        name.disabled = description.disabled = !canManage;
        remove.hidden = !canManage || !template; save.hidden = add.hidden = !canManage;
        document.getElementById('funnelDialogTitle').textContent = template ? 'Funnel template' : 'New funnel template';
        showError(''); render(); dialog.showModal(); document.body.classList.add('template-modal-open'); name.focus();
    }
    document.querySelectorAll('[data-new-funnel]').forEach(button => button.addEventListener('click', () => open(null)));
    document.querySelectorAll('[data-funnel-id]').forEach(button => button.addEventListener('click', () => open(templates.find(item => item.id === button.dataset.funnelId))));
    document.querySelectorAll('[data-close-funnel]').forEach(button => button.addEventListener('click', () => { if (!busy) dialog.close(); }));
    dialog.addEventListener('cancel', event => { if (busy) event.preventDefault(); });
    dialog.addEventListener('close', () => document.body.classList.remove('template-modal-open'));
    add.addEventListener('click', () => {
        if (!canManage || stages.length >= 20) return;
        stages.push({ stageName: '', hours: 24, responsibleRole: 'Recruiter' }); render(stages.length - 1);
    });
    async function send(deleting) {
        if (busy || !canManage) return;
        if (!deleting) {
            if (!form.reportValidity()) return;
            const message = core.validateTemplate({ name: name.value, description: description.value, stages });
            if (message) { showError(message); return; }
        }
        const body = new FormData(form);
        if (!deleting) stages.forEach((stage, index) => {
            body.set(`Stages[${index}].StageName`, stage.stageName.trim());
            body.set(`Stages[${index}].Hours`, stage.hours);
            body.set(`Stages[${index}].ResponsibleRole`, stage.responsibleRole);
        });
        const url = '/Employer/Company/Templates/Funnels' + (templateId ? `/${templateId}/${deleting ? 'Delete' : 'Update'}` : '');
        busy = true; showError('');
        const controls = [...dialog.querySelectorAll('button,input,select')];
        const disabled = controls.map(control => control.disabled); controls.forEach(control => control.disabled = true);
        try {
            const response = await fetch(url, { method: 'POST', body, credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
            const data = await response.json();
            if (!response.ok || !data.success) throw new Error(data.message || 'The template could not be saved.');
            window.location.reload();
        } catch (failure) {
            showError(failure instanceof SyntaxError ? 'Your session may have expired. Reload the page and try again.' : failure.message);
        } finally {
            busy = false; controls.forEach((control, i) => control.disabled = disabled[i]);
        }
    }
    form.addEventListener('submit', event => { event.preventDefault(); send(false); });
    remove.addEventListener('click', () => {
        if (templateId && window.confirm('Delete this template for your company? Existing vacancies and other companies are unaffected.')) send(true);
    });
})();
