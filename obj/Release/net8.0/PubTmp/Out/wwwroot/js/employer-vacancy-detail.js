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
    const funnelBoard = document.getElementById("vacancyFunnelBoard");
    const funnelMessage = document.getElementById("funnelMoveMessage");
    const meetingDialog = document.getElementById("meetingDialog");
    const meetingForm = document.getElementById("meetingForm");
    const meetingApplicationId = document.getElementById("meetingApplicationId");
    const meetingSubject = document.getElementById("meetingSubject");
    const meetingCandidateSummary = document.getElementById("meetingCandidateSummary");
    const meetingLocalStart = document.getElementById("meetingLocalStart");
    const meetingStartAtUtc = document.getElementById("meetingStartAtUtc");

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

    const showFunnelMessage = (message, isError = false) => {
        if (!funnelMessage)
            return;

        funnelMessage.textContent = message;
        funnelMessage.classList.toggle("error", isError);
        funnelMessage.hidden = false;
    };

    const updateFunnelColumnState = column => {
        if (!(column instanceof HTMLElement))
            return;

        const cardList = column.querySelector("[data-funnel-card-list]");
        const count = column.querySelector("[data-funnel-count]");
        const empty = column.querySelector("[data-funnel-empty]");
        const cardCount = cardList
            ? cardList.querySelectorAll(":scope > [data-funnel-card]").length
            : 0;

        if (count)
            count.textContent = String(cardCount);
        if (empty)
            empty.hidden = cardCount !== 0;
    };

    if (funnelBoard) {
        const columns = Array.from(
            funnelBoard.querySelectorAll("[data-funnel-column]"));
        const antiForgeryToken = document.querySelector(
            ".funnel-antiforgery-token input[name='__RequestVerificationToken']")
            ?.value ?? "";
        let draggedCard = null;

        funnelBoard.querySelectorAll("[data-funnel-card]").forEach(card => {
            card.addEventListener("dragstart", event => {
                if (!(card instanceof HTMLElement))
                    return;

                draggedCard = card;
                card.classList.add("dragging");
                event.dataTransfer?.setData(
                    "text/plain",
                    card.dataset.applicationId ?? "");
                if (event.dataTransfer)
                    event.dataTransfer.effectAllowed = "move";
            });

            card.addEventListener("dragend", () => {
                card.classList.remove("dragging");
                columns.forEach(column =>
                    column.classList.remove("drag-over"));
                draggedCard = null;
            });
        });

        columns.forEach(column => {
            column.addEventListener("dragover", event => {
                if (!draggedCard)
                    return;

                event.preventDefault();
                if (event.dataTransfer)
                    event.dataTransfer.dropEffect = "move";
                column.classList.add("drag-over");
            });

            column.addEventListener("dragleave", event => {
                if (!column.contains(event.relatedTarget))
                    column.classList.remove("drag-over");
            });

            column.addEventListener("drop", async event => {
                event.preventDefault();
                column.classList.remove("drag-over");

                const card = draggedCard;
                const targetList = column.querySelector(
                    "[data-funnel-card-list]");
                const targetStage = column.dataset.stageName ?? "";

                if (!(card instanceof HTMLElement)
                    || !(targetList instanceof HTMLElement)
                    || !targetStage
                    || card.dataset.currentStage === targetStage
                    || card.classList.contains("saving"))
                {
                    return;
                }

                const sourceColumn = card.closest("[data-funnel-column]");
                const sourceList = card.parentElement;
                const sourceStage = card.dataset.currentStage ?? "";
                const sourceNextSibling = card.nextElementSibling;
                const emptyTarget = targetList.querySelector(
                    ":scope > [data-funnel-empty]");

                targetList.insertBefore(card, emptyTarget);
                card.dataset.currentStage = targetStage;
                card.classList.add("saving");
                updateFunnelColumnState(sourceColumn);
                updateFunnelColumnState(column);

                try {
                    const response = await fetch(card.dataset.moveUrl ?? "", {
                        method: "POST",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            "RequestVerificationToken": antiForgeryToken,
                            "X-Requested-With": "XMLHttpRequest"
                        },
                        credentials: "same-origin",
                        body: JSON.stringify({ stageName: targetStage })
                    });
                    let payload = {};

                    try {
                        payload = await response.json();
                    } catch {
                        payload = {};
                    }

                    if (!response.ok || payload.success !== true) {
                        throw new Error(
                            payload.message
                            || "Candidate stage could not be changed.");
                    }

                    card.dataset.currentStage =
                        payload.stageName || targetStage;
                    showFunnelMessage(
                        payload.message
                        || `${card.dataset.candidateName ?? "Candidate"} moved to ${targetStage}.`);
                } catch (error) {
                    if (sourceList) {
                        sourceList.insertBefore(
                            card,
                            sourceNextSibling?.parentElement === sourceList
                                ? sourceNextSibling
                                : sourceList.querySelector(
                                    ":scope > [data-funnel-empty]"));
                    }

                    card.dataset.currentStage = sourceStage;
                    updateFunnelColumnState(sourceColumn);
                    updateFunnelColumnState(column);
                    showFunnelMessage(
                        error instanceof Error
                            ? error.message
                            : "Candidate stage could not be changed.",
                        true);
                } finally {
                    card.classList.remove("saving");
                }
            });
        });

        columns.forEach(updateFunnelColumnState);
    }

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

    const toLocalInputValue = date => {
        const pad = value => String(value).padStart(2, "0");
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
            + `T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    };

    const getDefaultMeetingStart = () => {
        const date = new Date(Date.now() + 30 * 60 * 1000);
        date.setSeconds(0, 0);
        date.setMinutes(Math.ceil(date.getMinutes() / 15) * 15);
        return date;
    };

    document.querySelectorAll("[data-schedule-meeting]").forEach(button => {
        button.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();

            if (!(meetingDialog instanceof HTMLDialogElement))
                return;

            if (meetingApplicationId)
                meetingApplicationId.value = button.dataset.applicationId ?? "";
            if (meetingSubject)
                meetingSubject.value = button.dataset.meetingSubject ?? "Interview";
            if (meetingCandidateSummary) {
                const name = button.dataset.candidateName ?? "Candidate";
                const email = button.dataset.candidateEmail ?? "";
                meetingCandidateSummary.textContent = email
                    ? `${name} · ${email}`
                    : name;
            }
            if (meetingLocalStart) {
                const start = getDefaultMeetingStart();
                meetingLocalStart.value = toLocalInputValue(start);
                meetingLocalStart.min = toLocalInputValue(new Date());
            }

            meetingDialog.showModal();
            meetingSubject?.focus();
        });
    });

    document.querySelectorAll("[data-meeting-close]").forEach(button => {
        button.addEventListener("click", () => meetingDialog?.close());
    });

    meetingDialog?.addEventListener("click", event => {
        if (event.target === meetingDialog)
            meetingDialog.close();
    });

    meetingForm?.addEventListener("submit", event => {
        if (!meetingLocalStart?.value) {
            event.preventDefault();
            meetingLocalStart?.focus();
            return;
        }

        const localDate = new Date(meetingLocalStart.value);
        if (Number.isNaN(localDate.getTime())) {
            event.preventDefault();
            meetingLocalStart.setCustomValidity("Select a valid date and time.");
            meetingLocalStart.reportValidity();
            return;
        }

        meetingLocalStart.setCustomValidity("");
        if (meetingStartAtUtc)
            meetingStartAtUtc.value = localDate.toISOString();

        const submitButton = meetingForm.querySelector("button[type='submit']");
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.textContent = "Creating meeting…";
        }
    });

    selectTab("analytics");
})();
