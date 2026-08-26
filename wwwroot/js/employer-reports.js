(() => {
    "use strict";

    const settingsToggle = document.getElementById("reportSettingsToggle");
    const settings = document.getElementById("reportSettings");

    if (settingsToggle && settings) {
        settings.hidden = true;
        settingsToggle.setAttribute("aria-expanded", "false");

        settingsToggle.addEventListener("click", () => {
            const willOpen = settings.hidden;
            settings.hidden = !willOpen;
            settingsToggle.setAttribute("aria-expanded", String(willOpen));
            settingsToggle.textContent = willOpen
                ? "⚙ Hide settings"
                : "⚙ Settings";
        });
    }

    const hierarchy = document.getElementById("reportHierarchy");
    const hierarchyLayout = document.getElementById("reportHierarchyLayout");
    const reportForm = document.getElementById("vacancyReportForm");
    let draggedField = null;

    const fieldLists = () => Array.from(
        hierarchy?.querySelectorAll("[data-hierarchy-field-list]") ?? []);

    const fieldsIn = list => Array.from(
        list?.querySelectorAll(":scope > [data-hierarchy-field]") ?? []);

    const canMoveTo = (field, targetList) => {
        if (!field || !targetList) return false;

        const sourceList = field.closest("[data-hierarchy-field-list]");
        if (!sourceList) return false;

        const fixedScope = field.dataset.fixedScope;
        if (fixedScope && fixedScope !== targetList.dataset.scope) return false;

        return sourceList === targetList || fieldsIn(sourceList).length > 1;
    };

    const serializeHierarchy = () => {
        if (!hierarchyLayout) return;

        hierarchyLayout.value = fieldLists()
            .map(list => {
                const keys = fieldsIn(list)
                    .map(field => field.dataset.fieldKey)
                    .filter(Boolean);
                return `${list.dataset.scope}:${keys.join(",")}`;
            })
            .join("|");
    };

    const refreshMoveButtons = () => {
        const lists = fieldLists();

        lists.forEach((list, levelIndex) => {
            const fields = fieldsIn(list);
            fields.forEach((field, fieldIndex) => {
                const fixedScope = field.dataset.fixedScope;
                const previousLevel = lists[levelIndex - 1];
                const nextLevel = lists[levelIndex + 1];

                const left = field.querySelector("[data-hierarchy-action='left']");
                const right = field.querySelector("[data-hierarchy-action='right']");
                const up = field.querySelector("[data-hierarchy-action='up']");
                const down = field.querySelector("[data-hierarchy-action='down']");

                if (left) left.disabled = fieldIndex === 0;
                if (right) right.disabled = fieldIndex === fields.length - 1;
                if (up) {
                    up.disabled = !previousLevel
                        || fields.length <= 1
                        || (fixedScope && fixedScope !== previousLevel.dataset.scope);
                }
                if (down) {
                    down.disabled = !nextLevel
                        || fields.length <= 1
                        || (fixedScope && fixedScope !== nextLevel.dataset.scope);
                }
            });
        });
    };

    const commitHierarchyChange = () => {
        serializeHierarchy();
        refreshMoveButtons();
    };

    if (hierarchy) {
        hierarchy.addEventListener("dragstart", event => {
            const field = event.target.closest("[data-hierarchy-field]");
            if (!field) return;

            draggedField = field;
            field.classList.add("is-dragging");
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.setData("text/plain", field.dataset.fieldKey ?? "");
        });

        fieldLists().forEach(list => {
            list.addEventListener("dragenter", event => {
                if (!canMoveTo(draggedField, list)) return;
                event.preventDefault();
                list.classList.add("is-drop-target");
            });

            list.addEventListener("dragover", event => {
                if (!canMoveTo(draggedField, list)) return;
                event.preventDefault();
                event.dataTransfer.dropEffect = "move";

                const target = event.target.closest("[data-hierarchy-field]");
                if (!target || target === draggedField || target.parentElement !== list) {
                    list.appendChild(draggedField);
                    return;
                }

                const rect = target.getBoundingClientRect();
                const sameVisualRow = event.clientY >= rect.top
                    && event.clientY <= rect.bottom;
                const placeBefore = sameVisualRow
                    ? event.clientX < rect.left + rect.width / 2
                    : event.clientY < rect.top + rect.height / 2;
                list.insertBefore(
                    draggedField,
                    placeBefore ? target : target.nextSibling);
            });

            list.addEventListener("dragleave", event => {
                if (!list.contains(event.relatedTarget))
                    list.classList.remove("is-drop-target");
            });

            list.addEventListener("drop", event => {
                if (!canMoveTo(draggedField, list)) return;
                event.preventDefault();
                list.classList.remove("is-drop-target");
                commitHierarchyChange();
            });
        });

        hierarchy.addEventListener("dragend", () => {
            draggedField?.classList.remove("is-dragging");
            fieldLists().forEach(list => list.classList.remove("is-drop-target"));
            draggedField = null;
            commitHierarchyChange();
        });

        hierarchy.addEventListener("click", event => {
            const button = event.target.closest("[data-hierarchy-action]");
            if (!button) return;

            const field = button.closest("[data-hierarchy-field]");
            const sourceList = field?.closest("[data-hierarchy-field-list]");
            if (!field || !sourceList) return;

            const action = button.dataset.hierarchyAction;
            if (action === "left") {
                const previous = field.previousElementSibling;
                if (previous) sourceList.insertBefore(field, previous);
            } else if (action === "right") {
                const next = field.nextElementSibling;
                if (next) sourceList.insertBefore(next, field);
            } else {
                const lists = fieldLists();
                const sourceIndex = lists.indexOf(sourceList);
                const targetIndex = action === "up"
                    ? sourceIndex - 1
                    : sourceIndex + 1;
                const targetList = lists[targetIndex];
                if (canMoveTo(field, targetList)) targetList.appendChild(field);
            }

            commitHierarchyChange();
        });

        commitHierarchyChange();
    }

    reportForm?.addEventListener("submit", serializeHierarchy);

    document.querySelectorAll(".report-row-toggle").forEach(button => {
        button.addEventListener("click", () => {
            const group = button.dataset.reportGroup;
            if (!group) return;

            const rows = Array.from(
                document.querySelectorAll("[data-report-group-row]"))
                .filter(row => row.dataset.reportGroupRow === group);
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
        if (!href) return;

        if (target.dataset.reportTarget === "_blank") {
            const newWindow = window.open(href, "_blank", "noopener,noreferrer");
            if (newWindow) newWindow.opener = null;
            return;
        }

        window.location.assign(href);
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
