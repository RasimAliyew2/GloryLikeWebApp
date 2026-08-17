(() => {
    const panels = Array.from(
        document.querySelectorAll("[data-panel]"));

    const panelBackdrop = document.querySelector(
        "[data-panel-backdrop]");

    const assessmentModal = document.querySelector(
        "[data-assessment-modal]");
    const assessmentBackdrop = document.querySelector(
        "[data-assessment-backdrop]");
    const assessmentConfig = document.querySelector(
        "#skillAssessmentConfig");
    const assessmentSkillName = document.querySelector(
        "[data-assessment-skill-name]");
    const assessmentLanguage = document.querySelector(
        "[data-assessment-language]");
    const assessmentLoading = document.querySelector(
        "[data-assessment-loading]");
    const assessmentError = document.querySelector(
        "[data-assessment-error]");
    const assessmentQuestions = document.querySelector(
        "[data-assessment-questions]");
    const assessmentProgress = document.querySelector(
        "[data-assessment-progress]");
    const assessmentResult = document.querySelector(
        "[data-assessment-result]");
    const assessmentScore = document.querySelector(
        "[data-assessment-score]");
    const assessmentResultCopy = document.querySelector(
        "[data-assessment-result-copy]");
    const submitAssessmentButton = document.querySelector(
        "[data-submit-assessment]");

    const state = {
        skillId: 0,
        skillName: "",
        questionnaire: null,
        selections: new Map(),
        shouldRefresh: false,
        requestController: null
    };

    const closePanels = () => {
        panels.forEach(panel => {
            panel.classList.remove("open");
            panel.setAttribute("aria-hidden", "true");
        });

        panelBackdrop?.classList.remove("visible");

        if (!assessmentModal?.classList.contains("open"))
            document.body.style.overflow = "";
    };

    const setAssessmentError = message => {
        if (!assessmentError) return;

        assessmentError.textContent = message || "Unexpected error.";
        assessmentError.hidden = false;
    };

    const resetAssessment = () => {
        state.questionnaire = null;
        state.selections.clear();
        state.shouldRefresh = false;

        if (assessmentQuestions)
            assessmentQuestions.replaceChildren();

        if (assessmentProgress)
            assessmentProgress.textContent = "0/0 answered";

        if (assessmentError) {
            assessmentError.textContent = "";
            assessmentError.hidden = true;
        }

        if (assessmentResult)
            assessmentResult.hidden = true;

        if (submitAssessmentButton) {
            submitAssessmentButton.hidden = false;
            submitAssessmentButton.disabled = true;
            submitAssessmentButton.textContent = "Finish and save score";
        }
    };

    const closeAssessment = () => {
        state.requestController?.abort();
        state.requestController = null;

        assessmentModal?.classList.remove("open");
        assessmentModal?.setAttribute("aria-hidden", "true");
        assessmentBackdrop?.classList.remove("visible");
        document.body.style.overflow = "";

        if (state.shouldRefresh)
            window.location.reload();
    };

    const antiforgeryToken = () =>
        assessmentConfig?.querySelector(
            'input[name="__RequestVerificationToken"]')?.value || "";

    const postJson = async (url, body, signal) => {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": antiforgeryToken()
            },
            body: JSON.stringify(body),
            signal
        });

        let data;
        try {
            data = await response.json();
        } catch {
            throw new Error(
                "Server uyğun JSON cavabı qaytarmadı.");
        }

        if (!response.ok || !data?.success)
            throw new Error(data?.message || "Operation failed.");

        return data;
    };

    const getVisibleQuestions = () => {
        const questionnaire = state.questionnaire;
        if (!questionnaire?.questions)
            return [];

        const revealedIds = new Set();

        questionnaire.questions.forEach(question => {
            const selected = state.selections.get(question.id)
                || new Set();

            (question.branching || []).forEach(rule => {
                if (selected.has(rule.ifOption))
                    revealedIds.add(rule.revealQuestionId);
            });
        });

        const visible = questionnaire.questions
            .filter(question =>
                !question.hiddenByDefault
                || revealedIds.has(question.id))
            .sort((left, right) => left.order - right.order);

        const visibleIds = new Set(
            visible.map(question => question.id));

        questionnaire.questions.forEach(question => {
            if (!visibleIds.has(question.id))
                state.selections.delete(question.id);
        });

        return visible;
    };

    const updateAssessmentProgress = visibleQuestions => {
        const answered = visibleQuestions.filter(question =>
            (state.selections.get(question.id)?.size || 0) > 0)
            .length;

        if (assessmentProgress) {
            assessmentProgress.textContent =
                `${answered}/${visibleQuestions.length} answered`;
        }

        if (submitAssessmentButton) {
            submitAssessmentButton.disabled =
                visibleQuestions.length === 0
                || answered !== visibleQuestions.length;
        }
    };

    const renderQuestions = () => {
        if (!assessmentQuestions)
            return;

        const visibleQuestions = getVisibleQuestions();
        assessmentQuestions.replaceChildren();

        visibleQuestions.forEach((question, index) => {
            const card = document.createElement("article");
            card.className = "assessment-question-card";

            const heading = document.createElement("div");
            heading.className = "assessment-question-heading";

            const number = document.createElement("span");
            number.textContent = String(index + 1);

            const copy = document.createElement("div");
            const dimension = document.createElement("small");
            dimension.textContent = question.dimension || "skill";
            const title = document.createElement("h3");
            title.textContent = question.text;

            copy.append(dimension, title);
            heading.append(number, copy);
            card.append(heading);

            const options = document.createElement("div");
            options.className = "assessment-option-list";

            const selected = state.selections.get(question.id)
                || new Set();

            (question.options || []).forEach(option => {
                const optionButton = document.createElement("button");
                optionButton.type = "button";
                optionButton.className = "assessment-option";
                optionButton.classList.toggle(
                    "selected",
                    selected.has(option.id));
                optionButton.setAttribute(
                    "aria-pressed",
                    selected.has(option.id) ? "true" : "false");

                const marker = document.createElement("span");
                marker.className = "assessment-option-marker";
                marker.textContent = selected.has(option.id) ? "✓" : "";

                const label = document.createElement("span");
                label.textContent = option.label;

                optionButton.append(marker, label);
                optionButton.addEventListener("click", () => {
                    const current = state.selections.get(question.id)
                        || new Set();

                    if ((question.type || "single").toLowerCase()
                        === "single") {
                        current.clear();
                        current.add(option.id);
                    } else if (current.has(option.id)) {
                        current.delete(option.id);
                    } else {
                        current.add(option.id);
                    }

                    if (current.size > 0)
                        state.selections.set(question.id, current);
                    else
                        state.selections.delete(question.id);

                    renderQuestions();
                });

                options.append(optionButton);
            });

            card.append(options);
            assessmentQuestions.append(card);
        });

        updateAssessmentProgress(visibleQuestions);
    };

    const loadQuestionnaire = async () => {
        if (!assessmentConfig || !state.skillName)
            return;

        state.requestController?.abort();
        state.requestController = new AbortController();
        resetAssessment();

        if (assessmentLoading)
            assessmentLoading.hidden = false;

        try {
            const data = await postJson(
                assessmentConfig.dataset.generateUrl,
                {
                    skillId: state.skillId,
                    skillName: state.skillName,
                    language: assessmentLanguage?.value || "az"
                },
                state.requestController.signal);

            state.questionnaire = data.questionnaire;
            renderQuestions();
        } catch (error) {
            if (error?.name !== "AbortError")
                setAssessmentError(error.message);
        } finally {
            if (assessmentLoading)
                assessmentLoading.hidden = true;
        }
    };

    const openAssessment = (skillId, skillName) => {
        if (!assessmentModal || !assessmentBackdrop)
            return;

        closePanels();
        state.skillId = Number.parseInt(skillId, 10) || 0;
        state.skillName = skillName || "Skill";

        if (assessmentSkillName)
            assessmentSkillName.textContent = state.skillName;

        assessmentModal.classList.add("open");
        assessmentModal.setAttribute("aria-hidden", "false");
        assessmentBackdrop.classList.add("visible");
        document.body.style.overflow = "hidden";

        loadQuestionnaire();
    };

    const submitAssessment = async () => {
        const visibleQuestions = getVisibleQuestions();
        const answers = visibleQuestions.map(question => ({
            questionId: question.id,
            selectedOptionIds: Array.from(
                state.selections.get(question.id) || [])
        }));

        if (answers.some(answer => answer.selectedOptionIds.length === 0)) {
            setAssessmentError(
                "Bütün görünən suallar cavablandırılmalıdır.");
            return;
        }

        submitAssessmentButton.disabled = true;
        submitAssessmentButton.textContent = "Saving score…";

        if (assessmentError)
            assessmentError.hidden = true;

        try {
            const data = await postJson(
                assessmentConfig.dataset.submitUrl,
                {
                    skillId: state.skillId,
                    skillName: state.skillName,
                    questionnaireId:
                        state.questionnaire.questionnaireId,
                    answers
                });

            state.shouldRefresh = true;

            if (assessmentQuestions)
                assessmentQuestions.replaceChildren();

            if (assessmentResult) {
                assessmentResult.hidden = false;
                assessmentScore.textContent = String(data.score ?? 0);
                assessmentResultCopy.textContent =
                    `${data.depthTier || "verified"} · `
                    + `${data.ownershipLevel || "assessed"}. `
                    + `Credibility: ${data.credibilityScore ?? 0}/100.`;
            }

            if (assessmentProgress)
                assessmentProgress.textContent = "Completed";

            submitAssessmentButton.hidden = true;
        } catch (error) {
            setAssessmentError(error.message);
            submitAssessmentButton.disabled = false;
            submitAssessmentButton.textContent =
                "Finish and save score";
        }
    };

    document
        .querySelectorAll("[data-open-panel]")
        .forEach(button => {
            button.addEventListener("click", () => {
                const targetName =
                    button.getAttribute("data-open-panel");

                const panel = document.querySelector(
                    `[data-panel="${targetName}"]`);

                if (!panel) return;

                closePanels();

                panel.classList.add("open");
                panel.setAttribute("aria-hidden", "false");
                panelBackdrop?.classList.add("visible");
                document.body.style.overflow = "hidden";

                const firstField =
                    panel.querySelector("select, input");

                window.setTimeout(
                    () => firstField?.focus(),
                    180);
            });
        });

    document
        .querySelectorAll("[data-close-panel]")
        .forEach(button => {
            button.addEventListener("click", closePanels);
        });

    document
        .querySelectorAll("[data-assess-skill]")
        .forEach(button => {
            button.addEventListener("click", () => {
                openAssessment(
                    button.dataset.skillId,
                    button.dataset.skillName);
            });
        });

    document
        .querySelectorAll("[data-close-assessment]")
        .forEach(button => {
            button.addEventListener("click", closeAssessment);
        });

    assessmentLanguage?.addEventListener(
        "change",
        loadQuestionnaire);

    submitAssessmentButton?.addEventListener(
        "click",
        submitAssessment);

    panelBackdrop?.addEventListener("click", closePanels);
    assessmentBackdrop?.addEventListener("click", closeAssessment);

    document.addEventListener("keydown", event => {
        if (event.key !== "Escape")
            return;

        if (assessmentModal?.classList.contains("open"))
            closeAssessment();
        else
            closePanels();
    });

    const autoSkillName =
        assessmentConfig?.dataset.autoSkillName || "";

    if (autoSkillName) {
        window.setTimeout(() => {
            openAssessment(
                assessmentConfig.dataset.autoSkillId,
                autoSkillName);
        }, 120);
    }
})();
