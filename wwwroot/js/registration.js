(() => {
    "use strict";

    const form = document.getElementById("quickRegistrationForm");
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

    if (!form
        || !profileNameLabel
        || !profileNameInput
        || !emailInput
        || !passwordInput
        || !industryInput
        || !acceptTerms
        || !submitButton
        || !registrationNote
        || !feedback) {
        return;
    }

    let accountType = "employer";

    const setFeedback = (message = "", isSuccess = false) => {
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

    const setAccountType = (type) => {
        accountType =
            type === "candidate"
                ? "candidate"
                : "employer";

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
        submitButton.disabled = !acceptTerms.checked;
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
        event.preventDefault();

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
            setFeedback(
                "Please fill in all required fields correctly.");

            const firstInvalid =
                inputsToValidate.find(
                    (input) => !input.checkValidity());

            firstInvalid?.focus();
            return;
        }

        if (!acceptTerms.checked) {
            setFeedback(
                "Please accept the terms and privacy policy.");
            return;
        }

        setFeedback(
            "The registration form is ready. Backend account creation will be connected in the registration API step.",
            true);
    });

    setAccountType("employer");
    submitButton.disabled = true;
})();
