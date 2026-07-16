(() => {
    const taxonomyElement =
        document.getElementById("jobTaxonomyJson");

    const initialState =
        window.gloryLikeVacancyInitialState
        ?? {
            jobFamilyId: 0,
            seniorityId: 0,
            positionId: 0,
            selectedSkillIds: []
        };

    let taxonomy = [];

    try {
        taxonomy = JSON.parse(
            taxonomyElement?.textContent
            ?? "[]");
    } catch {
        taxonomy = [];
    }

    const stages = Array.from(
        document.querySelectorAll(
            "[data-stage]"));

    const stepButtons = Array.from(
        document.querySelectorAll(
            "[data-step-target]"));

    const previousButton =
        document.getElementById(
            "previousStepButton");

    const nextButton =
        document.getElementById(
            "nextStepButton");

    const publishButton =
        document.getElementById(
            "publishVacancyButton");

    const stepText =
        document.getElementById(
            "currentStepText");

    const stepName =
        document.getElementById(
            "currentStepName");

    const stageNames = [
        "Role and Profile",
        "Application Requirements",
        "Screening",
        "Funnel",
        "Publication"
    ];

    let currentStage = 0;

    const showStage = stageIndex => {
        currentStage = Math.max(
            0,
            Math.min(
                stageIndex,
                stages.length - 1));

        stages.forEach(
            (stage, index) => {
                stage.classList.toggle(
                    "active",
                    index === currentStage);
            });

        stepButtons.forEach(
            (button, index) => {
                button.classList.toggle(
                    "active",
                    index === currentStage);

                button.classList.toggle(
                    "completed",
                    index < currentStage);
            });

        if (stepText) {
            stepText.textContent =
                `Step ${currentStage + 1} of ${stages.length}`;
        }

        if (stepName) {
            stepName.textContent =
                stageNames[currentStage];
        }

        if (previousButton) {
            previousButton.disabled =
                currentStage === 0;
        }

        if (nextButton) {
            nextButton.hidden =
                currentStage
                === stages.length - 1;
        }

        if (publishButton) {
            publishButton.hidden =
                currentStage
                !== stages.length - 1;
        }

        if (currentStage === stages.length - 1)
            updateReview();

        window.scrollTo({
            top: 0,
            behavior: "smooth"
        });
    };

    const jobSelect =
        document.getElementById(
            "jobFamilySelect");

    const senioritySelect =
        document.getElementById(
            "senioritySelect");

    const positionSelect =
        document.getElementById(
            "positionSelect");

    const roleTitleInput =
        document.getElementById(
            "roleTitleInput");

    const skillRequirements =
        document.getElementById(
            "skillRequirements");

    const skillLibraryMessage =
        document.getElementById(
            "skillLibraryMessage");

    const roleMatchBox =
        document.getElementById(
            "roleMatchBox");

    const selectedJobText =
        document.getElementById(
            "selectedJobText");

    const selectedPositionText =
        document.getElementById(
            "selectedPositionText");

    const getCurrentJob = () => {
        const id = Number(
            jobSelect?.value
            ?? 0);

        return taxonomy.find(
            job => Number(job.id) === id)
            ?? null;
    };

    const getCurrentSeniority = () => {
        const job = getCurrentJob();

        if (!job)
            return null;

        const id = Number(
            senioritySelect?.value
            ?? 0);

        return (
            job.seniorities
            ?? []
        ).find(
            seniority =>
                Number(seniority.id) === id)
            ?? null;
    };

    const getCurrentPosition = () => {
        const seniority =
            getCurrentSeniority();

        if (!seniority)
            return null;

        const id = Number(
            positionSelect?.value
            ?? 0);

        return (
            seniority.positions
            ?? []
        ).find(
            position =>
                Number(position.id) === id)
            ?? null;
    };

    const fillSelect = (
        select,
        items,
        placeholder,
        getValue,
        getText,
        selectedValue = 0) => {

        if (!select)
            return;

        select.innerHTML = "";

        const placeholderOption =
            document.createElement("option");

        placeholderOption.value = "";
        placeholderOption.textContent =
            placeholder;

        select.appendChild(
            placeholderOption);

        items.forEach(item => {
            const option =
                document.createElement("option");

            option.value =
                String(getValue(item));

            option.textContent =
                getText(item);

            if (
                Number(getValue(item))
                === Number(selectedValue)
            ) {
                option.selected = true;
            }

            select.appendChild(option);
        });

        select.disabled =
            items.length === 0;
    };

    const refreshSeniorities = (
        selectedValue = 0) => {

        const job = getCurrentJob();

        fillSelect(
            senioritySelect,
            job?.seniorities ?? [],
            job
                ? "Select Seniority"
                : "Select Job first",
            item => item.id,
            item => item.name,
            selectedValue);

        refreshPositions();
    };

    const refreshPositions = (
        selectedValue = 0) => {

        const seniority =
            getCurrentSeniority();

        fillSelect(
            positionSelect,
            seniority?.positions ?? [],
            seniority
                ? "Select Position"
                : "Select Seniority first",
            item => item.id,
            item => item.name,
            selectedValue);

        refreshSkills();
    };

    const renderSkill = (
        skill,
        isSelected) => {

        const label =
            document.createElement("label");

        label.className =
            "skill-requirement";

        if (isSelected)
            label.classList.add("selected");

        const checkbox =
            document.createElement("input");

        checkbox.type = "checkbox";
        checkbox.name =
            "SelectedSkillIds";
        checkbox.value =
            String(skill.id);
        checkbox.checked =
            isSelected;

        checkbox.addEventListener(
            "change",
            () => {
                label.classList.toggle(
                    "selected",
                    checkbox.checked);

                updateReview();
            });

        const copy =
            document.createElement("span");

        copy.className =
            "skill-requirement-copy";

        const title =
            document.createElement("strong");

        title.textContent =
            skill.skillName
            ?? "Unnamed skill";

        const tags =
            document.createElement("span");

        tags.className =
            "skill-tags";

        const requiredTag =
            document.createElement("span");

        requiredTag.textContent =
            "Required";

        const complexityTag =
            document.createElement("span");

        complexityTag.textContent =
            skill.skillComplexity
            ?? "medium";

        tags.append(
            requiredTag,
            complexityTag);

        const note =
            document.createElement("small");

        note.textContent =
            "Loaded from SQL Position skills";

        copy.append(
            title,
            tags,
            note);

        label.append(
            checkbox,
            copy);

        return label;
    };

    const refreshSkills = () => {
        const position =
            getCurrentPosition();

        const skills =
            position?.skills
            ?? [];

        if (skillRequirements)
            skillRequirements.innerHTML = "";

        const previouslySelected =
            new Set(
                (
                    initialState.selectedSkillIds
                    ?? []
                ).map(Number));

        if (!position) {
            if (skillLibraryMessage) {
                skillLibraryMessage.hidden =
                    false;

                skillLibraryMessage.textContent =
                    "Select SQL Job, Seniority and Position "
                    + "to load its skill library.";
            }

            updateRoleMatch();
            return;
        }

        if (skills.length === 0) {
            if (skillLibraryMessage) {
                skillLibraryMessage.hidden =
                    false;

                skillLibraryMessage.textContent =
                    "This SQL Position has no skills.";
            }

            updateRoleMatch();
            return;
        }

        if (skillLibraryMessage)
            skillLibraryMessage.hidden = true;

        skills.forEach(skill => {
            skillRequirements?.appendChild(
                renderSkill(
                    skill,
                    previouslySelected.has(
                        Number(skill.id))));
        });

        updateRoleMatch();
    };

    const updateRoleMatch = () => {
        const job = getCurrentJob();
        const position =
            getCurrentPosition();

        if (
            !job
            || !position
        ) {
            if (roleMatchBox)
                roleMatchBox.hidden = true;

            return;
        }

        if (
            roleTitleInput
            && !roleTitleInput.value.trim()
        ) {
            roleTitleInput.value =
                position.name
                ?? "";
        }

        if (selectedJobText) {
            selectedJobText.textContent =
                job.jobName
                ?? "";
        }

        if (selectedPositionText) {
            selectedPositionText.textContent =
                position.name
                ?? "";
        }

        if (roleMatchBox)
            roleMatchBox.hidden = false;
    };

    jobSelect?.addEventListener(
        "change",
        () => {
            initialState.selectedSkillIds = [];
            refreshSeniorities();
            updateReview();
        });

    senioritySelect?.addEventListener(
        "change",
        () => {
            initialState.selectedSkillIds = [];
            refreshPositions();
            updateReview();
        });

    positionSelect?.addEventListener(
        "change",
        () => {
            initialState.selectedSkillIds = [];

            if (roleTitleInput)
                roleTitleInput.value = "";

            refreshSkills();
            updateReview();
        });

    const validateStageOne = () => {
        const requiredElements = [
            jobSelect,
            senioritySelect,
            positionSelect,
            roleTitleInput
        ];

        const missingElement =
            requiredElements.find(
                element =>
                    !String(
                        element?.value
                        ?? ""
                    ).trim());

        if (missingElement) {
            missingElement.focus();

            window.alert(
                "Job Family, Seniority, Position "
                + "və Role Title doldurulmalıdır.");

            return false;
        }

        const selectedSkills =
            document.querySelectorAll(
                'input[name="SelectedSkillIds"]:checked');

        if (selectedSkills.length === 0) {
            window.alert(
                "Ən azı bir SQL skill seçilməlidir.");

            skillRequirements?.scrollIntoView({
                behavior: "smooth",
                block: "center"
            });

            return false;
        }

        return true;
    };

    const canLeaveCurrentStage = () => {
        if (currentStage === 0)
            return validateStageOne();

        return true;
    };

    nextButton?.addEventListener(
        "click",
        () => {
            if (!canLeaveCurrentStage())
                return;

            showStage(currentStage + 1);
        });

    previousButton?.addEventListener(
        "click",
        () => showStage(
            currentStage - 1));

    stepButtons.forEach(button => {
        button.addEventListener(
            "click",
            () => {
                const target =
                    Number(
                        button.dataset.stepTarget
                        ?? 0);

                if (
                    target > currentStage
                    && !canLeaveCurrentStage()
                ) {
                    return;
                }

                showStage(target);
            });
    });

    const bindRangeOutput = (
        inputId,
        outputId,
        suffix = "") => {

        const input =
            document.getElementById(
                inputId);

        const output =
            document.getElementById(
                outputId);

        const update = () => {
            if (output && input) {
                output.textContent =
                    `${input.value}${suffix}`;
            }

            updateReview();
        };

        input?.addEventListener(
            "input",
            update);

        update();
    };

    bindRangeOutput(
        "verificationRange",
        "verificationValue");

    bindRangeOutput(
        "matchScoreRange",
        "matchScoreValue",
        "%");

    bindRangeOutput(
        "trustScoreRange",
        "trustScoreValue");

    const updateReview = () => {
        const job =
            getCurrentJob();

        const seniority =
            getCurrentSeniority();

        const position =
            getCurrentPosition();

        const selectedSkillCount =
            document.querySelectorAll(
                'input[name="SelectedSkillIds"]:checked')
                .length;

        const reviewJob =
            document.getElementById(
                "reviewJob");

        const reviewSeniority =
            document.getElementById(
                "reviewSeniority");

        const reviewPosition =
            document.getElementById(
                "reviewPosition");

        const reviewSkills =
            document.getElementById(
                "reviewSkills");

        const reviewMatchScore =
            document.getElementById(
                "reviewMatchScore");

        const reviewVisibility =
            document.getElementById(
                "reviewVisibility");

        if (reviewJob)
            reviewJob.textContent =
                job?.jobName
                ?? "—";

        if (reviewSeniority)
            reviewSeniority.textContent =
                seniority?.name
                ?? "—";

        if (reviewPosition)
            reviewPosition.textContent =
                position?.name
                ?? "—";

        if (reviewSkills)
            reviewSkills.textContent =
                String(selectedSkillCount);

        const matchInput =
            document.getElementById(
                "matchScoreRange");

        if (reviewMatchScore) {
            reviewMatchScore.textContent =
                `${matchInput?.value ?? 0}%`;
        }

        const visibility =
            document.querySelector(
                '[name="Input.Visibility"]');

        if (reviewVisibility) {
            reviewVisibility.textContent =
                visibility?.value
                ?? "Public";
        }
    };

    document
        .querySelector(
            '[name="Input.Visibility"]')
        ?.addEventListener(
            "change",
            updateReview);

    const initializeTaxonomy = () => {
        if (
            Number(initialState.jobFamilyId) > 0
            && jobSelect
        ) {
            jobSelect.value =
                String(
                    initialState.jobFamilyId);
        }

        refreshSeniorities(
            initialState.seniorityId);

        if (
            Number(initialState.seniorityId) > 0
            && senioritySelect
        ) {
            senioritySelect.value =
                String(
                    initialState.seniorityId);
        }

        refreshPositions(
            initialState.positionId);

        if (
            Number(initialState.positionId) > 0
            && positionSelect
        ) {
            positionSelect.value =
                String(
                    initialState.positionId);
        }

        refreshSkills();
        updateReview();
    };

    initializeTaxonomy();
    showStage(0);
})();
