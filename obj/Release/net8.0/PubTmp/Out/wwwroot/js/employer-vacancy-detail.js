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
    const meetingDate = document.getElementById("meetingDate");
    const meetingTime = document.getElementById("meetingTime");
    const meetingStartAtUtc = document.getElementById("meetingStartAtUtc");
    const meetingDuration = meetingForm?.elements.namedItem("durationMinutes");
    const meetingAvailabilityCalendar = document.getElementById("meetingAvailabilityCalendar");
    const meetingAvailabilityStatus = document.getElementById("meetingAvailabilityStatus");
    const meetingAvailabilityRange = document.getElementById("meetingAvailabilityRange");
    const candidateAvailabilityNote = document.getElementById("candidateAvailabilityNote");
    const meetingConflictMessage = document.getElementById("meetingConflictMessage");

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

    const hourHeight = 50;
    const dayHeight = hourHeight * 24;
    const pad = value => String(value).padStart(2, "0");
    const availabilityState = {
        weekStart: null,
        busySlots: [],
        loaded: false,
        requestController: null,
        scrollToBusinessHours: true
    };

    const toDateInputValue = date =>
        `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

    const toTimeInputValue = date =>
        `${pad(date.getHours())}:${pad(date.getMinutes())}`;

    const addDays = (date, days) => {
        const result = new Date(date);
        result.setDate(result.getDate() + days);
        return result;
    };

    const startOfLocalDay = date =>
        new Date(date.getFullYear(), date.getMonth(), date.getDate());

    const getWeekStart = date => {
        const result = startOfLocalDay(date);
        const mondayOffset = (result.getDay() + 6) % 7;
        result.setDate(result.getDate() - mondayOffset);
        return result;
    };

    const getDefaultMeetingStart = () => {
        const date = new Date(Date.now() + 30 * 60 * 1000);
        date.setSeconds(0, 0);
        date.setMinutes(Math.ceil(date.getMinutes() / 15) * 15);
        return date;
    };

    const normalizeManualTime = value => {
        const compact = value.trim().replace(/[^0-9:]/g, "");
        if (/^\d{1,2}:\d{2}$/.test(compact)) {
            const [hours, minutes] = compact.split(":");
            return `${pad(hours)}:${minutes}`;
        }
        if (/^\d{1,2}$/.test(compact))
            return `${pad(compact)}:00`;
        if (/^\d{3,4}$/.test(compact)) {
            const hours = compact.slice(0, -2);
            const minutes = compact.slice(-2);
            return `${pad(hours)}:${minutes}`;
        }
        return compact;
    };

    const parseSelectedStart = () => {
        const dateMatch = meetingDate?.value.match(
            /^(\d{4})-(\d{2})-(\d{2})$/);
        const timeMatch = meetingTime?.value.match(
            /^([01]\d|2[0-3]):([0-5]\d)$/);
        if (!dateMatch || !timeMatch)
            return null;

        const localDate = new Date(
            Number(dateMatch[1]),
            Number(dateMatch[2]) - 1,
            Number(dateMatch[3]),
            Number(timeMatch[1]),
            Number(timeMatch[2]),
            0,
            0);
        return Number.isNaN(localDate.getTime()) ? null : localDate;
    };

    const getDurationMinutes = () => {
        const value = Number(meetingDuration?.value ?? 0);
        return Number.isFinite(value) ? value : 0;
    };

    const formatClock = date =>
        date.toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
            hour12: false
        });

    const formatWeekRange = weekStart => {
        const weekEnd = addDays(weekStart, 6);
        const startText = weekStart.toLocaleDateString([], {
            month: "short",
            day: "numeric"
        });
        const endText = weekEnd.toLocaleDateString([], {
            month: "short",
            day: "numeric",
            year: "numeric"
        });
        return `${startText} – ${endText}`;
    };

    const overlaps = (firstStart, firstEnd, secondStart, secondEnd) =>
        firstStart < secondEnd && secondStart < firstEnd;

    const createAvailabilityElement = (tagName, className, text) => {
        const element = document.createElement(tagName);
        element.className = className;
        if (text)
            element.textContent = text;
        return element;
    };

    const addCalendarBlock = (track, slot, dayStart, dayEnd, extraClass = "") => {
        const segmentStart = slot.start < dayStart ? dayStart : slot.start;
        const segmentEnd = slot.end > dayEnd ? dayEnd : slot.end;
        if (segmentEnd <= segmentStart)
            return;

        const startsBeforeDay = slot.start <= dayStart;
        const endsAfterDay = slot.end >= dayEnd;
        const startMinutes = startsBeforeDay
            ? 0
            : segmentStart.getHours() * 60
                + segmentStart.getMinutes()
                + segmentStart.getSeconds() / 60;
        const endMinutes = endsAfterDay
            ? 1440
            : segmentEnd.getHours() * 60
                + segmentEnd.getMinutes()
                + segmentEnd.getSeconds() / 60;
        const block = createAvailabilityElement(
            "div",
            `availability-event ${slot.source} ${extraClass}`.trim());
        block.style.top = `${(startMinutes / 60) * hourHeight}px`;
        block.style.height = `${Math.max(17, ((endMinutes - startMinutes) / 60) * hourHeight - 2)}px`;
        const timeText = slot.isAllDay || startsBeforeDay && endsAfterDay
            ? "All day"
            : `${formatClock(segmentStart)}–${formatClock(segmentEnd)}`;
        block.textContent = `${timeText} ${slot.title}`;
        block.title = block.textContent;
        block.addEventListener("click", event => event.stopPropagation());
        track.append(block);
    };

    const renderSelectedTime = () => {
        meetingAvailabilityCalendar
            ?.querySelectorAll(".availability-event.selection")
            .forEach(element => element.remove());

        const start = parseSelectedStart();
        const duration = getDurationMinutes();
        const submitButton = meetingForm?.querySelector("button[type='submit']");
        if (!start || duration <= 0) {
            if (submitButton)
                submitButton.disabled = true;
            return;
        }

        const end = new Date(start.getTime() + duration * 60 * 1000);
        const displayedWeekEnd = availabilityState.weekStart
            ? addDays(availabilityState.weekStart, 7)
            : null;
        const selectionIsInDisplayedWeek = availabilityState.weekStart
            && displayedWeekEnd
            && start >= availabilityState.weekStart
            && start < displayedWeekEnd;
        const availabilityReady = availabilityState.loaded
            && selectionIsInDisplayedWeek;
        const conflicts = availabilityReady
            ? availabilityState.busySlots.filter(slot =>
                overlaps(start, end, slot.start, slot.end))
            : [];
        const hasConflict = conflicts.length > 0;

        if (meetingConflictMessage) {
            if (!availabilityReady) {
                meetingConflictMessage.textContent =
                    selectionIsInDisplayedWeek
                        ? "Outlook availability yoxlanılır…"
                        : "Open the selected date's week to verify availability.";
                meetingConflictMessage.hidden = false;
            } else if (hasConflict) {
                const sources = new Set(conflicts.map(slot => slot.source));
                const conflictOwner = sources.has("organizer")
                    && sources.has("candidate")
                    ? "HR and candidate"
                    : sources.has("organizer")
                        ? "HR"
                        : "Candidate";
                meetingConflictMessage.textContent =
                    `${conflictOwner} is busy at the selected time. Choose another time.`;
                meetingConflictMessage.hidden = false;
            } else {
                meetingConflictMessage.hidden = true;
                meetingConflictMessage.textContent = "";
            }
        }

        if (submitButton)
            submitButton.disabled = !availabilityReady || hasConflict;

        if (!availabilityState.weekStart)
            return;
        for (let index = 0; index < 7; index++) {
            const dayStart = addDays(availabilityState.weekStart, index);
            const dayEnd = addDays(dayStart, 1);
            if (!overlaps(start, end, dayStart, dayEnd))
                continue;
            const track = meetingAvailabilityCalendar?.querySelector(
                `[data-availability-day="${index}"]`);
            if (track) {
                addCalendarBlock(
                    track,
                    {
                        source: "selection",
                        title: "Selected interview",
                        start,
                        end,
                        isAllDay: false
                    },
                    dayStart,
                    dayEnd,
                    hasConflict ? "conflict" : "");
            }
        }
    };

    const chooseCalendarTime = (day, event) => {
        const rect = event.currentTarget.getBoundingClientRect();
        const relativeY = Math.max(
            0,
            Math.min(dayHeight - 1, event.clientY - rect.top));
        const rawMinutes = relativeY / dayHeight * 1440;
        const minutes = Math.min(1425, Math.round(rawMinutes / 15) * 15);
        const selected = new Date(day);
        selected.setMinutes(minutes);
        if (meetingDate)
            meetingDate.value = toDateInputValue(selected);
        if (meetingTime)
            meetingTime.value = toTimeInputValue(selected);
        renderSelectedTime();
    };

    const renderAvailabilityCalendar = () => {
        if (!meetingAvailabilityCalendar || !availabilityState.weekStart)
            return;

        const previousScrollTop = meetingAvailabilityCalendar.scrollTop;
        const previousScrollLeft = meetingAvailabilityCalendar.scrollLeft;
        const grid = createAvailabilityElement(
            "div",
            "availability-week-grid");
        const corner = createAvailabilityElement(
            "div",
            "availability-corner",
            "LOCAL");
        grid.append(corner);

        const todayValue = toDateInputValue(new Date());
        for (let index = 0; index < 7; index++) {
            const day = addDays(availabilityState.weekStart, index);
            const heading = createAvailabilityElement(
                "div",
                `availability-day-heading${toDateInputValue(day) === todayValue ? " today" : ""}`);
            heading.style.gridColumn = String(index + 2);
            const weekDay = createAvailabilityElement(
                "span",
                "",
                day.toLocaleDateString([], { weekday: "short" }));
            const dayNumber = createAvailabilityElement(
                "strong",
                "",
                String(day.getDate()));
            heading.append(weekDay, dayNumber);
            grid.append(heading);
        }

        const timeTrack = createAvailabilityElement(
            "div",
            "availability-time-track");
        for (let hour = 0; hour < 24; hour++) {
            const label = createAvailabilityElement(
                "span",
                "availability-time-label",
                `${pad(hour)}:00`);
            label.style.top = `${hour * hourHeight}px`;
            timeTrack.append(label);
        }
        grid.append(timeTrack);

        for (let index = 0; index < 7; index++) {
            const dayStart = addDays(availabilityState.weekStart, index);
            const dayEnd = addDays(dayStart, 1);
            const track = createAvailabilityElement(
                "div",
                `availability-day-track${dayStart.getDay() % 6 === 0 ? " weekend" : ""}`);
            track.style.gridColumn = String(index + 2);
            track.dataset.availabilityDay = String(index);
            track.addEventListener("click", event =>
                chooseCalendarTime(dayStart, event));

            availabilityState.busySlots
                .filter(slot => overlaps(
                    slot.start,
                    slot.end,
                    dayStart,
                    dayEnd))
                .forEach(slot => addCalendarBlock(
                    track,
                    slot,
                    dayStart,
                    dayEnd));
            grid.append(track);
        }

        meetingAvailabilityCalendar.replaceChildren(grid);
        if (availabilityState.scrollToBusinessHours) {
            meetingAvailabilityCalendar.scrollTop = 7 * hourHeight + 1;
            availabilityState.scrollToBusinessHours = false;
        } else {
            meetingAvailabilityCalendar.scrollTop = previousScrollTop;
            meetingAvailabilityCalendar.scrollLeft = previousScrollLeft;
        }
        renderSelectedTime();
    };

    const readJsonResponse = async response => {
        try {
            return await response.json();
        } catch {
            return {};
        }
    };

    const normalizeUtcIso = value => {
        const textValue = String(value ?? "");
        return /(?:z|[+-]\d{2}:\d{2})$/i.test(textValue)
            ? textValue
            : `${textValue}Z`;
    };

    const loadAvailability = async () => {
        if (!meetingForm
            || !availabilityState.weekStart
            || !meetingApplicationId?.value)
        {
            return;
        }

        availabilityState.requestController?.abort();
        const requestController = new AbortController();
        availabilityState.requestController = requestController;
        availabilityState.loaded = false;
        availabilityState.busySlots = [];
        if (meetingAvailabilityStatus) {
            meetingAvailabilityStatus.textContent = "Loading Outlook calendar…";
            meetingAvailabilityStatus.classList.remove("error");
        }
        if (candidateAvailabilityNote) {
            candidateAvailabilityNote.textContent = "";
            candidateAvailabilityNote.classList.remove("warning");
        }
        if (meetingAvailabilityRange)
            meetingAvailabilityRange.textContent = formatWeekRange(
                availabilityState.weekStart);
        renderAvailabilityCalendar();

        const vacancyId = Number(
            meetingForm.elements.namedItem("vacancyId")?.value ?? 0);
        const applicationId = Number(meetingApplicationId.value);
        const rangeStart = new Date(availabilityState.weekStart);
        const rangeEnd = addDays(rangeStart, 7);
        const antiForgeryToken = meetingForm.querySelector(
            "input[name='__RequestVerificationToken']")?.value ?? "";

        try {
            const response = await fetch(
                meetingForm.dataset.availabilityUrl ?? "",
                {
                    method: "POST",
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json",
                        "RequestVerificationToken": antiForgeryToken,
                        "X-Requested-With": "XMLHttpRequest"
                    },
                    credentials: "same-origin",
                    signal: requestController.signal,
                    body: JSON.stringify({
                        vacancyId,
                        applicationId,
                        rangeStartUtc: rangeStart.toISOString(),
                        rangeEndUtc: rangeEnd.toISOString()
                    })
                });
            const payload = await readJsonResponse(response);
            if (!response.ok || payload.success !== true)
                throw new Error(payload.message || "Outlook calendar could not be loaded.");

            availabilityState.busySlots = Array.isArray(payload.busySlots)
                ? payload.busySlots
                    .map(slot => ({
                        source: slot.source === "candidate"
                            ? "candidate"
                            : "organizer",
                        title: slot.title || "Busy",
                        start: new Date(normalizeUtcIso(slot.startAtUtc)),
                        end: new Date(normalizeUtcIso(slot.endAtUtc)),
                        isAllDay: slot.isAllDay === true
                    }))
                    .filter(slot => !Number.isNaN(slot.start.getTime())
                        && !Number.isNaN(slot.end.getTime())
                        && slot.end > slot.start)
                : [];
            availabilityState.loaded = true;
            if (meetingAvailabilityStatus) {
                meetingAvailabilityStatus.textContent =
                    `${availabilityState.busySlots.length} busy calendar block(s) found.`;
            }
            if (candidateAvailabilityNote) {
                candidateAvailabilityNote.textContent =
                    payload.candidateAvailabilityMessage || "";
                candidateAvailabilityNote.classList.toggle(
                    "warning",
                    payload.candidateAvailabilityAvailable !== true);
            }
            renderAvailabilityCalendar();
        } catch (error) {
            if (error?.name === "AbortError")
                return;
            availabilityState.loaded = false;
            if (meetingAvailabilityStatus) {
                meetingAvailabilityStatus.textContent = error instanceof Error
                    ? error.message
                    : "Outlook calendar could not be loaded.";
                meetingAvailabilityStatus.classList.add("error");
            }
            renderSelectedTime();
        }
    };

    const moveAvailabilityWeek = days => {
        if (!availabilityState.weekStart)
            return;
        availabilityState.weekStart = addDays(
            availabilityState.weekStart,
            days);
        availabilityState.scrollToBusinessHours = true;
        loadAvailability();
    };

    document.querySelector("[data-availability-previous]")
        ?.addEventListener("click", () => moveAvailabilityWeek(-7));
    document.querySelector("[data-availability-next]")
        ?.addEventListener("click", () => moveAvailabilityWeek(7));
    document.querySelector("[data-availability-today]")
        ?.addEventListener("click", () => {
            availabilityState.weekStart = getWeekStart(new Date());
            availabilityState.scrollToBusinessHours = true;
            loadAvailability();
        });

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

            const start = getDefaultMeetingStart();
            if (meetingDate) {
                meetingDate.value = toDateInputValue(start);
                meetingDate.min = toDateInputValue(new Date());
            }
            if (meetingTime)
                meetingTime.value = toTimeInputValue(start);
            availabilityState.weekStart = getWeekStart(start);
            availabilityState.loaded = false;
            availabilityState.scrollToBusinessHours = true;

            meetingDialog.showModal();
            loadAvailability();
            meetingTime?.focus();
            meetingTime?.select();
        });
    });

    document.querySelectorAll("[data-meeting-close]").forEach(button => {
        button.addEventListener("click", () => {
            availabilityState.requestController?.abort();
            meetingDialog?.close();
        });
    });

    meetingDialog?.addEventListener("click", event => {
        if (event.target === meetingDialog) {
            availabilityState.requestController?.abort();
            meetingDialog.close();
        }
    });

    meetingTime?.addEventListener("blur", () => {
        meetingTime.value = normalizeManualTime(meetingTime.value);
        renderSelectedTime();
    });
    meetingTime?.addEventListener("input", renderSelectedTime);
    meetingDuration?.addEventListener("change", renderSelectedTime);
    meetingDate?.addEventListener("change", () => {
        const selectedStart = parseSelectedStart();
        if (!selectedStart)
            return;
        const selectedWeekStart = getWeekStart(selectedStart);
        if (!availabilityState.weekStart
            || selectedWeekStart.getTime() !== availabilityState.weekStart.getTime())
        {
            availabilityState.weekStart = selectedWeekStart;
            availabilityState.scrollToBusinessHours = true;
            loadAvailability();
        } else {
            renderSelectedTime();
        }
    });

    meetingForm?.addEventListener("submit", event => {
        if (meetingTime)
            meetingTime.value = normalizeManualTime(meetingTime.value);
        const localDate = parseSelectedStart();
        const duration = getDurationMinutes();
        if (!localDate || duration <= 0) {
            event.preventDefault();
            meetingTime?.setCustomValidity(
                "Enter a valid time in 24-hour HH:mm format.");
            meetingTime?.reportValidity();
            return;
        }

        meetingTime?.setCustomValidity("");
        const displayedWeekEnd = availabilityState.weekStart
            ? addDays(availabilityState.weekStart, 7)
            : null;
        const availabilityReady = availabilityState.loaded
            && availabilityState.weekStart
            && displayedWeekEnd
            && localDate >= availabilityState.weekStart
            && localDate < displayedWeekEnd;
        if (!availabilityReady) {
            event.preventDefault();
            if (meetingConflictMessage) {
                meetingConflictMessage.textContent =
                    "Wait until Outlook availability is loaded.";
                meetingConflictMessage.hidden = false;
            }
            return;
        }

        const end = new Date(localDate.getTime() + duration * 60 * 1000);
        if (availabilityState.busySlots.some(slot =>
            overlaps(localDate, end, slot.start, slot.end)))
        {
            event.preventDefault();
            renderSelectedTime();
            return;
        }

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
