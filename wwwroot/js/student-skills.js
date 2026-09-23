(() => {
    const addForm = document.querySelector("[data-student-add-skill]");
    const search = document.querySelector("[data-student-skill-search]");
    const results = document.querySelector("[data-skill-results]");
    const searchStatus = document.querySelector("[data-skill-search-status]");
    const catalogueElement = document.querySelector("#studentSkillCatalogue");

    if (addForm && search && results && catalogueElement) {
        let catalogue;
        try {
            catalogue = JSON.parse(catalogueElement.textContent);
        } catch {
            catalogue = null;
        }

        if (Array.isArray(catalogue)) {
            const fallback = addForm.querySelector("[data-skill-search-fallback]");
            fallback.hidden = true;
            fallback.querySelector("select").disabled = true;
            addForm.querySelector("[data-skill-search-enhancement]").hidden = false;

            const normalize = value => value.trim().toLocaleLowerCase();
            search.addEventListener("input", () => {
                const query = normalize(search.value);
                results.replaceChildren();
                results.hidden = !query;
                searchStatus.textContent = "";
                if (!query) return;

                const matches = catalogue.filter(skill => normalize(skill.name).includes(query));
                const visible = matches.slice(0, 8);
                searchStatus.textContent = matches.length === 0
                    ? "No skills found. Try another search."
                    : matches.length > visible.length
                        ? `Showing ${visible.length} of ${matches.length} skills. Keep typing to narrow the results.`
                        : `${matches.length} ${matches.length === 1 ? "skill" : "skills"} found.`;
                results.hidden = visible.length === 0;

                visible.forEach(skill => {
                    const button = document.createElement("button");
                    button.type = "submit";
                    button.name = "SkillId";
                    button.value = String(skill.id);
                    button.className = "student-skill-search-result";
                    const name = document.createElement("span");
                    name.textContent = skill.name;
                    const action = document.createElement("small");
                    action.textContent = "+ Add skill";
                    button.append(name, action);
                    results.append(button);
                });
            });

            search.addEventListener("keydown", event => {
                if (event.key === "ArrowDown" && !results.hidden) {
                    event.preventDefault();
                    results.querySelector("button")?.focus();
                }
                if (event.key === "Enter") event.preventDefault();
                if (event.key === "Escape") {
                    results.hidden = true;
                    searchStatus.textContent = "";
                }
            });

            results.addEventListener("keydown", event => {
                if (event.key !== "ArrowDown" && event.key !== "ArrowUp" && event.key !== "Escape") return;
                event.preventDefault();
                const buttons = Array.from(results.querySelectorAll("button"));
                const index = buttons.indexOf(document.activeElement);
                if (event.key === "Escape" || (event.key === "ArrowUp" && index === 0)) {
                    search.focus();
                } else {
                    buttons[Math.max(0, Math.min(buttons.length - 1, index + (event.key === "ArrowDown" ? 1 : -1)))]?.focus();
                }
            });

            addForm.addEventListener("submit", event => {
                const choice = event.submitter;
                if (!choice?.classList.contains("student-skill-search-result")) {
                    event.preventDefault();
                    return;
                }

                // Disabled submit buttons are omitted from form data, so retain the selection explicitly.
                const selected = document.createElement("input");
                selected.type = "hidden";
                selected.name = "SkillId";
                selected.value = choice.value;
                addForm.append(selected);
                results.querySelectorAll("button").forEach(button => { button.disabled = true; });
                choice.querySelector("small").textContent = "Adding…";
                search.readOnly = true;
            });
        }
    }

    const dialog = document.querySelector("#studentSkillAssessment");
    const config = document.querySelector("#studentSkillAssessmentConfig");
    if (!dialog || !config) return;

    const title = dialog.querySelector("[data-student-quiz-skill-name]");
    const language = dialog.querySelector("[data-student-quiz-language]");
    const loading = dialog.querySelector("[data-student-quiz-loading]");
    const errorBox = dialog.querySelector("[data-student-quiz-error]");
    const errorCopy = dialog.querySelector("[data-student-quiz-error-copy]");
    const retryButton = dialog.querySelector("[data-student-retry-quiz]");
    const questions = dialog.querySelector("[data-student-quiz-questions]");
    const progress = dialog.querySelector("[data-student-quiz-progress]");
    const result = dialog.querySelector("[data-student-quiz-result]");
    const score = dialog.querySelector("[data-student-quiz-score]");
    const resultCopy = dialog.querySelector("[data-student-quiz-result-copy]");
    const submit = dialog.querySelector("[data-student-submit-quiz]");
    const closeButtons = dialog.querySelectorAll("[data-student-close-quiz]");
    const state = { skillId: 0, skillName: "", questionnaire: null, selections: new Map(), saved: false, saving: false, request: null, revision: 0, previousOverflow: "" };

    const post = async (url, body, signal) => {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": config.querySelector('input[name="__RequestVerificationToken"]')?.value || ""
            },
            body: JSON.stringify(body),
            signal
        });
        let data;
        try { data = await response.json(); }
        catch { throw new Error("We could not read the quiz response. Please try again."); }
        if (!response.ok || !data?.success) throw new Error(data?.message || "The quiz is temporarily unavailable. Please try again.");
        return data;
    };

    const showError = (message, allowRetry) => {
        errorCopy.textContent = message;
        errorBox.hidden = false;
        retryButton.hidden = !allowRetry;
    };

    const updateProgress = () => {
        const total = state.questionnaire?.questions.length || 0;
        progress.textContent = `${state.selections.size}/${total} answered`;
        submit.disabled = total === 0 || state.selections.size !== total || state.saving;
    };

    const renderQuestions = () => {
        questions.replaceChildren();
        state.questionnaire.questions.forEach((question, index) => {
            const fieldset = document.createElement("fieldset");
            fieldset.className = "student-quiz-question";
            const legend = document.createElement("legend");
            const number = document.createElement("span");
            number.textContent = `${index + 1}.`;
            legend.append(number, document.createTextNode(question.text));
            const options = document.createElement("div");
            options.className = "student-quiz-options";
            question.options.forEach(option => {
                const label = document.createElement("label");
                label.className = "student-quiz-option";
                const input = document.createElement("input");
                input.type = "radio";
                input.name = `student-question-${index}`;
                input.value = option.id;
                input.addEventListener("change", () => {
                    state.selections.set(question.id, option.id);
                    updateProgress();
                });
                const copy = document.createElement("span");
                copy.textContent = option.label;
                label.append(input, copy);
                options.append(label);
            });
            fieldset.append(legend, options);
            questions.append(fieldset);
        });
        updateProgress();
    };

    const loadQuiz = async () => {
        if (!dialog.open || state.saving || state.saved) return;
        state.request?.abort();
        const controller = new AbortController();
        state.request = controller;
        const revision = ++state.revision;
        state.questionnaire = null;
        state.selections.clear();
        questions.replaceChildren();
        result.hidden = true;
        errorBox.hidden = true;
        loading.hidden = false;
        submit.hidden = false;
        submit.textContent = "Finish and save score";
        updateProgress();
        const timeout = window.setTimeout(() => controller.abort(), 120000);
        try {
            const data = await post(config.dataset.generateUrl, { skillId: state.skillId, skillName: state.skillName, language: language.value }, controller.signal);
            if (revision !== state.revision || !dialog.open) return;
            const questionnaire = data.questionnaire;
            if (!questionnaire?.questionnaireId || !Array.isArray(questionnaire.questions) || questionnaire.questions.length === 0
                || questionnaire.questions.some(question => !question.id || !question.text || !Array.isArray(question.options) || question.options.length < 2)) {
                throw new Error("We could not prepare a complete quiz. Please try again.");
            }
            state.questionnaire = questionnaire;
            renderQuestions();
        } catch (error) {
            if (revision !== state.revision || !dialog.open) return;
            showError(error.name === "AbortError" ? "Preparing the quiz took too long. Please try again." : error.message, true);
        } finally {
            window.clearTimeout(timeout);
            if (revision === state.revision) loading.hidden = true;
        }
    };

    const openQuiz = (skillId, skillName) => {
        if (dialog.open) return;
        state.skillId = Number.parseInt(skillId, 10) || 0;
        state.skillName = skillName || "Skill";
        state.saved = false;
        state.saving = false;
        language.disabled = false;
        closeButtons.forEach(button => { button.disabled = false; });
        state.previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        title.textContent = state.skillName;
        dialog.showModal();
        dialog.scrollTop = 0;
        loadQuiz();
    };

    const closeQuiz = () => {
        if (!state.saving) dialog.close();
    };

    dialog.addEventListener("close", () => {
        ++state.revision;
        state.request?.abort();
        document.body.style.overflow = state.previousOverflow;
        if (state.saved) window.location.reload();
    });
    dialog.addEventListener("cancel", event => {
        event.preventDefault();
        closeQuiz();
    });
    dialog.addEventListener("click", event => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) closeQuiz();
    });
    closeButtons.forEach(button => button.addEventListener("click", closeQuiz));
    retryButton.addEventListener("click", loadQuiz);
    language.addEventListener("change", loadQuiz);
    document.querySelectorAll("[data-student-assess-skill]").forEach(button => {
        button.addEventListener("click", () => openQuiz(button.dataset.skillId, button.dataset.skillName));
    });

    submit.addEventListener("click", async () => {
        const questionnaire = state.questionnaire;
        if (!questionnaire || state.saving || state.selections.size !== questionnaire.questions.length) return;
        state.saving = true;
        submit.disabled = true;
        submit.textContent = "Saving score…";
        language.disabled = true;
        closeButtons.forEach(button => { button.disabled = true; });
        questions.querySelectorAll("input").forEach(input => { input.disabled = true; });
        errorBox.hidden = true;
        const controller = new AbortController();
        const timeout = window.setTimeout(() => controller.abort(), 60000);
        try {
            const data = await post(config.dataset.submitUrl, {
                skillId: state.skillId,
                skillName: state.skillName,
                questionnaireId: questionnaire.questionnaireId,
                answers: questionnaire.questions.map(question => ({ questionId: question.id, selectedOptionIds: [state.selections.get(question.id)] }))
            }, controller.signal);
            state.saved = true;
            questions.replaceChildren();
            score.textContent = String(Math.round(Math.max(0, Math.min(100, Number(data.score) || 0))));
            resultCopy.textContent = "Your knowledge score has been saved and contributes to your SSI.";
            result.hidden = false;
            progress.textContent = "Completed";
            submit.hidden = true;
            dialog.scrollTop = 0;
        } catch (error) {
            showError(error.name === "AbortError" ? "Saving took too long. Try saving your answers again." : error.message, false);
            submit.textContent = "Finish and save score";
            language.disabled = false;
            questions.querySelectorAll("input").forEach(input => { input.disabled = false; });
        } finally {
            window.clearTimeout(timeout);
            state.saving = false;
            closeButtons.forEach(button => { button.disabled = false; });
            updateProgress();
            if (state.saved) progress.textContent = "Completed";
        }
    });

    if (config.dataset.autoSkillName && Number(config.dataset.autoSkillId) > 0) {
        openQuiz(config.dataset.autoSkillId, config.dataset.autoSkillName);
    }
})();
