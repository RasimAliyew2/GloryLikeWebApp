(() => {
    "use strict";
    const form = document.getElementById("roleEditorForm");
    if (!form) return;
    const checkboxes = Array.from(form.querySelectorAll('input[name="PermissionKeys"]'));
    const total = document.getElementById("totalRights");
    const footerTotal = document.getElementById("footerRights");
    const technicalToggle = document.getElementById("technicalKeysToggle");

    const updateCounts = () => {
        const count = checkboxes.filter((input) => input.checked).length;
        if (total) total.textContent = String(count);
        if (footerTotal) footerTotal.textContent = String(count);
        document.querySelectorAll("[data-permission-group]").forEach((group) => {
            const key = group.dataset.permissionGroup;
            const selected = group.querySelectorAll('input[name="PermissionKeys"]:checked').length;
            const target = document.querySelector('[data-group-count="' + key + '"]');
            if (target) target.textContent = String(selected);
            const button = group.querySelector("[data-toggle-group]");
            if (button) {
                const all = group.querySelectorAll('input[name="PermissionKeys"]').length;
                button.textContent = selected === all ? "Clear all" : "Select all";
            }
        });
    };
    checkboxes.forEach((input) => input.addEventListener("change", updateCounts));
    document.querySelectorAll("[data-toggle-group]").forEach((button) => {
        button.addEventListener("click", () => {
            const group = document.querySelector('[data-permission-group="' + button.dataset.toggleGroup + '"]');
            if (!group) return;
            const inputs = Array.from(group.querySelectorAll('input[name="PermissionKeys"]'));
            const shouldSelect = inputs.some((input) => !input.checked);
            inputs.forEach((input) => { input.checked = shouldSelect; });
            updateCounts();
        });
    });
    document.getElementById("selectAllRights")?.addEventListener("click", () => {
        checkboxes.forEach((input) => { input.checked = true; });
        updateCounts();
    });
    document.getElementById("clearAllRights")?.addEventListener("click", () => {
        checkboxes.forEach((input) => { input.checked = false; });
        updateCounts();
    });
    technicalToggle?.addEventListener("change", () => {
        document.body.classList.toggle("show-technical-keys", technicalToggle.checked);
    });
    updateCounts();
})();
