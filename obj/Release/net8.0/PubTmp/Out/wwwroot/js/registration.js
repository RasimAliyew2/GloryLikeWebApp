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
    const studentSection = document.querySelector('.student-only-fields');
    const studentInputs = [...document.querySelectorAll('.student-only-fields input')];
    const studentNote = document.getElementById('studentRegistrationNote');
    const socialLinks = [...document.querySelectorAll('.social-buttons a')];
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
    const isTeamInvitation =
        form?.dataset.teamInvitation === "true";

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
        ["candidate", "student"].includes(accountTypeInput.value) ? accountTypeInput.value : "employer";

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
            !isTeamInvitation && ["candidate", "student"].includes(type) ? type : "employer";
        accountTypeInput.value = accountType;

        const isEmployer = accountType === "employer";
        const isStudent = accountType === "student";
        if (studentSection) studentSection.hidden = !isStudent;
        if (studentNote) studentNote.hidden = !isStudent;
        studentInputs.forEach(input => { input.disabled = !isStudent; input.required = isStudent; input.classList.remove('user-invalid'); });
        socialLinks.forEach(link => {
            const url = new URL(link.href, location.origin);
            url.searchParams.set('accountType', isStudent ? 'student' : 'candidate');
            link.href = url.toString();
        });

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

        industryInput.required =
            isEmployer && !isTeamInvitation;
        profileNameLabel.textContent =
            isTeamInvitation
                ? "FULL NAME"
                : isEmployer
                ? "COMPANY NAME"
                : "FULL NAME";
        profileNameInput.placeholder =
            isTeamInvitation
                ? "Your Full Name"
                : isEmployer
                ? "Your Company Name"
                : "Your Full Name";
        profileNameInput.autocomplete =
            isTeamInvitation
                ? "name"
                : isEmployer
                ? "organization"
                : "name";
        emailInput.placeholder =
            isEmployer
                ? "hr@company.com"
                : isStudent ? "you@university.edu" : "you@example.com";
        submitButton.textContent =
            isTeamInvitation
                ? "Accept Invitation"
                : isEmployer
                ? "Create a Business Profile"
                : isStudent ? "Create a Student Profile" : "Create a Candidate Profile";
        registrationNote.textContent =
            isTeamInvitation
                ? "After email verification, your name will automatically appear in the company team."
                : isEmployer
                ? "You will be able to create your first vacancy immediately after registration."
                : isStudent ? "Start with your profile — build your skills and discover internships in your Student home."
                : "You will be able to complete your skills and career profile after registration.";

        setBenefitsType(isEmployer ? "employer" : "candidate");
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

        if (accountType === "employer"
            && !isTeamInvitation) {
            inputsToValidate.push(industryInput);
        }

        if (accountType === "student") inputsToValidate.push(...studentInputs);

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
        submitButton.textContent =
            "Sending verification code...";
        setFeedback();
    });

    setAccountType(accountType, false);
    syncSubmitState();
})();
