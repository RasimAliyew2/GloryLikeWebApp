(() => {
    const core = window.BothFindFunnels;
    const initialState =
        window.gloryLikeVacancyInitialState
        ?? {};

    const stageList =
        document.getElementById(
            "funnelStageList");

    const emptyState =
        document.getElementById(
            "funnelEmptyState");

    const addButton =
        document.getElementById(
            "addFunnelStageButton");

    const stageCount =
        document.getElementById(
            "funnelStageCount");

    const totalHours =
        document.getElementById(
            "funnelTotalHours");

    const vacancyForm =
        document.getElementById(
            "createVacancyForm");

    const nextButton =
        document.getElementById(
            "nextStepButton");

    const funnelStage =
        document.querySelector(
            '[data-stage="3"]');

    const templateButtons = Array.from(
        document.querySelectorAll(
            "[data-funnel-template]"));

    if (!stageList || !addButton)
        return;

    const maximumStageCount = 20;
    const maximumHours = 8760;

    const templates = Object.fromEntries(
        (initialState.funnelTemplates ?? []).map(template => [template.id, template.stages]));

    const normalize = value =>
        String(value ?? "")
            .trim()
            .toLocaleLowerCase();

    const normalizeStage = stage => {
        return {
            stageName: String(
                stage?.stageName
                ?? stage?.StageName
                ?? ""),
            hours: String(
                stage?.hours
                ?? stage?.Hours
                ?? 0),
            responsibleRole: stage?.responsibleRole ?? stage?.ResponsibleRole ?? "Recruiter",
            isStandard: false
        };
    };

    const createTemplateStages = templateName => core.copyStages(templates[templateName] ?? []).map(normalizeStage);

    const hasInitialStages = Array.isArray(
        initialState.funnelStages);

    let stages = hasInitialStages
        ? initialState.funnelStages
            .slice(0, maximumStageCount + 1)
            .map(normalizeStage)
        : [];

    const getNumericHours = stage => {
        if (!String(stage.hours).trim())
            return 0;

        const hours = Number(stage.hours);

        return Number.isInteger(hours)
            && hours >= 0
            && hours <= maximumHours
            ? hours
            : 0;
    };

    const updateTemplateSelection = () => {
        let selectedTemplate = "";

        Object.entries(templates).some(
            ([templateName, templateStages]) => {
                const matches = core.matches(stages, templateStages);

                if (matches)
                    selectedTemplate = templateName;

                return matches;
            });

        templateButtons.forEach(button => {
            button.setAttribute("aria-pressed", String(button.dataset.funnelTemplate === selectedTemplate));
            button.classList.toggle(
                "active",
                button.dataset.funnelTemplate
                    === selectedTemplate);
        });
    };

    const updateSummary = () => {
        const hours = stages.reduce(
            (total, stage) =>
                total + getNumericHours(stage),
            0);

        if (stageCount)
            stageCount.textContent = String(stages.length);

        if (totalHours)
            totalHours.textContent = String(hours);

        const review =
            document.getElementById(
                "reviewFunnelStages");

        if (review) {
            review.textContent =
                `${stages.length} stages / ${hours} h`;
        }

        updateTemplateSelection();
    };

    const moveStage = (index, offset) => {
        const targetIndex = index + offset;

        if (
            targetIndex < 0
            || targetIndex >= stages.length
        ) {
            return;
        }

        stages = core.moveStage(stages, index, offset).map(normalizeStage);
        renderStages(targetIndex, false);
    };

    const createIconButton = (
        text,
        title,
        className,
        clickHandler,
        disabled = false) => {

        const button = document.createElement("button");
        button.type = "button";
        button.className =
            `funnel-stage-action ${className}`;
        button.textContent = text;
        button.title = title;
        button.setAttribute("aria-label", title);
        button.disabled = disabled;
        button.addEventListener("click", clickHandler);

        return button;
    };

    const createStageRow = (stage, index) => {
        const row = document.createElement("article");
        row.className = "funnel-stage-row";
        row.dataset.funnelStageIndex = String(index);

        const sequence = document.createElement("span");
        sequence.className = "funnel-stage-sequence";
        sequence.textContent = String(index + 1);

        const fields = document.createElement("div");
        fields.className = "funnel-stage-fields";

        const nameField = document.createElement("label");
        nameField.className = "funnel-stage-name-field";

        const nameLabel = document.createElement("span");
        nameLabel.textContent = "Stage name";

        const nameInput = document.createElement("input");
        nameInput.type = "text";
        nameInput.name =
            `Input.FunnelStages[${index}].StageName`;
        nameInput.value = stage.stageName;
        nameInput.maxLength = 100;
        nameInput.required = true;
        nameInput.placeholder = "Stage name";
        nameInput.setAttribute(
            "aria-label",
            `Funnel stage ${index + 1} name`);

        nameInput.addEventListener("input", () => {
            stage.stageName = nameInput.value;
            updateSummary();
        });

        nameField.append(nameLabel, nameInput);

        const hoursField = document.createElement("label");
        hoursField.className = "funnel-stage-hours-field";

        const hoursLabel = document.createElement("span");
        hoursLabel.textContent = "Allowed time";

        const hoursShell = document.createElement("span");
        hoursShell.className = "funnel-stage-hours-shell";

        const hoursInput = document.createElement("input");
        hoursInput.type = "number";
        hoursInput.name =
            `Input.FunnelStages[${index}].Hours`;
        hoursInput.value = stage.hours;
        hoursInput.min = "0";
        hoursInput.max = String(maximumHours);
        hoursInput.step = "1";
        hoursInput.required = true;
        hoursInput.setAttribute(
            "aria-label",
            `Allowed hours for funnel stage ${index + 1}`);

        hoursInput.addEventListener("input", () => {
            stage.hours = hoursInput.value;
            updateSummary();
        });

        const hoursUnit = document.createElement("span");
        hoursUnit.textContent = "h";

        hoursShell.append(hoursInput, hoursUnit);
        hoursField.append(hoursLabel, hoursShell);

        const standardInput = document.createElement("input");
        standardInput.type = "hidden";
        standardInput.name =
            `Input.FunnelStages[${index}].IsStandard`;
        standardInput.value = stage.isStandard
            ? "true"
            : "false";

        const roleField = document.createElement("label");
        roleField.className = "funnel-stage-role-field";
        const roleLabel = document.createElement("span");
        roleLabel.textContent = "Responsible role";
        const roleSelect = document.createElement("select");
        roleSelect.name = `Input.FunnelStages[${index}].ResponsibleRole`;
        roleSelect.setAttribute("aria-label", `Responsible role for stage ${index + 1}`);
        core.roles.forEach(role => roleSelect.add(new Option(role, role)));
        roleSelect.value = stage.responsibleRole;
        roleSelect.addEventListener("change", () => { stage.responsibleRole = roleSelect.value; updateSummary(); });
        roleField.append(roleLabel, roleSelect);

        fields.append(
            nameField,
            hoursField,
            roleField,
            standardInput);

        const actions = document.createElement("div");
        actions.className = "funnel-stage-actions";

        actions.append(
            createIconButton(
                "↑",
                "Move stage up",
                "move",
                () => moveStage(index, -1),
                index === 0),
            createIconButton(
                "↓",
                "Move stage down",
                "move",
                () => moveStage(index, 1),
                index === stages.length - 1),
            createIconButton(
                "×",
                "Delete stage",
                "delete",
                () => {
                    stages.splice(index, 1);
                    renderStages();
                }));

        row.append(
            sequence,
            fields,
            actions);

        return row;
    };

    function renderStages(
        focusIndex = null,
        selectName = true) {

        stageList.replaceChildren();

        stages.forEach((stage, index) => {
            stageList.appendChild(
                createStageRow(stage, index));

            if (index < stages.length - 1) {
                const connector =
                    document.createElement("div");

                connector.className =
                    "funnel-stage-connector";
                connector.setAttribute(
                    "aria-hidden",
                    "true");
                connector.textContent = "↓";
                stageList.appendChild(connector);
            }
        });

        if (emptyState) {
            emptyState.hidden =
                stages.length > 0;
        }

        const limitReached =
            stages.length >= maximumStageCount;

        addButton.disabled = limitReached;
        addButton.title = limitReached
            ? "Maximum 20 stages"
            : "Add a custom funnel stage";

        updateSummary();

        if (Number.isInteger(focusIndex)) {
            window.setTimeout(() => {
                const input = stageList
                    .querySelector(
                        `[data-funnel-stage-index="${focusIndex}"] `
                        + ".funnel-stage-name-field input");

                input?.focus();

                if (selectName)
                    input?.select();
            }, 0);
        }
    }

    const addStage = () => {
        if (stages.length >= maximumStageCount)
            return;

        const hiredIndex = stages.findIndex(
            stage =>
                normalize(stage.stageName)
                    === "hired");

        const insertIndex = hiredIndex >= 0
            ? hiredIndex
            : stages.length;

        stages.splice(insertIndex, 0, {
            stageName: "New stage",
            hours: "24",
            responsibleRole: "Recruiter",
            isStandard: false
        });

        renderStages(insertIndex);
    };

    const focusInvalidStage = (
        index,
        selector) => {

        document
            .querySelector(
                '[data-step-target="3"]')
            ?.click();

        window.setTimeout(() => {
            stageList
                .querySelector(
                    `[data-funnel-stage-index="${index}"] `
                    + selector)
                ?.focus();
        }, 0);
    };

    const validateStages = () => {
        if (stages.length === 0) {
            window.alert(
                "Add at least one funnel stage.");

            document
                .querySelector(
                    '[data-step-target="3"]')
                ?.click();

            return false;
        }

        if (stages.length > maximumStageCount) {
            window.alert(
                `You can add up to ${maximumStageCount} stages.`);

            document
                .querySelector(
                    '[data-step-target="3"]')
                ?.click();

            return false;
        }

        const emptyNameIndex = stages.findIndex(
            stage =>
                !String(stage.stageName).trim());

        if (emptyNameIndex >= 0) {
            window.alert(
                "Stage name cannot be empty.");

            focusInvalidStage(
                emptyNameIndex,
                ".funnel-stage-name-field input");

            return false;
        }

        const invalidHoursIndex = stages.findIndex(stage => {
            if (!String(stage.hours).trim())
                return true;

            const hours = Number(stage.hours);

            return !Number.isInteger(hours)
                || hours < 0
                || hours > maximumHours;
        });

        if (invalidHoursIndex >= 0) {
            window.alert(
                "Allowed time must be a whole number between 0 and 8760 hours.");

            focusInvalidStage(
                invalidHoursIndex,
                ".funnel-stage-hours-field input");

            return false;
        }

        const stageNames = new Set();

        const duplicateIndex = stages.findIndex(stage => {
            const name = normalize(stage.stageName);

            if (stageNames.has(name))
                return true;

            stageNames.add(name);
            return false;
        });

        if (duplicateIndex >= 0) {
            window.alert(
                "Stage names must be unique.");

            focusInvalidStage(
                duplicateIndex,
                ".funnel-stage-name-field input");

            return false;
        }

        return true;
    };

    const stopNavigationWhenInvalid = event => {
        if (!funnelStage?.classList.contains("active"))
            return;

        const target = Number(
            event.currentTarget?.dataset?.stepTarget
            ?? 4);

        if (target <= 3 || validateStages())
            return;

        event.preventDefault();
        event.stopImmediatePropagation();
    };

    addButton.addEventListener(
        "click",
        addStage);

    templateButtons.forEach(button => {
        button.addEventListener("click", () => {
            const templateName =
                button.dataset.funnelTemplate
                ?? "";

            if (!templates[templateName] || button.classList.contains("active")) return;
            if (stages.length && !window.confirm("Replace this vacancy’s current stages with the selected template?")) return;
            stages = createTemplateStages(templateName);
            renderStages();
        });
    });

    nextButton?.addEventListener(
        "click",
        stopNavigationWhenInvalid,
        true);

    document
        .querySelectorAll(
            "[data-step-target]")
        .forEach(button => {
            button.addEventListener(
                "click",
                stopNavigationWhenInvalid,
                true);
        });

    vacancyForm?.addEventListener(
        "submit",
        event => {
            if (validateStages())
                return;

            event.preventDefault();
            event.stopImmediatePropagation();
        },
        true);

    renderStages();

    if (Number(initialState.startStage) === 3) {
        window.setTimeout(() => {
            document
                .querySelector(
                    '[data-step-target="3"]')
                ?.click();
        }, 0);
    }
})();
