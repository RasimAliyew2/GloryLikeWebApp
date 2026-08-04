(() => {
    const initialState =
        window.gloryLikeVacancyInitialState
        ?? {};

    const questionList =
        document.getElementById(
            "screeningQuestionList");

    const emptyState =
        document.getElementById(
            "screeningEmptyState");

    const addButton =
        document.getElementById(
            "addScreeningQuestionButton");

    const vacancyForm =
        document.getElementById(
            "createVacancyForm");

    const nextButton =
        document.getElementById(
            "nextStepButton");

    const screeningStage =
        document.querySelector(
            '[data-stage="2"]');

    if (!questionList || !addButton)
        return;

    const maximumQuestionCount = 20;

    const answerTypes = [
        { value: "Text", label: "Text" },
        { value: "TrueFalse", label: "True/False" },
        { value: "OneChoice", label: "One Choice" },
        { value: "ShortAnswer", label: "Short Answer" },
        { value: "Number", label: "Number" },
        { value: "Date", label: "Date" }
    ];

    const normalize = value =>
        String(value ?? "")
            .trim()
            .toLocaleLowerCase();

    const normalizeQuestion = question => {
        const requestedAnswerType = String(
            question?.answerType
            ?? question?.AnswerType
            ?? "Text").trim();

        const answerType = answerTypes.find(
            item => normalize(item.value)
                === normalize(requestedAnswerType))
            ?.value
            ?? "Text";

        const requestedRequirement = String(
            question?.requirementType
            ?? question?.RequirementType
            ?? "Required").trim();

        return {
            questionText: String(
                question?.questionText
                ?? question?.QuestionText
                ?? ""),
            answerType,
            requirementType:
                normalize(requestedRequirement)
                    === normalize("KnockOut")
                    ? "KnockOut"
                    : "Required"
        };
    };

    const initialQuestions = Array.isArray(
        initialState.screeningQuestions)
        ? initialState.screeningQuestions
        : [];

    const questions = initialQuestions
        .slice(0, maximumQuestionCount)
        .map(normalizeQuestion);

    const createRuleOption = (
        question,
        index,
        value,
        labelText) => {

        const label = document.createElement("label");
        label.className =
            `screening-rule-option ${normalize(value)}`;

        const input = document.createElement("input");
        input.type = "radio";
        input.name =
            `Input.ScreeningQuestions[${index}].RequirementType`;
        input.value = value;
        input.checked =
            question.requirementType === value;

        input.addEventListener("change", () => {
            if (input.checked)
                question.requirementType = value;
        });

        const indicator = document.createElement("span");
        indicator.className = "screening-rule-indicator";
        indicator.textContent = "✓";

        const text = document.createElement("span");
        text.textContent = labelText;

        label.append(input, indicator, text);
        return label;
    };

    const createQuestionCard = (
        question,
        index) => {

        const card = document.createElement("article");
        card.className = "screening-question-card";
        card.dataset.questionIndex = String(index);

        const header = document.createElement("div");
        header.className = "screening-question-header";

        const number = document.createElement("span");
        number.className = "screening-question-number";
        number.textContent = `${index + 1}.`;

        const questionInput = document.createElement("input");
        questionInput.type = "text";
        questionInput.className = "screening-question-text";
        questionInput.name =
            `Input.ScreeningQuestions[${index}].QuestionText`;
        questionInput.value = question.questionText;
        questionInput.maxLength = 500;
        questionInput.placeholder =
            "The text of the question...";
        questionInput.setAttribute(
            "aria-label",
            `Screening question ${index + 1}`);

        questionInput.addEventListener("input", () => {
            question.questionText = questionInput.value;
        });

        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.className = "screening-question-delete";
        deleteButton.textContent = "×";
        deleteButton.title = "Delete question";
        deleteButton.setAttribute(
            "aria-label",
            `Delete screening question ${index + 1}`);

        deleteButton.addEventListener("click", () => {
            questions.splice(index, 1);
            renderQuestions();
        });

        header.append(
            number,
            questionInput,
            deleteButton);

        const settings = document.createElement("div");
        settings.className = "screening-question-settings";

        const answerControl = document.createElement("label");
        answerControl.className = "screening-answer-control";

        const answerLabel = document.createElement("span");
        answerLabel.textContent = "Answer method";

        const answerSelect = document.createElement("select");
        answerSelect.name =
            `Input.ScreeningQuestions[${index}].AnswerType`;
        answerSelect.setAttribute(
            "aria-label",
            `Answer method for question ${index + 1}`);

        answerTypes.forEach(answerType => {
            const option = document.createElement("option");
            option.value = answerType.value;
            option.textContent = answerType.label;
            option.selected =
                answerType.value === question.answerType;

            answerSelect.appendChild(option);
        });

        answerSelect.addEventListener("change", () => {
            question.answerType = answerSelect.value;
        });

        answerControl.append(
            answerLabel,
            answerSelect);

        const ruleControl = document.createElement("div");
        ruleControl.className = "screening-rule-control";

        const ruleLabel = document.createElement("span");
        ruleLabel.className = "screening-rule-label";
        ruleLabel.textContent = "Question rule";

        const ruleOptions = document.createElement("div");
        ruleOptions.className = "screening-rule-options";

        ruleOptions.append(
            createRuleOption(
                question,
                index,
                "Required",
                "Required"),
            createRuleOption(
                question,
                index,
                "KnockOut",
                "KnockOut"));

        ruleControl.append(
            ruleLabel,
            ruleOptions);

        settings.append(
            answerControl,
            ruleControl);

        card.append(header, settings);
        return card;
    };

    const updateReviewCount = () => {
        const reviewCount =
            document.getElementById(
                "reviewScreeningQuestions");

        if (reviewCount) {
            reviewCount.textContent =
                String(questions.length);
        }
    };

    function renderQuestions(focusIndex = null) {
        questionList.replaceChildren();

        questions.forEach((question, index) => {
            questionList.appendChild(
                createQuestionCard(
                    question,
                    index));
        });

        if (emptyState) {
            emptyState.hidden =
                questions.length > 0;
        }

        const limitReached =
            questions.length
            >= maximumQuestionCount;

        addButton.disabled = limitReached;
        addButton.title = limitReached
            ? "Maximum 20 questions"
            : "Add screening question";

        updateReviewCount();

        if (Number.isInteger(focusIndex)) {
            window.setTimeout(() => {
                questionList
                    .querySelector(
                        `[data-question-index="${focusIndex}"] `
                        + ".screening-question-text")
                    ?.focus();
            }, 0);
        }
    }

    const addQuestion = () => {
        if (
            questions.length
            >= maximumQuestionCount
        ) {
            return;
        }

        const newIndex = questions.length;

        questions.push({
            questionText: "",
            answerType: "Text",
            requirementType: "Required"
        });

        renderQuestions(newIndex);
    };

    const validateQuestions = () => {
        const invalidIndex = questions.findIndex(
            question =>
                !String(question.questionText).trim());

        if (invalidIndex < 0)
            return true;

        window.alert(
            "Screening sualının mətni boş ola bilməz. "
            + "Sualı doldurun və ya silin.");

        document
            .querySelector(
                '[data-step-target="2"]')
            ?.click();

        window.setTimeout(() => {
            questionList
                .querySelector(
                    `[data-question-index="${invalidIndex}"] `
                    + ".screening-question-text")
                ?.focus();
        }, 0);

        return false;
    };

    const stopNavigationWhenInvalid = event => {
        if (!screeningStage?.classList.contains("active"))
            return;

        const target = Number(
            event.currentTarget?.dataset?.stepTarget
            ?? 3);

        if (target <= 2 || validateQuestions())
            return;

        event.preventDefault();
        event.stopImmediatePropagation();
    };

    addButton.addEventListener(
        "click",
        addQuestion);

    nextButton?.addEventListener(
        "click",
        stopNavigationWhenInvalid,
        true);

    document
        .querySelectorAll(
            '[data-step-target]')
        .forEach(button => {
            button.addEventListener(
                "click",
                stopNavigationWhenInvalid,
                true);
        });

    vacancyForm?.addEventListener(
        "submit",
        event => {
            if (validateQuestions())
                return;

            event.preventDefault();
            event.stopImmediatePropagation();
        },
        true);

    renderQuestions();

    if (Number(initialState.startStage) === 2) {
        window.setTimeout(() => {
            document
                .querySelector(
                    '[data-step-target="2"]')
                ?.click();
        }, 0);
    }
})();
