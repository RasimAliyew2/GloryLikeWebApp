(() => {
    "use strict";

    const readJson = (id) => {
        try { return JSON.parse(document.getElementById(id)?.textContent || "[]"); }
        catch { return []; }
    };
    const normalize = (value) => String(value || "").trim().replace(/\s+/g, " ");
    const normalizePosition = (position) => ({
        name: normalize(position?.name),
        seniority: normalize(position?.seniority) || "Not specified",
        headcount: Number.isInteger(Number(position?.headcount)) && Number(position.headcount) > 0
            ? Number(position.headcount)
            : 1,
        reportsTo: normalize(position?.reportsTo)
    });

    const state = readJson("companyStructureData").map(department => ({
        name: normalize(department.name),
        divisions: (department.divisions || []).map(division => ({
            name: normalize(division.name),
            positions: (division.positions || []).map(normalizePosition)
        }))
    }));

    const token = document.querySelector('#structureSecurityToken input[name="__RequestVerificationToken"]')?.value || "";
    const message = document.getElementById("structureMessage");
    const modal = document.getElementById("structureNameModal");
    const modalTitle = document.getElementById("structureModalTitle");
    const modalLabel = document.getElementById("structureNameLabel");
    const modalInput = document.getElementById("structureNameInput");
    const modalForm = document.getElementById("structureNameForm");
    const positionFields = document.getElementById("structurePositionFields");
    const seniorityInput = document.getElementById("structureSeniorityInput");
    const headcountInput = document.getElementById("structureHeadcountInput");
    const reportsToInput = document.getElementById("structureReportsToInput");
    const uploadInput = document.getElementById("structureExcelInput");
    let pendingAction = null;

    const isPositionAction = (action) => [
        "add-position",
        "add-direct-position",
        "rename-position"
    ].includes(action);

    const showMessage = (text, isError = false) => {
        message.textContent = text;
        message.classList.toggle("error", isError);
        message.hidden = false;
        message.scrollIntoView({ behavior: "smooth", block: "center" });
    };

    const save = async () => {
        const response = await fetch("/Employer/Company/Structure/Save", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify({ departments: state })
        });
        const payload = await response.json();
        if (!response.ok || !payload.success)
            throw new Error(payload.message || "Company structure could not be saved.");
        window.location.reload();
    };

    const closeModal = () => {
        modal.hidden = true;
        document.body.style.overflow = "";
        pendingAction = null;
        seniorityInput.required = false;
        headcountInput.required = false;
    };

    const openModal = (button) => {
        const action = button.dataset.structureAction;
        const departmentIndex = Number(button.dataset.departmentIndex);
        const divisionIndex = Number(button.dataset.divisionIndex);
        const positionIndex = Number(button.dataset.positionIndex);
        let title = "Add department";
        let label = "Department name";
        let value = "";
        let position = null;

        if (action === "rename-department") {
            title = "Rename department";
            value = state[departmentIndex]?.name || "";
        } else if (action === "add-division") {
            title = "Add division";
            label = "Division name";
        } else if (action === "rename-division") {
            title = "Rename division";
            label = "Division name";
            value = state[departmentIndex]?.divisions?.[divisionIndex]?.name || "";
        } else if (action === "add-position" || action === "add-direct-position") {
            title = "Add position";
            label = "Position title";
        } else if (action === "rename-position") {
            title = "Edit position";
            label = "Position title";
            position = state[departmentIndex]?.divisions?.[divisionIndex]?.positions?.[positionIndex];
            value = position?.name || "";
        }

        const positionAction = isPositionAction(action);
        pendingAction = { action, departmentIndex, divisionIndex, positionIndex };
        modalTitle.textContent = title;
        modalLabel.textContent = label;
        modalInput.maxLength = positionAction ? 160 : 120;
        modalInput.value = value;
        positionFields.hidden = !positionAction;
        seniorityInput.required = positionAction;
        headcountInput.required = positionAction;
        seniorityInput.value = position?.seniority || "";
        headcountInput.value = String(position?.headcount || 1);
        reportsToInput.value = position?.reportsTo || "";
        modal.hidden = false;
        document.body.style.overflow = "hidden";
        window.setTimeout(() => { modalInput.focus(); modalInput.select(); }, 20);
    };

    document.querySelectorAll("[data-structure-action]").forEach(button => {
        button.addEventListener("click", async event => {
            event.preventDefault();
            event.stopPropagation();
            const action = button.dataset.structureAction;
            const departmentIndex = Number(button.dataset.departmentIndex);
            const divisionIndex = Number(button.dataset.divisionIndex);
            const positionIndex = Number(button.dataset.positionIndex);

            if (!action.startsWith("delete-")) {
                openModal(button);
                return;
            }

            const labels = {
                "delete-department": state[departmentIndex]?.name,
                "delete-division": state[departmentIndex]?.divisions?.[divisionIndex]?.name,
                "delete-position": state[departmentIndex]?.divisions?.[divisionIndex]?.positions?.[positionIndex]?.name
            };
            if (!window.confirm(`Delete '${labels[action] || "this item"}'?`)) return;

            if (action === "delete-department") state.splice(departmentIndex, 1);
            if (action === "delete-division") state[departmentIndex].divisions.splice(divisionIndex, 1);
            if (action === "delete-position") {
                const division = state[departmentIndex].divisions[divisionIndex];
                division.positions.splice(positionIndex, 1);
                if (!division.name && division.positions.length === 0)
                    state[departmentIndex].divisions.splice(divisionIndex, 1);
            }

            try { await save(); }
            catch (error) { showMessage(error.message, true); }
        });
    });

    modalForm?.addEventListener("submit", async event => {
        event.preventDefault();
        const name = normalize(modalInput.value);
        if (!name || !pendingAction) return;

        const { action, departmentIndex, divisionIndex, positionIndex } = pendingAction;
        let position = null;
        if (isPositionAction(action)) {
            const headcount = Number(headcountInput.value);
            const seniority = normalize(seniorityInput.value);
            if (!seniority) {
                seniorityInput.focus();
                return;
            }
            if (!Number.isInteger(headcount) || headcount < 1 || headcount > 10000) {
                headcountInput.setCustomValidity("Headcount must be a whole number between 1 and 10000.");
                headcountInput.reportValidity();
                headcountInput.setCustomValidity("");
                return;
            }
            position = {
                name,
                seniority,
                headcount,
                reportsTo: normalize(reportsToInput.value)
            };
        }

        if (action === "add-department") state.push({ name, divisions: [] });
        if (action === "rename-department") state[departmentIndex].name = name;
        if (action === "add-division") state[departmentIndex].divisions.push({ name, positions: [] });
        if (action === "rename-division") state[departmentIndex].divisions[divisionIndex].name = name;
        if (action === "add-position") state[departmentIndex].divisions[divisionIndex].positions.push(position);
        if (action === "add-direct-position") {
            let directDivision = state[departmentIndex].divisions.find(division => !division.name);
            if (!directDivision) {
                directDivision = { name: "", positions: [] };
                state[departmentIndex].divisions.unshift(directDivision);
            }
            directDivision.positions.push(position);
        }
        if (action === "rename-position")
            state[departmentIndex].divisions[divisionIndex].positions[positionIndex] = position;

        const button = document.getElementById("structureSaveNameButton");
        button.disabled = true;
        button.textContent = "Saving...";
        try { await save(); }
        catch (error) { closeModal(); showMessage(error.message, true); }
        finally { button.disabled = false; button.textContent = "Save"; }
    });

    document.querySelectorAll("[data-close-structure-modal]").forEach(button =>
        button.addEventListener("click", closeModal));

    document.getElementById("structureUploadButton")?.addEventListener("click", () => {
        uploadInput.value = "";
        uploadInput.click();
    });

    uploadInput?.addEventListener("change", async () => {
        const file = uploadInput.files?.[0];
        if (!file) return;
        const data = new FormData();
        data.append("__RequestVerificationToken", token);
        data.append("file", file);

        showMessage("Validating and uploading company structure Excel...");
        try {
            const response = await fetch("/Employer/Company/Structure/Import", {
                method: "POST",
                body: data
            });
            const payload = await response.json();
            if (!response.ok || !payload.success)
                throw new Error(payload.message || "Company structure Excel could not be imported.");
            window.location.reload();
        } catch (error) {
            showMessage(error.message, true);
        }
    });

    document.getElementById("employerMenuButton")?.addEventListener("click", () =>
        document.getElementById("employerSidebar")?.classList.toggle("open"));
})();
