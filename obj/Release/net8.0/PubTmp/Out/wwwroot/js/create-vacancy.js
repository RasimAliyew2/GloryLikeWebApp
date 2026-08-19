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

    const skillInstances = (() => {
        const flattened = [];

        taxonomy.forEach(job => {
            (job.positions ?? []).forEach(position => {
                (position.seniorities ?? []).forEach(seniority => {
                    (seniority.skills ?? []).forEach(skill => {
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
                            positionId:
                                Number(position.id),
                            seniorityId:
                                Number(seniority.id),
                            senioritySortOrder:
                                Number(
                                    seniority.sortOrder
                                    ?? 0),
                            seniorityName:
                                String(
                                    seniority.name
                                    ?? "").trim(),
                            positionName:
                                String(
                                    position.name
                                    ?? "").trim(),
                            minimumSenioritySortOrder:
                                Number(
                                    skill.minimumSenioritySortOrder
                                    ?? 1),
                            isCore:
                                Boolean(skill.isCore),
                            assessmentType:
                                String(
                                    skill.assessmentType
                                    ?? "TP").trim(),
                            verificationMethod:
                                String(
                                    skill.verificationMethod
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
        skillInstances.map(skill => [
            skill.id,
            skill
        ]));

    // Manual search is a global skill catalogue. Core skills are duplicated
    // per Position in SQL for automatic selection, so show one option per
    // skill name instead of repeating the same catalogue item.
    const allSqlSkills = (() => {
        const byName = new Map();

        skillInstances.forEach(skill => {
            const key = normalize(skill.skillName);
            const existing = byName.get(key);

            if (!existing || skill.id < existing.id)
                byName.set(key, skill);
        });

        return Array.from(byName.values())
            .sort((left, right) =>
                left.skillName.localeCompare(
                    right.skillName));
    })();

    const selectedRequirements = new Map();

    const isSkillNameSelected = skillName => {
        const normalizedName = normalize(skillName);

        return Array.from(
            selectedRequirements.keys())
            .map(skillId => skillById.get(skillId))
            .filter(Boolean)
            .some(skill =>
                normalize(skill.skillName)
                === normalizedName);
    };

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

    const companyLocationSelect =
        document.getElementById(
            "companyLocationSelect");

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

    const getCurrentPosition = () => {
        const job = getCurrentJob();

        if (!job)
            return null;

        const id = Number(
            positionSelect?.value
            ?? 0);

        return (
            job.positions
            ?? []
        ).find(
            position =>
                Number(position.id) === id)
            ?? null;
    };

    const getCurrentJobSeniorities = () => {
        const job = getCurrentJob();

        if (!job)
            return [];

        const senioritiesById = new Map();

        (job.positions ?? []).forEach(position => {
            (position.seniorities ?? []).forEach(seniority => {
                const id = Number(seniority.id);

                if (id > 0 && !senioritiesById.has(id))
                    senioritiesById.set(id, seniority);
            });
        });

        return Array.from(senioritiesById.values())
            .sort((left, right) => {
                const orderDifference =
                    Number(left.sortOrder ?? 0)
                    - Number(right.sortOrder ?? 0);

                return orderDifference !== 0
                    ? orderDifference
                    : String(left.name ?? "").localeCompare(
                        String(right.name ?? ""));
            });
    };

    const getCurrentSeniority = () => {
        const id = Number(
            senioritySelect?.value
            ?? 0);

        return getCurrentJobSeniorities().find(
            seniority =>
                Number(seniority.id) === id)
            ?? null;
    };

    const getPositionsForCurrentSeniority = () => {
        const job = getCurrentJob();
        const seniority = getCurrentSeniority();

        if (!job || !seniority)
            return [];

        return (job.positions ?? []).filter(position =>
            (position.seniorities ?? []).some(item =>
                Number(item.id) === Number(seniority.id)));
    };

    const getCurrentAutomaticSkills = () => {
        const position = getCurrentPosition();
        const seniority = getCurrentSeniority();

        if (!position || !seniority)
            return [];

        const positionSeniority =
            (position.seniorities ?? []).find(item =>
                Number(item.id) === Number(seniority.id));

        if (!positionSeniority)
            return [];

        return (positionSeniority.skills ?? [])
            .map(skill =>
                skillById.get(Number(skill.id)))
            .filter(Boolean)
            .sort((left, right) => {
                if (left.isCore !== right.isCore)
                    return left.isCore ? -1 : 1;

                return left.skillName.localeCompare(
                    right.skillName);
            });
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
            getCurrentJobSeniorities(),
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

        const seniority = getCurrentSeniority();

        fillSelect(
            positionSelect,
            getPositionsForCurrentSeniority(),
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
                "Position seçildikdə onun bütün skill-ləri avtomatik "
                + "əlavə olunur. Manual axtarış bütün SQL skill "
                + "kataloqunda işləyir.";
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

        const position = getCurrentPosition();

        if (!normalizedQuery || !position)
            return [];

        return allSqlSkills
            .filter(
                skill =>
                    !isSkillNameSelected(
                        skill.skillName))
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

            const plus =
                document.createElement("b");

            plus.textContent = "＋";

            button.append(
                title,
                plus);

            button.addEventListener(
                "click",
                () => addSkill(skill));

            skillSuggestions.appendChild(button);
        });

        skillSuggestions.hidden = false;
    };

    const addSkill = skill => {
        if (
            selectedRequirements.has(skill.id)
            || isSkillNameSelected(skill.skillName)
        )
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
        tp.textContent =
            `[${skill.assessmentType || "TP"}]`;

        const ai =
            document.createElement("span");

        ai.className = "vacancy-skill-ai";
        ai.textContent = "✣ AI";

        titleRow.append(title, tp, ai);

        titleArea.append(titleRow);

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
            const position = getCurrentPosition();
            const automaticSkills =
                getCurrentAutomaticSkills();
            const coreSkillCount =
                automaticSkills.filter(skill =>
                    skill.isCore).length;
            const positionSkillCount =
                automaticSkills.length
                - coreSkillCount;

            skillLibraryMessage.hidden = false;
            skillLibraryMessage.textContent = position
                ? `${coreSkillCount} Core və ${positionSkillCount} Position skill-i `
                    + "seçilən Seniority-yə uyğun olaraq avtomatik əlavə olunub. "
                    + "Manual axtarış bütün SQL skill kataloqunda işləyir."
                : "Position seçildikdə uyğun Core və Position skill-ləri "
                    + "avtomatik əlavə olunur. Manual axtarış bütün SQL skill "
                    + "kataloqunda işləyir.";
        }
    };

    const replaceWithCurrentPositionSkills = () => {
        const position = getCurrentPosition();

        selectedRequirements.clear();

        if (position) {
            getCurrentAutomaticSkills()
                .forEach(skill => {
                    selectedRequirements.set(
                        skill.id,
                        {
                            skillId: skill.id,
                            minimumVerificationLevel: 70,
                            requirementType: "Required"
                        });
                });
        }

        if (skillSearchInput)
            skillSearchInput.value = "";

        closeSuggestions();
        renderSelectedSkills();
        updateReview();
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
            if (roleTitleInput)
                roleTitleInput.value = "";

            refreshSeniorities();
            replaceWithCurrentPositionSkills();
        });

    senioritySelect?.addEventListener(
        "change",
        () => {
            if (roleTitleInput)
                roleTitleInput.value = "";

            refreshPositions();
            replaceWithCurrentPositionSkills();
        });

    positionSelect?.addEventListener(
        "change",
        () => {
            updateRoleMatch();
            replaceWithCurrentPositionSkills();
            renderSuggestions();
            updateReview();
        });

    const validateStageOne = () => {
        const requiredElements = [
            jobSelect,
            senioritySelect,
            positionSelect,
            roleTitleInput,
            companyLocationSelect
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
                "Job Family, Seniority, Position, Role Title "
                + "və Company Location doldurulmalıdır.");

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
