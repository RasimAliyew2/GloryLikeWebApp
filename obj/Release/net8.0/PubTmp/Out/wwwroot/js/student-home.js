(function (root) {
    'use strict';
    function animatedValue(target, elapsed, duration = 500) {
        const value = Number.isFinite(Number(target)) ? Math.max(0, Number(target)) : 0;
        return value * Math.min(1, Math.max(0, elapsed / Math.max(1, duration)));
    }
    if (typeof module !== 'undefined' && module.exports) { module.exports = { animatedValue }; return; }
    const numbers = [...document.querySelectorAll('[data-st-number]')];
    const rings = [...document.querySelectorAll('[data-st-ring]')];
    const bars = [...document.querySelectorAll('[data-st-bar]')];
    function paint(elapsed) {
        numbers.forEach(el => el.textContent = String(Math.round(animatedValue(Number(el.dataset.stNumber), elapsed))));
        rings.forEach(el => el.setAttribute('stroke-dashoffset', String(100 - animatedValue(Number(el.dataset.stRing), elapsed))));
        bars.forEach(el => el.style.width = animatedValue(Number(el.dataset.stBar), elapsed) + '%');
    }
    if (root.matchMedia('(prefers-reduced-motion: reduce)').matches) paint(500);
    else {
        paint(0); const start = performance.now();
        function frame(now) { paint(now - start); if (now - start < 500) requestAnimationFrame(frame); }
        requestAnimationFrame(frame);
    }
    const menu = document.getElementById('studentMenuButton');
    const backdrop = document.getElementById('studentBackdrop');
    function setMenu(open) { document.body.classList.toggle('menu-open', open); menu?.setAttribute('aria-expanded', String(open)); if (backdrop) backdrop.hidden = !open; }
    menu?.addEventListener('click', () => setMenu(!document.body.classList.contains('menu-open')));
    backdrop?.addEventListener('click', () => setMenu(false));
    document.querySelectorAll('.student-nav a').forEach(a => a.addEventListener('click', () => setMenu(false)));
    document.addEventListener('keydown', e => { if (e.key === 'Escape') setMenu(false); });
    root.matchMedia('(min-width: 961px)').addEventListener('change', e => { if (e.matches) setMenu(false); });
    document.querySelectorAll('[data-open-student-dialog]').forEach(button => button.addEventListener('click', () => {
        const dialog = document.getElementById(button.dataset.openStudentDialog); if (dialog && !dialog.open) dialog.showModal();
    }));
    document.querySelectorAll('[data-close-student-dialog]').forEach(button => button.addEventListener('click', () => button.closest('dialog')?.close()));
    const form = document.getElementById('studentEducationForm');
    let saving = false;
    form?.addEventListener('submit', async e => {
        e.preventDefault(); if (saving || !form.reportValidity()) return;
        const error = document.getElementById('studentEducationError'); error.hidden = true;
        const button = form.querySelector('[type="submit"]'); const body = new FormData(form);
        saving = true; button.disabled = true; button.textContent = 'Saving…';
        try {
            const response = await fetch(form.action, { method: 'POST', body, credentials: 'same-origin', headers: { Accept: 'application/json' } });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error(result.message || 'Education could not be saved. Please try again.');
            location.hash = 'education'; location.reload();
        } catch (ex) {
            error.textContent = ex instanceof SyntaxError ? 'Your session may have expired. Reload and try again.' : ex.message;
            error.hidden = false;
        } finally { saving = false; button.disabled = false; button.textContent = 'Save education'; }
    });
})(typeof window !== 'undefined' ? window : globalThis);
