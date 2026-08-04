(() => {
    "use strict";

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
