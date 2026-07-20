(() => {

    const publicationStage =
        document.querySelector(
            '[data-stage="4"]');

    if (!publicationStage)
        return;

    const initialState =
        window.gloryLikeVacancyInitialState
        ?? {};

    const vacancyForm =
        document.getElementById(
            "createVacancyForm");

    const visibilityInput =
        document.getElementById(
            "publicationVisibilityValue");

    const typeOptions = Array.from(
        publicationStage.querySelectorAll(
            "[data-publication-type]"));

    const typeRadios = Array.from(
        publicationStage.querySelectorAll(
            '[name="publicationVisibilityChoice"]'));

    const priorityInput =
        document.getElementById(
            "publicationPriorityRange");

    const priorityOutput =
        document.getElementById(
            "publicationPriorityValue");

    const widgetCodeElement =
        document.getElementById(
            "careerWidgetCode");

    const skillMatchInput =
        publicationStage.querySelector(
            '[name="Input.PublishOnSkillMatch"]');

    const allowedVisibilityValues = [
        "Public",
        "Internal",
        "Anonymous"
    ];

    const isAllowedVisibility = value =>
        allowedVisibilityValues.includes(
            String(value ?? ""));

    const renderVisibility = value => {
        const selectedValue =
            isAllowedVisibility(value)
                ? String(value)
                : "Internal";

        if (visibilityInput)
            visibilityInput.value = selectedValue;

        typeOptions.forEach(option => {
            const isSelected =
                option.dataset.publicationType
                === selectedValue;

            option.classList.toggle(
                "active",
                isSelected);

            option.setAttribute(
                "aria-checked",
                String(isSelected));
        });

        typeRadios.forEach(radio => {
            radio.checked =
                radio.value === selectedValue;
        });

        return selectedValue;
    };

    typeRadios.forEach(radio => {
        radio.addEventListener(
            "change",
            () => {
                if (!radio.checked)
                    return;

                renderVisibility(radio.value);

                visibilityInput?.dispatchEvent(
                    new Event(
                        "change",
                        { bubbles: true }));
            });
    });

    const updatePriority = () => {
        if (!priorityInput)
            return;

        const minimum = Number(
            priorityInput.min
            || 1);

        const maximum = Number(
            priorityInput.max
            || 10);

        const requestedValue = Number(
            priorityInput.value);

        const value = Math.max(
            minimum,
            Math.min(
                maximum,
                Number.isFinite(requestedValue)
                    ? Math.round(requestedValue)
                    : 5));

        const progress = maximum === minimum
            ? 100
            : ((value - minimum)
                / (maximum - minimum)) * 100;

        priorityInput.value = String(value);
        priorityInput.style.setProperty(
            "--publication-priority-percent",
            `${progress}%`);

        if (priorityOutput)
            priorityOutput.textContent = String(value);
    };

    priorityInput?.addEventListener(
        "input",
        updatePriority);

    priorityInput?.addEventListener(
        "change",
        updatePriority);

    const vacancyId = String(
        initialState.platformVacancyId
        ?? document.querySelector(
            '[name="Input.PlatformVacancyId"]')?.value
        ?? "vacancy")
        .trim()
        || "vacancy";

    const escapeAttribute = value =>
        String(value)
            .replaceAll("&", "&amp;")
            .replaceAll('"', "&quot;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;");

    const trackingLink =
        "https://skillmatch.az/vacancies/"
        + `${encodeURIComponent(vacancyId)}`
        + "?utm_source=social";

    const widgetCode =
        '<script src="https://skillmatch.az/widget.js" '
        + `data-vacancy="${escapeAttribute(vacancyId)}"></script>`;

    if (widgetCodeElement)
        widgetCodeElement.textContent = widgetCode;

    const copyText = async value => {
        if (
            navigator.clipboard
            && typeof navigator.clipboard.writeText
                === "function"
        ) {
            try {
                await navigator.clipboard.writeText(value);
                return true;
            } catch {
                // HTTP və köhnə brauzerlər üçün aşağıdakı fallback işləyir.
            }
        }

        const fallbackInput =
            document.createElement("textarea");

        fallbackInput.value = value;
        fallbackInput.setAttribute(
            "readonly",
            "readonly");
        fallbackInput.style.position = "fixed";
        fallbackInput.style.left = "-9999px";

        document.body.appendChild(
            fallbackInput);

        fallbackInput.select();

        let copied = false;

        try {
            copied = Boolean(
                document.execCommand("copy"));
        } catch {
            copied = false;
        }

        fallbackInput.remove();
        return copied;
    };

    const bindCopyButton = (
        buttonId,
        statusId,
        valueProvider) => {

        const button =
            document.getElementById(buttonId);

        const status =
            document.getElementById(statusId);

        const label =
            button?.querySelector(
                "[data-copy-label]");

        const defaultLabel =
            label?.textContent
            ?? "Copy";

        let resetTimer = 0;

        button?.addEventListener(
            "click",
            async () => {
                button.disabled = true;

                const copied = await copyText(
                    valueProvider());

                if (label) {
                    label.textContent = copied
                        ? "Copied"
                        : "Copy failed";
                }

                if (status) {
                    status.textContent = copied
                        ? "Copied to clipboard."
                        : "Could not copy automatically.";
                }

                window.clearTimeout(resetTimer);
                resetTimer = window.setTimeout(
                    () => {
                        if (label)
                            label.textContent = defaultLabel;

                        if (status)
                            status.textContent = "";
                    },
                    1800);

                button.disabled = false;
            });
    };

    bindCopyButton(
        "copyTrackingLinkButton",
        "trackingLinkCopyStatus",
        () => trackingLink);

    bindCopyButton(
        "copyWidgetCodeButton",
        "widgetCodeCopyStatus",
        () => widgetCode);

    const validatePublication = () => {
        const visibility = String(
            visibilityInput?.value
            ?? "");

        if (!isAllowedVisibility(visibility)) {
            window.alert(
                "Publication type seçilməlidir.");

            document
                .querySelector(
                    '[data-step-target="4"]')
                ?.click();

            typeRadios[0]?.focus();
            return false;
        }

        const priority = Number(
            priorityInput?.value);

        if (
            !Number.isInteger(priority)
            || priority < 1
            || priority > 10
        ) {
            window.alert(
                "Vacancy priority 1–10 arasında olmalıdır.");

            document
                .querySelector(
                    '[data-step-target="4"]')
                ?.click();

            priorityInput?.focus();
            return false;
        }

        return true;
    };

    vacancyForm?.addEventListener(
        "submit",
        event => {
            if (validatePublication())
                return;

            event.preventDefault();
            event.stopImmediatePropagation();
        },
        true);

    if (skillMatchInput)
        skillMatchInput.value = "true";

    renderVisibility(
        visibilityInput?.value);

    updatePriority();

    if (Number(initialState.startStage) === 4) {
        window.setTimeout(() => {
            document
                .querySelector(
                    '[data-step-target="4"]')
                ?.click();
        }, 0);
    }
})();
