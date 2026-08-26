(() => {
    "use strict";

    const settingsToggle = document.getElementById("reportSettingsToggle");
    const settings = document.getElementById("reportSettings");

    if (settingsToggle && settings) {
        settingsToggle.addEventListener("click", () => {
            const willOpen = settings.hidden;
            settings.hidden = !willOpen;
            settingsToggle.setAttribute("aria-expanded", String(willOpen));
            settingsToggle.textContent = willOpen
                ? "⚙ Settings"
                : "⚙ Show settings";
        });
    }

    const vacancyField = document.getElementById("showVacanciesField");
    const vacancyChildSettings = Array.from(
        document.querySelectorAll(".vacancy-child-setting"));

    const syncVacancyChildren = () => {
        if (!vacancyField) return;

        vacancyChildSettings.forEach(node => {
            node.classList.toggle("is-disabled", !vacancyField.checked);
            node.querySelectorAll("input:not([disabled])").forEach(input => {
                input.disabled = !vacancyField.checked;
            });
        });
    };

    vacancyField?.addEventListener("change", syncVacancyChildren);
    syncVacancyChildren();

    document.querySelectorAll(".report-row-toggle").forEach(button => {
        button.addEventListener("click", () => {
            const group = button.dataset.reportGroup;
            if (!group) return;

            const rows = Array.from(document.querySelectorAll(`.${group}`));
            const willExpand = button.getAttribute("aria-expanded") !== "true";
            rows.forEach(row => {
                row.hidden = !willExpand;
            });
            button.setAttribute("aria-expanded", String(willExpand));
            button.textContent = willExpand ? "−" : "+";
        });
    });

    const openCustomValue = target => {
        const href = target?.dataset?.reportHref;
        if (href) window.location.assign(href);
    };

    document.addEventListener("dblclick", event => {
        const target = event.target.closest("[data-report-href]");
        if (!target) return;
        event.preventDefault();
        openCustomValue(target);
    });

    document.addEventListener("keydown", event => {
        if (event.key !== "Enter") return;
        const target = event.target.closest("[data-report-href]");
        if (!target) return;
        event.preventDefault();
        openCustomValue(target);
    });
})();
