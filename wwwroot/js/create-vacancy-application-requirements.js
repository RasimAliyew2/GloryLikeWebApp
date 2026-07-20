
(() => {
    const stage =
        document.querySelector(".application-requirements-stage");

    if (!stage)
        return;

    const requiredCount =
        document.getElementById("applicationRequiredCount");

    const optionalCount =
        document.getElementById("applicationOptionalCount");

    const estimatedMinutes =
        document.getElementById("applicationEstimatedMinutes");

    const customFieldsContainer =
        document.getElementById("applicationCustomFields");

    const addCustomFieldButton =
        document.getElementById("applicationRequestFieldButton");

    const previewButton =
        document.getElementById("applicationPreviewButton");

    const previewPanel =
        document.getElementById("applicationPreviewPanel");

    const previewBackdrop =
        document.getElementById("applicationPreviewBackdrop");

    const previewCloseButton =
        document.getElementById("applicationPreviewCloseButton");

    const previewContent =
        document.getElementById("applicationPreviewContent");

    const fixedFieldLabels = new Map([
        ["Input.ApplicationRequirements.FullName", "Full Name"],
        ["Input.ApplicationRequirements.Email", "Email"],
        ["Input.ApplicationRequirements.Phone", "Phone"],
        ["Input.ApplicationRequirements.Location", "Location"],
        ["Input.ApplicationRequirements.WorkExperience", "Work experience"],
        ["Input.ApplicationRequirements.CurrentPosition", "Current position"],
        ["Input.ApplicationRequirements.PreviousCompanies", "Previous companies"],
        ["Input.ApplicationRequirements.Education", "Education"],
        ["Input.ApplicationRequirements.Certifications", "Certifications"],
        ["Input.ApplicationRequirements.Trainings", "Trainings"],
        ["Input.ApplicationRequirements.Languages", "Languages"],
        ["Input.ApplicationRequirements.Tools", "Tools"],
        ["Input.ApplicationRequirements.LinkedIn", "LinkedIn"],
        ["Input.ApplicationRequirements.GitHub", "GitHub"],
        ["Input.ApplicationRequirements.Portfolio", "Portfolio"],
        ["Input.ApplicationRequirements.PersonalWebsite", "Personal website"],
        ["Input.ApplicationRequirements.CoverLetter", "Cover letter"],
        ["Input.ApplicationRequirements.AdditionalFiles", "Additional Files"]
    ]);

    const getModeSelects = () =>
        Array.from(
            stage.querySelectorAll(".application-field-mode"));

    const setModeStyle = select => {
        select.dataset.mode = select.value;
    };

    const reindexCustomFields = () => {
        const rows = Array.from(
            customFieldsContainer?.querySelectorAll(
                "[data-custom-field-row]")
            ?? []);

        rows.forEach((row, index) => {
            const labelInput =
                row.querySelector("[data-custom-field-label]");

            const modeSelect =
                row.querySelector("[data-custom-field-mode]");

            if (labelInput) {
                labelInput.name =
                    `Input.ApplicationRequirements.CustomFields[${index}].Label`;
            }

            if (modeSelect) {
                modeSelect.name =
                    `Input.ApplicationRequirements.CustomFields[${index}].Requirement`;
            }
        });
    };

    const updateSummary = () => {
        const selects = getModeSelects();

        const required = selects.filter(
            select => select.value === "Required").length;

        const optional = selects.filter(
            select => select.value === "Optional").length;

        const minutes = Math.max(
            2,
            Math.round(
                3
                + required * 1.5
                + optional * 0.5));

        if (requiredCount)
            requiredCount.textContent = String(required);

        if (optionalCount)
            optionalCount.textContent = String(optional);

        if (estimatedMinutes)
            estimatedMinutes.textContent = `${minutes} min`;

        selects.forEach(setModeStyle);
    };

    const createCustomFieldRow = () => {
        const row = document.createElement("article");
        row.className = "application-custom-field-row";
        row.dataset.customFieldRow = "true";

        row.innerHTML = `
            <input
                type="text"
                maxlength="100"
                placeholder="New field name"
                data-custom-field-label />

            <select
                class="application-field-mode"
                data-custom-field-mode>
                <option value="Required">Required</option>
                <option value="Optional" selected>Optional</option>
                <option value="Hidden">Hidden</option>
            </select>

            <button
                type="button"
                class="application-remove-custom-field"
                data-remove-custom-field
                aria-label="Remove custom field">
                ×
            </button>
        `;

        customFieldsContainer?.appendChild(row);
        wireCustomFieldRow(row);
        reindexCustomFields();
        updateSummary();

        row
            .querySelector("[data-custom-field-label]")
            ?.focus();
    };

    const wireCustomFieldRow = row => {
        row
            .querySelector("[data-remove-custom-field]")
            ?.addEventListener("click", () => {
                row.remove();
                reindexCustomFields();
                updateSummary();
            });

        row
            .querySelector("[data-custom-field-mode]")
            ?.addEventListener("change", updateSummary);
    };

    const getPreviewFields = () => {
        const fields = [];

        fixedFieldLabels.forEach((label, name) => {
            const select =
                stage.querySelector(`select[name="${name}"]`);

            if (!select || select.value === "Hidden")
                return;

            fields.push({
                label,
                mode: select.value
            });
        });

        Array.from(
            customFieldsContainer?.querySelectorAll(
                "[data-custom-field-row]")
            ?? [])
            .forEach(row => {
                const label =
                    row
                        .querySelector("[data-custom-field-label]")
                        ?.value
                        .trim();

                const mode =
                    row
                        .querySelector("[data-custom-field-mode]")
                        ?.value
                    ?? "Optional";

                if (!label || mode === "Hidden")
                    return;

                fields.push({
                    label,
                    mode
                });
            });

        return fields;
    };

    const renderPreview = () => {
        if (!previewContent)
            return;

        const fields = getPreviewFields();
        previewContent.innerHTML = "";

        if (fields.length === 0) {
            const empty = document.createElement("div");
            empty.className = "application-preview-empty";
            empty.textContent =
                "All candidate fields are hidden.";
            previewContent.appendChild(empty);
            return;
        }

        fields.forEach(field => {
            const wrapper = document.createElement("div");
            wrapper.className = "application-preview-field";

            const label = document.createElement("label");
            label.textContent = field.label;

            if (field.mode === "Required") {
                const required = document.createElement("span");
                required.textContent = "Required";
                label.appendChild(required);
            }

            const input = document.createElement("input");
            input.type = "text";
            input.disabled = true;
            input.placeholder = `Candidate ${field.label.toLowerCase()}`;

            wrapper.append(label, input);
            previewContent.appendChild(wrapper);
        });
    };

    const openPreview = () => {
        renderPreview();

        if (previewBackdrop)
            previewBackdrop.hidden = false;

        previewPanel?.classList.add("open");
        previewPanel?.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    };

    const closePreview = () => {
        if (previewBackdrop)
            previewBackdrop.hidden = true;

        previewPanel?.classList.remove("open");
        previewPanel?.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
    };

    getModeSelects().forEach(select => {
        setModeStyle(select);
        select.addEventListener("change", updateSummary);
    });

    Array.from(
        customFieldsContainer?.querySelectorAll(
            "[data-custom-field-row]")
        ?? [])
        .forEach(wireCustomFieldRow);

    addCustomFieldButton?.addEventListener(
        "click",
        createCustomFieldRow);

    previewButton?.addEventListener(
        "click",
        openPreview);

    previewCloseButton?.addEventListener(
        "click",
        closePreview);

    previewBackdrop?.addEventListener(
        "click",
        closePreview);

    document.addEventListener("keydown", event => {
        if (event.key === "Escape")
            closePreview();
    });

    reindexCustomFields();
    updateSummary();
})();
