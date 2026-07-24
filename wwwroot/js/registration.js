(() => {
    "use strict";

    const form = document.getElementById("quickRegistrationForm");
    const accountTypeInput =
        document.getElementById("accountType");
    const accountTypeButtons = Array.from(
        document.querySelectorAll("[data-account-type]"));
    const benefitsTabs = Array.from(
        document.querySelectorAll("[data-benefits-type]"));
    const benefitsPanels = Array.from(
        document.querySelectorAll("[data-benefits-panel]"));
    const employerOnlyFields = Array.from(
        document.querySelectorAll(".employer-only-field"));
    const profileNameLabel =
        document.getElementById("profileNameLabel");
    const profileNameInput =
        document.getElementById("profileName");
    const emailInput =
        document.getElementById("registrationEmail");
    const passwordInput =
        document.getElementById("registrationPassword");
    const industryInput =
        document.getElementById("industry");
    const acceptTerms =
        document.getElementById("acceptTerms");
    const submitButton =
        document.getElementById("registrationSubmit");
    const registrationNote =
        document.getElementById("registrationNote");
    const feedback =
        document.getElementById("registrationFeedback");

    const syncSubmitState = () => {
        if (!acceptTerms || !submitButton)
            return;

        submitButton.disabled = !acceptTerms.checked;
    };

    if (acceptTerms && submitButton) {
        acceptTerms.addEventListener("input", syncSubmitState);
        acceptTerms.addEventListener("change", syncSubmitState);
        window.addEventListener("pageshow", syncSubmitState);
        syncSubmitState();
    }

    if (!form
        || !accountTypeInput
        || !profileNameLabel
        || !profileNameInput
        || !emailInput
        || !passwordInput
        || !industryInput
        || !acceptTerms
        || !submitButton
        || !registrationNote) {
        return;
    }

    let accountType =
        accountTypeInput.value === "candidate"
            ? "candidate"
            : "employer";

    const setFeedback = (message = "", isSuccess = false) => {
        if (!feedback)
            return;

        feedback.textContent = message;
        feedback.hidden = !message;
        feedback.classList.toggle("success", isSuccess);
    };

    const setBenefitsType = (type) => {
        benefitsTabs.forEach((tab) => {
            const active = tab.dataset.benefitsType === type;

            tab.classList.toggle("active", active);
            tab.setAttribute(
                "aria-selected",
                active ? "true" : "false");
        });

        benefitsPanels.forEach((panel) => {
            const active = panel.dataset.benefitsPanel === type;

            panel.classList.toggle("active", active);
            panel.hidden = !active;
        });
    };

    const setAccountType = (
        type,
        clearFeedback = true) => {
        accountType =
            type === "candidate"
                ? "candidate"
                : "employer";
        accountTypeInput.value = accountType;

        const isEmployer = accountType === "employer";

        accountTypeButtons.forEach((button) => {
            const active =
                button.dataset.accountType === accountType;

            button.classList.toggle("active", active);
            button.setAttribute(
                "aria-pressed",
                active ? "true" : "false");
        });

        employerOnlyFields.forEach((field) => {
            field.classList.toggle("is-hidden", !isEmployer);
        });

        industryInput.required = isEmployer;
        profileNameLabel.textContent =
            isEmployer
                ? "COMPANY NAME"
                : "FULL NAME";
        profileNameInput.placeholder =
            isEmployer
                ? "Your Company Name"
                : "Your Full Name";
        profileNameInput.autocomplete =
            isEmployer
                ? "organization"
                : "name";
        emailInput.placeholder =
            isEmployer
                ? "hr@company.com"
                : "you@example.com";
        submitButton.textContent =
            isEmployer
                ? "Create a Business Profile"
                : "Create a Candidate Profile";
        registrationNote.textContent =
            isEmployer
                ? "You will be able to create your first vacancy immediately after registration."
                : "You will be able to complete your skills and career profile after registration.";

        setBenefitsType(accountType);
        if (clearFeedback)
            setFeedback();
    };

    const validateInput = (input) => {
        const valid = input.checkValidity();
        input.classList.toggle("user-invalid", !valid);

        return valid;
    };

    accountTypeButtons.forEach((button) => {
        button.addEventListener("click", () => {
            setAccountType(button.dataset.accountType);
        });
    });

    benefitsTabs.forEach((tab) => {
        tab.addEventListener("click", () => {
            setBenefitsType(tab.dataset.benefitsType);
        });
    });

    acceptTerms.addEventListener("change", () => {
        setFeedback();
    });

    [
        profileNameInput,
        emailInput,
        passwordInput,
        industryInput
    ].forEach((input) => {
        input.addEventListener("input", () => {
            input.classList.remove("user-invalid");
            setFeedback();
        });

        input.addEventListener("blur", () => {
            if (input.required && input.value.length > 0) {
                validateInput(input);
            }
        });
    });

    form.addEventListener("submit", (event) => {
        const inputsToValidate = [
            profileNameInput,
            emailInput,
            passwordInput
        ];

        if (accountType === "employer")
            inputsToValidate.push(industryInput);

        const valid =
            inputsToValidate
                .map(validateInput)
                .every(Boolean);

        if (!valid) {
            event.preventDefault();
            setFeedback(
                "Please fill in all required fields correctly.");

            const firstInvalid =
                inputsToValidate.find(
                    (input) => !input.checkValidity());

            firstInvalid?.focus();
            return;
        }

        if (!acceptTerms.checked) {
            event.preventDefault();
            setFeedback(
                "Please accept the terms and privacy policy.");
            return;
        }

        submitButton.disabled = true;
        submitButton.textContent = "Sending verification code...";
        setFeedback();
    });

    setAccountType(accountType, false);
    syncSubmitState();
})();
