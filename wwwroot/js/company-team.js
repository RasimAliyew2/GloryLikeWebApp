(() => {
    "use strict";

    const removeForms = Array.from(
        document.querySelectorAll("[data-team-remove]"));

    removeForms.forEach((removeForm) => {
        removeForm.addEventListener("submit", async (event) => {
            event.preventDefault();

            if (removeForm.dataset.submitting === "true")
                return;

            const memberName =
                removeForm.dataset.memberName
                || "this team member";

            if (!window.confirm(
                `Remove ${memberName} from the team?`)) {
                return;
            }

            const removeButton =
                removeForm.querySelector("button[type='submit']");

            removeForm.dataset.submitting = "true";

            if (removeButton)
                removeButton.disabled = true;

            try {
                const response = await fetch(
                    removeForm.action,
                    {
                        method: "POST",
                        body: new FormData(removeForm),
                        headers: {
                            "X-Requested-With":
                                "XMLHttpRequest"
                        }
                    });

                let result = null;

                try {
                    result = await response.json();
                }
                catch {
                    result = null;
                }

                if (!response.ok || !result?.success) {
                    throw new Error(
                        result?.message
                        || "Team member could not be removed.");
                }

                window.location.reload();
            }
            catch (error) {
                window.alert(
                    error instanceof Error
                        ? error.message
                        : "Team member could not be removed.");

                removeForm.dataset.submitting = "false";

                if (removeButton)
                    removeButton.disabled = false;
            }
        });
    });

    const roleForms = Array.from(
        document.querySelectorAll("[data-team-role-form]"));

    roleForms.forEach((roleForm) => {
        const select = roleForm.querySelector("[data-team-role-select]");

        select?.addEventListener("change", async () => {
            if (roleForm.dataset.submitting === "true")
                return;

            const originalRole = select.dataset.originalRole || "";
            const formData = new FormData(roleForm);
            roleForm.dataset.submitting = "true";
            select.disabled = true;

            try {
                const response = await fetch(roleForm.action, {
                    method: "POST",
                    body: formData,
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                const result = await response.json();
                if (!response.ok || !result?.success) {
                    throw new Error(
                        result?.message || "Access level could not be updated.");
                }

                select.dataset.originalRole = select.value;
                window.location.reload();
            }
            catch (error) {
                select.value = originalRole;
                window.alert(
                    error instanceof Error
                        ? error.message
                        : "Access level could not be updated.");
                roleForm.dataset.submitting = "false";
                select.disabled = false;
            }
        });
    });

    const modal =
        document.getElementById("teamInviteModal");
    const form =
        document.getElementById("teamInviteForm");
    const emailInput =
        document.getElementById("teamInviteEmail");
    const submitButton =
        document.getElementById("teamInviteSubmit");
    const feedback =
        document.getElementById("teamInviteFeedback");
    const openButtons = Array.from(
        document.querySelectorAll(
            "#openTeamInvite, [data-open-team-invite]"));
    const closeButtons = Array.from(
        document.querySelectorAll(
            "[data-close-team-invite]"));

    if (!modal
        || !form
        || !emailInput
        || !submitButton
        || openButtons.length === 0) {
        return;
    }

    let submitting = false;

    const setFeedback = (
        message = "",
        success = false) => {
        if (!feedback)
            return;

        feedback.textContent = message;
        feedback.hidden = !message;
        feedback.classList.toggle(
            "success",
            success);
    };

    const openModal = () => {
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("team-modal-open");
        setFeedback();

        window.requestAnimationFrame(() => {
            emailInput.focus();
        });
    };

    const closeModal = () => {
        if (submitting)
            return;

        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("team-modal-open");
        setFeedback();
    };

    openButtons.forEach((button) => {
        button.addEventListener("click", openModal);
    });

    closeButtons.forEach((button) => {
        button.addEventListener("click", closeModal);
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape"
            && modal.classList.contains("is-open")) {
            closeModal();
        }
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (submitting)
            return;

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        submitting = true;
        submitButton.disabled = true;
        submitButton.textContent = "Sending...";
        setFeedback();

        try {
            const response = await fetch(
                form.action,
                {
                    method: "POST",
                    body: new FormData(form),
                    headers: {
                        "X-Requested-With":
                            "XMLHttpRequest"
                    }
                });

            let result = null;

            try {
                result = await response.json();
            }
            catch {
                result = null;
            }

            if (!response.ok || !result?.success) {
                throw new Error(
                    result?.message
                    || "Invitation göndərilmədi.");
            }

            setFeedback(
                result.message
                || "Invitation email göndərildi.",
                true);

            window.setTimeout(() => {
                window.location.reload();
            }, 500);
        }
        catch (error) {
            setFeedback(
                error instanceof Error
                    ? error.message
                    : "Invitation göndərilmədi.");
            submitting = false;
            submitButton.disabled = false;
            submitButton.textContent =
                "Send Invitation";
        }
    });
})();
