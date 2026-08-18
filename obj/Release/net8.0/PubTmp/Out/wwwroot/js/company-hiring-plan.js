(() => {
    "use strict";

    const readJson = (id) => {
        try { return JSON.parse(document.getElementById(id)?.textContent || "[]"); }
        catch { return []; }
    };

    const taxonomy = readJson("hiringPlanTaxonomy");
    const plans = readJson("hiringPlanRows");
    const modal = document.getElementById("hiringPlanModal");
    const form = document.getElementById("hiringPlanForm");
    const message = document.getElementById("hiringPlanMessage");
    const title = document.getElementById("planModalTitle");
    const job = document.getElementById("planJobFamily");
    const position = document.getElementById("planPosition");
    const seniority = document.getElementById("planSeniority");
    let editingId = null;

    const option = (value, text) => {
        const item = document.createElement("option");
        item.value = String(value); item.textContent = text;
        return item;
    };

    const fillJobs = () => {
        job.innerHTML = '<option value="">Select SQL Job</option>';
        taxonomy.forEach(item => job.append(option(item.id, item.jobName)));
    };

    const fillPositions = (selectedId = null) => {
        position.innerHTML = '<option value="">Select SQL Position</option>';
        const selectedJob = taxonomy.find(item => item.id === Number(job.value));
        (selectedJob?.positions || []).forEach(item => position.append(option(item.id, item.name)));
        position.disabled = !selectedJob;
        if (selectedId) position.value = String(selectedId);
        fillSeniorities();
    };

    const fillSeniorities = (selectedId = null) => {
        seniority.innerHTML = '<option value="">Select SQL Seniority</option>';
        const selectedJob = taxonomy.find(item => item.id === Number(job.value));
        const selectedPosition = selectedJob?.positions?.find(item => item.id === Number(position.value));
        (selectedPosition?.seniorities || []).forEach(item => seniority.append(option(item.id, item.name)));
        seniority.disabled = !selectedPosition;
        if (selectedId) seniority.value = String(selectedId);
    };

    const open = (plan = null) => {
        form.reset(); editingId = plan?.id || null;
        title.textContent = plan ? "Edit planned position" : "Add planned position";
        form.action = editingId
            ? `/Employer/Company/HiringPlan/${editingId}/Update`
            : "/Employer/Company/HiringPlan";
        fillJobs();
        if (plan) {
            job.value = String(plan.jobFamilyId); fillPositions(plan.positionId); fillSeniorities(plan.seniorityId);
            document.getElementById("planHeadcount").value = plan.headcount;
            document.getElementById("planPriority").value = plan.priority;
            document.getElementById("planTargetDate").value = plan.targetStartDate?.slice(0, 10) || "";
            document.getElementById("planEmploymentType").value = plan.employmentType;
            document.getElementById("planNotes").value = plan.notes || "";
        } else {
            fillPositions(); document.getElementById("planHeadcount").value = "1";
        }
        modal.hidden = false; document.body.style.overflow = "hidden"; job.focus();
    };

    const close = () => { modal.hidden = true; document.body.style.overflow = ""; };
    const showMessage = (text, isError = false) => {
        message.textContent = text; message.classList.toggle("error", isError); message.hidden = false;
        message.scrollIntoView({ behavior: "smooth", block: "center" });
    };

    fillJobs();
    job?.addEventListener("change", () => fillPositions());
    position?.addEventListener("change", () => fillSeniorities());
    document.getElementById("addHiringPlanButton")?.addEventListener("click", () => open());
    document.querySelectorAll("[data-open-plan]").forEach(button => button.addEventListener("click", () => open()));
    document.querySelectorAll("[data-close-plan]").forEach(button => button.addEventListener("click", close));

    document.querySelectorAll("[data-plan-menu]").forEach(button => button.addEventListener("click", (event) => {
        event.stopPropagation();
        const menu = button.nextElementSibling;
        document.querySelectorAll(".plan-menu").forEach(item => { if (item !== menu) item.hidden = true; });
        menu.hidden = !menu.hidden;
    }));
    document.addEventListener("click", () => document.querySelectorAll(".plan-menu").forEach(item => item.hidden = true));

    document.querySelectorAll("[data-edit-plan]").forEach(button => button.addEventListener("click", (event) => {
        event.stopPropagation();
        open(plans.find(item => item.id === Number(button.dataset.editPlan)));
    }));

    document.querySelectorAll("[data-delete-plan]").forEach(button => button.addEventListener("click", async (event) => {
        event.stopPropagation();
        if (!window.confirm("Delete this hiring plan row?")) return;
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const data = new FormData(); data.append("__RequestVerificationToken", token || "");
        try {
            const response = await fetch(button.dataset.deleteUrl, { method: "POST", body: data });
            const payload = await response.json();
            if (!response.ok || !payload.success) throw new Error(payload.message || "Hiring plan could not be deleted.");
            window.location.reload();
        } catch (error) { showMessage(error.message, true); }
    }));

    form?.addEventListener("submit", async (event) => {
        event.preventDefault();
        const saveButton = document.getElementById("saveHiringPlanButton");
        saveButton.disabled = true; saveButton.textContent = "Saving...";
        try {
            const response = await fetch(form.action, { method: "POST", body: new FormData(form) });
            const payload = await response.json();
            if (!response.ok || !payload.success) throw new Error(payload.message || "Hiring plan could not be saved.");
            window.location.reload();
        } catch (error) {
            close(); showMessage(error.message, true);
        } finally { saveButton.disabled = false; saveButton.textContent = "Save plan"; }
    });

    document.getElementById("employerMenuButton")?.addEventListener("click", () =>
        document.getElementById("employerSidebar")?.classList.toggle("open"));
})();
