(() => {
    "use strict";

    const tabButtons = Array.from(
        document.querySelectorAll("[data-detail-tab]"));
    const panels = Array.from(
        document.querySelectorAll("[data-detail-panel]"));
    const statusForm = document.getElementById("vacancyStatusForm");
    const statusButton = document.getElementById("vacancyStatusToggle");
    const statusBadge = document.getElementById("vacancyStatusBadge");
    const statusLabel = document.getElementById("vacancyStatusToggleLabel");
    const statusIcon = document.getElementById("vacancyStatusToggleIcon");
    const statusMessage = document.getElementById("vacancyStatusMessage");
    const closeForm = document.getElementById("vacancyCloseForm");
    const closeButton = document.getElementById("vacancyCloseButton");
    const settingsStatus = document.getElementById("settingsStatusText");
    const answerDialog = document.getElementById("candidateAnswerDialog");
    const answerDialogContent = document.getElementById("candidateAnswerDialogContent");

    const selectTab = (name) => {
        tabButtons.forEach((button) => {
            const selected = button.dataset.detailTab === name;
            button.classList.toggle("active", selected);
            button.setAttribute("aria-selected", String(selected));
        });

        panels.forEach((panel) => {
            const selected = panel.dataset.detailPanel === name;
            panel.hidden = !selected;
            panel.classList.toggle("active", selected);
        });
    };

    tabButtons.forEach((button) => {
        button.addEventListener("click", () => {
            selectTab(button.dataset.detailTab ?? "analytics");
        });
    });

    const openCandidateAnswers = applicationId => {
        if (!answerDialog || !answerDialogContent)
            return;

        const template = document.querySelector(
            `[data-candidate-answer-template="${applicationId}"]`);

        if (!(template instanceof HTMLTemplateElement))
            return;

        answerDialogContent.replaceChildren(
            template.content.cloneNode(true));
        answerDialog.showModal();
    };

    document.querySelectorAll("[data-candidate-details]").forEach(row => {
        row.addEventListener("click", () => {
            openCandidateAnswers(row.dataset.candidateDetails ?? "");
        });
        row.addEventListener("keydown", event => {
            if (event.key !== "Enter" && event.key !== " ")
                return;

            event.preventDefault();
            openCandidateAnswers(row.dataset.candidateDetails ?? "");
        });
    });

    document.querySelector("[data-candidate-answer-close]")
        ?.addEventListener("click", () => answerDialog?.close());
    answerDialog?.addEventListener("click", event => {
        if (event.target === answerDialog)
            answerDialog.close();
    });

    const showStatusMessage = (message, isError) => {
        if (!statusMessage) {
            return;
        }

        statusMessage.textContent = message;
        statusMessage.classList.toggle("error", isError);
        statusMessage.hidden = false;
    };

    statusForm?.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (!statusButton || statusButton.disabled) {
            return;
        }

        const url = statusForm.dataset.toggleUrl ?? "";
        const antiForgeryToken = statusForm.querySelector(
            "input[name='__RequestVerificationToken']")?.value ?? "";

        if (!url) {
            showStatusMessage("Status endpoint is not available.", true);
            return;
        }

        statusButton.disabled = true;
        statusButton.classList.add("loading");

        try {
            const response = await fetch(url, {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "RequestVerificationToken": antiForgeryToken,
                    "X-Requested-With": "XMLHttpRequest"
                },
                credentials: "same-origin"
            });
            let payload = {};

            try {
                payload = await response.json();
            } catch {
                payload = {};
            }

            if (!response.ok || payload.success !== true) {
                throw new Error(
                    payload.message || "Vacancy status could not be changed.");
            }

            if (statusBadge) {
                statusBadge.textContent = payload.statusLabel;
                statusBadge.classList.remove("active", "suspended", "other");
                statusBadge.classList.add(payload.statusClass);
            }

            if (statusLabel) {
                statusLabel.textContent = payload.actionLabel;
            }

            if (statusIcon) {
                statusIcon.textContent = payload.actionIcon;
            }

            if (settingsStatus) {
                settingsStatus.textContent = payload.statusLabel;
            }

            showStatusMessage(
                payload.message || "Vacancy status updated.",
                false);
        } catch (error) {
            showStatusMessage(
                error instanceof Error
                    ? error.message
                    : "Vacancy status could not be changed.",
                true);
        } finally {
            statusButton.disabled = false;
            statusButton.classList.remove("loading");
        }
    });

    closeForm?.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (!closeButton || !window.confirm("Close this vacancy? This action marks the linked hiring plan vacancy as finished.")) return;

        closeButton.disabled = true;
        const antiForgeryToken = closeForm.querySelector("input[name='__RequestVerificationToken']")?.value ?? "";
        try {
            const response = await fetch(closeForm.dataset.closeUrl ?? "", {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "RequestVerificationToken": antiForgeryToken,
                    "X-Requested-With": "XMLHttpRequest"
                },
                credentials: "same-origin"
            });
            const payload = await response.json();
            if (!response.ok || payload.success !== true) throw new Error(payload.message || "Vacancy could not be closed.");

            statusBadge.textContent = payload.statusLabel;
            statusBadge.classList.remove("active", "suspended", "other");
            statusBadge.classList.add("closed");
            if (settingsStatus) settingsStatus.textContent = payload.statusLabel;
            if (statusButton) statusButton.disabled = true;
            closeForm.remove();
            showStatusMessage(payload.message || "Vacancy closed.", false);
        } catch (error) {
            showStatusMessage(error instanceof Error ? error.message : "Vacancy could not be closed.", true);
            closeButton.disabled = false;
        }
    });

    selectTab("analytics");
})();
