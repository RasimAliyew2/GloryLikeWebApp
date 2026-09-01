(() => {
    "use strict";

    const readJson = async (response) => {
        try { return await response.json(); }
        catch { return null; }
    };

    document.querySelectorAll("[data-team-remove]").forEach((form) => {
        form.addEventListener("submit", async (event) => {
            event.preventDefault();
            if (form.dataset.submitting === "true") return;
            const name = form.dataset.memberName || "this participant";
            if (!window.confirm("Revoke " + name + "'s company access?")) return;
            const button = form.querySelector("button[type='submit']");
            form.dataset.submitting = "true";
            if (button) button.disabled = true;
            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new FormData(form),
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const result = await readJson(response);
                if (!response.ok || !result?.success)
                    throw new Error(result?.message || "Access could not be revoked.");
                window.location.reload();
            } catch (error) {
                window.alert(error instanceof Error ? error.message : "Access could not be revoked.");
                form.dataset.submitting = "false";
                if (button) button.disabled = false;
            }
        });
    });

    document.querySelectorAll("[data-team-role-form]").forEach((form) => {
        const select = form.querySelector("[data-team-role-select]");
        select?.addEventListener("change", async () => {
            if (form.dataset.submitting === "true") return;
            const original = select.dataset.originalRole || "";
            form.dataset.submitting = "true";
            select.disabled = true;
            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new FormData(form),
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                const result = await readJson(response);
                if (!response.ok || !result?.success)
                    throw new Error(result?.message || "Role could not be updated.");
                select.dataset.originalRole = select.value;
                window.location.reload();
            } catch (error) {
                select.value = original;
                window.alert(error instanceof Error ? error.message : "Role could not be updated.");
                form.dataset.submitting = "false";
                select.disabled = false;
            }
        });
    });

    const participantRows = Array.from(document.querySelectorAll("[data-participant-row]"));
    const participantSearch = document.getElementById("participantSearch");
    const participantRole = document.getElementById("participantRoleFilter");
    const participantStatus = document.getElementById("participantStatusFilter");
    const participantEmpty = document.getElementById("participantEmpty");

    const filterParticipants = () => {
        if (!participantRows.length) {
            if (participantEmpty) participantEmpty.hidden = false;
            return;
        }
        const query = participantSearch?.value.trim().toLowerCase() || "";
        const role = participantRole?.value || "";
        const status = participantStatus?.value || "";
        let visible = 0;
        participantRows.forEach((row) => {
            const matches = (!query || row.dataset.search?.includes(query))
                && (!role || row.dataset.role === role)
                && (!status || row.dataset.status === status);
            row.hidden = !matches;
            if (matches) visible += 1;
        });
        if (participantEmpty) participantEmpty.hidden = visible > 0;
    };
    participantSearch?.addEventListener("input", filterParticipants);
    participantRole?.addEventListener("change", filterParticipants);
    participantStatus?.addEventListener("change", filterParticipants);
    if (participantRows.length || participantEmpty) filterParticipants();

    const historyRows = Array.from(document.querySelectorAll("[data-history-row]"));
    const historyParticipant = document.getElementById("historyParticipantFilter");
    const historyEvent = document.getElementById("historyEventFilter");
    const historyTime = document.getElementById("historyTimeFilter");
    const historyEmpty = document.getElementById("historyEmpty");
    const filterHistory = () => {
        const participant = historyParticipant?.value || "";
        const eventType = historyEvent?.value || "";
        const days = historyTime?.value || "all";
        const cutoff = days === "all" ? null : Date.now() - Number(days) * 24 * 60 * 60 * 1000;
        let visible = 0;
        historyRows.forEach((row) => {
            const involved = row.dataset.actor === participant || row.dataset.target === participant;
            const date = Date.parse(row.dataset.date || "");
            const matches = (!participant || involved)
                && (!eventType || row.dataset.event === eventType)
                && (cutoff === null || (!Number.isNaN(date) && date >= cutoff));
            row.hidden = !matches;
            if (matches) visible += 1;
        });
        if (historyEmpty) historyEmpty.hidden = visible > 0;
    };
    historyParticipant?.addEventListener("change", filterHistory);
    historyEvent?.addEventListener("change", filterHistory);
    historyTime?.addEventListener("change", filterHistory);
    if (historyRows.length || historyEmpty) filterHistory();

    const modal = document.getElementById("teamInviteModal");
    const inviteForm = document.getElementById("teamInviteForm");
    const emailInput = document.getElementById("teamInviteEmail");
    const submitButton = document.getElementById("teamInviteSubmit");
    const feedback = document.getElementById("teamInviteFeedback");
    const openButtons = document.querySelectorAll("#openTeamInvite, [data-open-team-invite]");
    const closeButtons = document.querySelectorAll("[data-close-team-invite]");
    if (!modal || !inviteForm || !emailInput || !submitButton || !openButtons.length) return;

    let submitting = false;
    const setFeedback = (message = "", success = false) => {
        if (!feedback) return;
        feedback.textContent = message;
        feedback.hidden = !message;
        feedback.classList.toggle("success", success);
    };
    const openModal = () => {
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("team-modal-open");
        setFeedback();
        window.requestAnimationFrame(() => emailInput.focus());
    };
    const closeModal = () => {
        if (submitting) return;
        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("team-modal-open");
        setFeedback();
    };
    openButtons.forEach((button) => button.addEventListener("click", openModal));
    closeButtons.forEach((button) => button.addEventListener("click", closeModal));
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && modal.classList.contains("is-open")) closeModal();
    });

    inviteForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (submitting) return;
        if (!inviteForm.checkValidity()) {
            inviteForm.reportValidity();
            return;
        }
        submitting = true;
        submitButton.disabled = true;
        submitButton.textContent = "Sending...";
        setFeedback();
        try {
            const response = await fetch(inviteForm.action, {
                method: "POST",
                body: new FormData(inviteForm),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const result = await readJson(response);
            if (!response.ok || !result?.success)
                throw new Error(result?.message || "Invitation could not be sent.");
            setFeedback(result.message || "Invitation sent.", true);
            window.setTimeout(() => window.location.reload(), 450);
        } catch (error) {
            setFeedback(error instanceof Error ? error.message : "Invitation could not be sent.");
            submitting = false;
            submitButton.disabled = false;
            submitButton.textContent = "Send invitation";
        }
    });
})();
