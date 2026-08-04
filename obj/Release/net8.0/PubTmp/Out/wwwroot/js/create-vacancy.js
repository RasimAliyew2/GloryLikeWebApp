(() => {

    const taxonomyElement =
        document.getElementById("jobTaxonomyJson");

    const initialState =
        window.gloryLikeVacancyInitialState
        ?? {
            jobFamilyId: 0,
            seniorityId: 0,
            positionId: 0,
            selectedSkillIds: [],
            skillRequirements: []
        };

    let taxonomy = [];

    try {
        taxonomy = JSON.parse(
            taxonomyElement?.textContent
            ?? "[]");
    } catch {
        taxonomy = [];
    }

    const normalize = value =>
        String(value ?? "")
            .trim()
            .toLocaleLowerCase();

    const allSqlSkills = (() => {
        const flattened = [];

        taxonomy.forEach(job => {
            (job.seniorities ?? []).forEach(seniority => {
                (seniority.positions ?? []).forEach(position => {
                    (position.skills ?? []).forEach(skill => {
                        if (
                            Number(skill.id) <= 0
                            || !String(
                                skill.skillName
                                ?? "").trim()
                        ) {
                            return;
                        }

                        flattened.push({
                            id: Number(skill.id),
                            skillName:
                                String(skill.skillName).trim(),
                            skillComplexity:
                                String(
                                    skill.skillComplexity
                                    ?? "medium").trim(),
                            jobName:
                                String(
                                    job.jobName
                                    ?? "").trim(),
                            seniorityName:
                                String(
                                    seniority.name
                                    ?? "").trim(),
                            positionName:
                                String(
                                    position.name
                                    ?? "").trim()
                        });
                    });
                });
            });
        });

        const byId = new Map();

        flattened.forEach(skill => {
            if (!byId.has(skill.id))
                byId.set(skill.id, skill);
        });

        return Array.from(byId.values())
            .sort((left, right) =>
                left.skillName.localeCompare(
                    right.skillName));
    })();

    const skillById = new Map(
        allSqlSkills.map(skill => [
            skill.id,
            skill
        ]));

    const selectedRequirements = new Map();

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

        if (stepName)
            stepName.textContent = stageNames[currentStage];

        if (previousButton)
            previousButton.disabled = currentStage === 0;

        if (nextButton) {
            nextButton.hidden =
                currentStage === stages.length - 1;
        }

        if (publishButton) {
            publishButton.hidden =
                currentStage !== stages.length - 1;
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

    const skillSearchShell =
        document.getElementById(
            "vacancySkillSearchShell");

    const skillSearchInput =
        document.getElementById(
            "vacancySkillSearchInput");

    const skillSuggestions =
        document.getElementById(
            "vacancySkillSuggestions");

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

        updateRoleMatch();
    };

    const updateRoleMatch = () => {
        const job = getCurrentJob();
        const position =
            getCurrentPosition();

        if (!job || !position) {
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

    const wireSkillSearchUi = () => {
        if (
            !skillSearchShell
            || !skillSearchInput
            || !skillSuggestions
        ) {
            return;
        }

        if (skillLibraryMessage) {
            skillLibraryMessage.textContent =
                `${allSqlSkills.length} skill SQL taxonomy-dən yükləndi. `
                + `Məsələn “Fa” yazaraq uyğun skill-ləri tap.`;
        }

        skillSearchInput.addEventListener(
            "input",
            renderSuggestions);

        skillSearchInput.addEventListener(
            "focus",
            renderSuggestions);

        skillSearchInput.addEventListener(
            "keydown",
            event => {
                if (event.key === "Escape")
                    closeSuggestions();
            });

        document.addEventListener(
            "click",
            event => {
                if (!skillSearchShell.contains(event.target))
                    closeSuggestions();
            });
    };

    const closeSuggestions = () => {
        if (!skillSuggestions)
            return;

        skillSuggestions.hidden = true;
        skillSuggestions.innerHTML = "";
    };

    const getMatchingSkills = query => {
        const normalizedQuery =
            normalize(query);

        if (!normalizedQuery)
            return [];

        return allSqlSkills
            .filter(
                skill =>
                    !selectedRequirements.has(
                        skill.id))
            .filter(skill =>
                normalize(skill.skillName)
                    .includes(normalizedQuery))
            .sort((left, right) => {
                const leftStarts =
                    normalize(left.skillName)
                        .startsWith(normalizedQuery);

                const rightStarts =
                    normalize(right.skillName)
                        .startsWith(normalizedQuery);

                if (leftStarts !== rightStarts)
                    return leftStarts ? -1 : 1;

                return left.skillName.localeCompare(
                    right.skillName);
            })
            .slice(0, 10);
    };

    const renderSuggestions = () => {
        if (
            !skillSuggestions
            || !skillSearchInput
        ) {
            return;
        }

        const query =
            skillSearchInput.value.trim();

        if (!query) {
            closeSuggestions();
            return;
        }

        const matches =
            getMatchingSkills(query);

        skillSuggestions.innerHTML = "";

        if (matches.length === 0) {
            const empty =
                document.createElement("div");

            empty.className =
                "vacancy-skill-suggestion-empty";

            empty.textContent =
                `“${query}” üçün SQL skill tapılmadı.`;

            skillSuggestions.appendChild(empty);
            skillSuggestions.hidden = false;
            return;
        }

        matches.forEach(skill => {
            const button =
                document.createElement("button");

            button.type = "button";
            button.className =
                "vacancy-skill-suggestion";
            button.setAttribute("role", "option");

            const title =
                document.createElement("strong");

            title.textContent =
                skill.skillName;

            const context =
                document.createElement("span");

            context.textContent =
                [
                    skill.jobName,
                    skill.positionName,
                    skill.skillComplexity
                ]
                .filter(Boolean)
                .join(" · ");

            const plus =
                document.createElement("b");

            plus.textContent = "＋";

            button.append(
                title,
                context,
                plus);

            button.addEventListener(
                "click",
                () => addSkill(skill));

            skillSuggestions.appendChild(button);
        });

        skillSuggestions.hidden = false;
    };

    const addSkill = skill => {
        if (selectedRequirements.has(skill.id))
            return;

        selectedRequirements.set(
            skill.id,
            {
                skillId: skill.id,
                minimumVerificationLevel: 70,
                requirementType: "Required"
            });

        if (skillSearchInput) {
            skillSearchInput.value = "";
            skillSearchInput.focus();
        }

        closeSuggestions();
        renderSelectedSkills();
        updateReview();
    };

    const removeSkill = skillId => {
        selectedRequirements.delete(skillId);
        renderSelectedSkills();
        renderSuggestions();
        updateReview();
    };

    const createHiddenInput = (
        name,
        value) => {

        const input =
            document.createElement("input");

        input.type = "hidden";
        input.name = name;
        input.value = String(value);

        return input;
    };

    const createSelectedSkillCard = (
        requirement,
        index) => {

        const skill =
            skillById.get(
                requirement.skillId);

        if (!skill)
            return null;

        const card =
            document.createElement("article");

        card.className =
            "vacancy-skill-config-card";

        const header =
            document.createElement("header");

        header.className =
            "vacancy-skill-config-header";

        const titleArea =
            document.createElement("div");

        titleArea.className =
            "vacancy-skill-config-title";

        const titleRow =
            document.createElement("div");

        titleRow.className =
            "vacancy-skill-title-row";

        const title =
            document.createElement("h3");

        title.textContent =
            skill.skillName;

        const tp =
            document.createElement("span");

        tp.className = "vacancy-skill-tp";
        tp.textContent = "[TP]";

        const ai =
            document.createElement("span");

        ai.className = "vacancy-skill-ai";
        ai.textContent = "✣ AI";

        titleRow.append(title, tp, ai);

        const context =
            document.createElement("small");

        context.textContent =
            [
                skill.jobName,
                skill.positionName
            ]
            .filter(Boolean)
            .join(" · ");

        titleArea.append(
            titleRow,
            context);

        const removeButton =
            document.createElement("button");

        removeButton.type = "button";
        removeButton.className =
            "vacancy-skill-remove";
        removeButton.textContent = "×";
        removeButton.title = "Remove skill";
        removeButton.setAttribute(
            "aria-label",
            `Remove ${skill.skillName}`);

        removeButton.addEventListener(
            "click",
            () => removeSkill(skill.id));

        header.append(
            titleArea,
            removeButton);

        const typeSelector =
            document.createElement("div");

        typeSelector.className =
            "vacancy-skill-type-selector";

        ["Required", "Desirable"]
            .forEach(type => {
                const button =
                    document.createElement("button");

                button.type = "button";
                button.textContent = type;
                button.classList.toggle(
                    "active",
                    requirement.requirementType === type);

                button.addEventListener(
                    "click",
                    () => {
                        requirement.requirementType = type;
                        renderSelectedSkills();
                    });

                typeSelector.appendChild(button);
            });

        const verification =
            document.createElement("div");

        verification.className =
            "vacancy-skill-verification";

        const verificationHeader =
            document.createElement("div");

        const verificationLabel =
            document.createElement("span");

        verificationLabel.textContent =
            "Min. Verification Level";

        const verificationValue =
            document.createElement("strong");

        verificationValue.textContent =
            String(
                requirement.minimumVerificationLevel);

        verificationHeader.append(
            verificationLabel,
            verificationValue);

        const range =
            document.createElement("input");

        range.type = "range";
        range.min = "1";
        range.max = "100";
        range.step = "1";
        range.value = String(
            requirement.minimumVerificationLevel);

        range.addEventListener(
            "input",
            () => {
                const value = Math.max(
                    1,
                    Math.min(
                        100,
                        Number(range.value)));

                requirement.minimumVerificationLevel =
                    value;

                verificationValue.textContent =
                    String(value);

                levelInput.value =
                    String(value);
            });

        const scale =
            document.createElement("div");

        scale.className =
            "vacancy-skill-range-scale";

        scale.innerHTML = `
            <span>1</span>
            <span>50</span>
            <span>100</span>
        `;

        verification.append(
            verificationHeader,
            range,
            scale);

        const skillIdInput =
            createHiddenInput(
                `Input.SkillRequirements[${index}].SkillId`,
                requirement.skillId);

        const levelInput =
            createHiddenInput(
                `Input.SkillRequirements[${index}].MinimumVerificationLevel`,
                requirement.minimumVerificationLevel);

        const typeInput =
            createHiddenInput(
                `Input.SkillRequirements[${index}].RequirementType`,
                requirement.requirementType);

        const compatibilitySkillId =
            createHiddenInput(
                "Input.SelectedSkillIds",
                requirement.skillId);

        card.append(
            header,
            typeSelector,
            verification,
            skillIdInput,
            levelInput,
            typeInput,
            compatibilitySkillId);

        return card;
    };

    const renderSelectedSkills = () => {
        if (!skillRequirements)
            return;

        skillRequirements.innerHTML = "";

        Array.from(
            selectedRequirements.values())
            .forEach(
                (requirement, index) => {
                    const card =
                        createSelectedSkillCard(
                            requirement,
                            index);

                    if (card)
                        skillRequirements.appendChild(card);
                });

        if (skillLibraryMessage) {
            skillLibraryMessage.hidden =
                selectedRequirements.size > 0;

            if (selectedRequirements.size === 0) {
                skillLibraryMessage.textContent =
                    `${allSqlSkills.length} skill SQL taxonomy-dən yükləndi. `
                    + `Yuxarıdakı axtarış sahəsinə yazaraq skill əlavə et.`;
            }
        }
    };

    const initializeSelectedRequirements = () => {
        const detailed =
            Array.isArray(
                initialState.skillRequirements)
                ? initialState.skillRequirements
                : [];

        detailed.forEach(requirement => {
            const skillId =
                Number(
                    requirement.skillId
                    ?? requirement.SkillId
                    ?? 0);

            if (!skillById.has(skillId))
                return;

            selectedRequirements.set(
                skillId,
                {
                    skillId,
                    minimumVerificationLevel:
                        Math.max(
                            1,
                            Math.min(
                                100,
                                Number(
                                    requirement.minimumVerificationLevel
                                    ?? requirement.MinimumVerificationLevel
                                    ?? 70))),
                    requirementType:
                        String(
                            requirement.requirementType
                            ?? requirement.RequirementType
                            ?? "Required")
                            .toLocaleLowerCase()
                            === "desirable"
                            ? "Desirable"
                            : "Required"
                });
        });

        if (selectedRequirements.size > 0)
            return;

        const legacyLevel = Math.max(
            1,
            Math.min(
                100,
                Number(
                    initialState.minimumVerificationLevel
                    ?? 70)));

        (
            initialState.selectedSkillIds
            ?? []
        ).map(Number)
            .filter(skillId =>
                skillById.has(skillId))
            .forEach(skillId => {
                selectedRequirements.set(
                    skillId,
                    {
                        skillId,
                        minimumVerificationLevel:
                            legacyLevel,
                        requirementType:
                            "Required"
                    });
            });
    };

    jobSelect?.addEventListener(
        "change",
        () => {
            refreshSeniorities();
            updateReview();
        });

    senioritySelect?.addEventListener(
        "change",
        () => {
            refreshPositions();
            updateReview();
        });

    positionSelect?.addEventListener(
        "change",
        () => {
            if (roleTitleInput)
                roleTitleInput.value = "";

            updateRoleMatch();
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
                        ?? "").trim());

        if (missingElement) {
            missingElement.focus();

            window.alert(
                "Job Family, Seniority, Position "
                + "və Role Title doldurulmalıdır.");

            return false;
        }

        if (selectedRequirements.size === 0) {
            window.alert(
                "Ən azı bir SQL skill seçilməlidir.");

            skillSearchInput?.focus();

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

    function updateReview() {
        const job =
            getCurrentJob();

        const seniority =
            getCurrentSeniority();

        const position =
            getCurrentPosition();

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

        if (reviewJob) {
            reviewJob.textContent =
                job?.jobName
                ?? "—";
        }

        if (reviewSeniority) {
            reviewSeniority.textContent =
                seniority?.name
                ?? "—";
        }

        if (reviewPosition) {
            reviewPosition.textContent =
                position?.name
                ?? "—";
        }

        if (reviewSkills) {
            reviewSkills.textContent =
                String(
                    selectedRequirements.size);
        }

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
    }

    document
        .querySelector(
            '[name="Input.Visibility"]')
        ?.addEventListener(
            "change",
            updateReview);

    // updateReview function declaration olduğu üçün hoist edilir və
    // bindRangeOutput ilkin update zamanı onu təhlükəsiz çağıra bilir.
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

        updateRoleMatch();
    };

    initializeSelectedRequirements();
    wireSkillSearchUi();
    initializeTaxonomy();
    renderSelectedSkills();
    updateReview();
    showStage(0);
})();
