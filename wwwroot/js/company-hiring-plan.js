(() => {
    "use strict";

    const readJson = (id) => {
        try { return JSON.parse(document.getElementById(id)?.textContent || "[]"); }
        catch { return []; }
    };

    const structure = readJson("hiringPlanStructure");
    const seniorities = readJson("hiringPlanSeniorities");
    const plans = readJson("hiringPlanRows");
    const modal = document.getElementById("hiringPlanModal");
    const form = document.getElementById("hiringPlanForm");
    const message = document.getElementById("hiringPlanMessage");
    const title = document.getElementById("planModalTitle");
    const department = document.getElementById("planDepartment");
    const position = document.getElementById("planPosition");
    const seniority = document.getElementById("planSeniority");
    const excelInput = document.getElementById("hiringPlanExcelInput");
    let editingId = null;

    const option = (value, text) => {
        const item = document.createElement("option");
        item.value = String(value); item.textContent = text;
        return item;
    };

    const fillDepartments = () => {
        department.innerHTML = '<option value="">Select department</option>';
        structure.forEach(item => department.append(option(item.name, item.name)));
    };

    const fillPositions = (selectedName = null) => {
        position.innerHTML = '<option value="">Select position</option>';
        const selectedDepartment = structure.find(item => item.name === department.value);
        (selectedDepartment?.positions || []).forEach(item => position.append(option(item.name, item.name)));
        position.disabled = !selectedDepartment;
        if (selectedName) position.value = selectedName;
        fillSeniorities();
    };

    const fillSeniorities = (selectedId = null) => {
        seniority.innerHTML = '<option value="">Select SQL Seniority</option>';
        seniorities.forEach(item => seniority.append(option(item.id, item.name)));
        seniority.disabled = !position.value;
        if (selectedId) seniority.value = String(selectedId);
    };

    const open = (plan = null) => {
        form.reset(); editingId = plan?.id || null;
        title.textContent = plan ? "Edit planned position" : "Add planned position";
        form.action = editingId
            ? `/Employer/Company/HiringPlan/${editingId}/Update`
            : "/Employer/Company/HiringPlan";
        fillDepartments();
        if (plan) {
            const matchingDepartment = structure.find(item => item.name === plan.departmentName)
                || structure.find(item => (item.positions || []).some(positionItem =>
                    positionItem.name === plan.positionName));
            department.value = matchingDepartment?.name || "";
            fillPositions(plan.positionName);
            fillSeniorities(plan.seniorityId);
            document.getElementById("planHeadcount").value = plan.headcount;
            document.getElementById("planPriority").value = plan.priority;
            document.getElementById("planTargetDate").value = plan.targetStartDate?.slice(0, 10) || "";
            document.getElementById("planEmploymentType").value = plan.employmentType;
            document.getElementById("planNotes").value = plan.notes || "";
        } else {
            fillPositions(); document.getElementById("planHeadcount").value = "1";
        }
        modal.hidden = false; document.body.style.overflow = "hidden"; department.focus();
    };

    const close = () => { modal.hidden = true; document.body.style.overflow = ""; };
    const showMessage = (text, isError = false) => {
        message.textContent = text; message.classList.toggle("error", isError); message.hidden = false;
        message.scrollIntoView({ behavior: "smooth", block: "center" });
    };

    fillDepartments();
    department?.addEventListener("change", () => fillPositions());
    position?.addEventListener("change", () => fillSeniorities());
    document.getElementById("addHiringPlanButton")?.addEventListener("click", () => open());
    document.querySelectorAll("[data-open-plan]").forEach(button => button.addEventListener("click", () => open()));
    document.querySelectorAll("[data-close-plan]").forEach(button => button.addEventListener("click", close));

    document.getElementById("uploadHiringPlanButton")?.addEventListener("click", () => {
        excelInput.value = "";
        excelInput.click();
    });

    excelInput?.addEventListener("change", async () => {
        const file = excelInput.files?.[0];
        if (!file) return;

        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
        const data = new FormData();
        data.append("__RequestVerificationToken", token);
        data.append("file", file);
        showMessage("Uploading Hiring Plan Excel...");

        try {
            const response = await fetch("/Employer/Company/HiringPlan/Import", {
                method: "POST",
                body: data
            });
            const payload = await response.json();
            if (!response.ok || !payload.success)
                throw new Error(payload.message || "Hiring Plan Excel could not be imported.");
            window.location.reload();
        } catch (error) {
            showMessage(error.message, true);
        }
    });

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
