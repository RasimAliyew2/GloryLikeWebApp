(() => {
    const parseJson = (id, fallback) => {
        const element = document.getElementById(id);
        if (!element) return fallback;
        try {
            return JSON.parse(element.textContent || "") ?? fallback;
        } catch {
            return fallback;
        }
    };

    const templates = parseJson("companyTemplatesData", []);
    const variables = parseJson("companyTemplateVariables", []);
    const context = parseJson("companyTemplatesContext", {});
    const canManage = context.canManageTemplates === true;
    const byId = new Map(templates.map((item) => [String(item.id), item]));

    const modal = document.getElementById("templateModal");
    const form = document.getElementById("templateForm");
    const idInput = document.getElementById("templateId");
    const nameInput = document.getElementById("templateName");
    const audienceInput = document.getElementById("templateAudience");
    const categoryInput = document.getElementById("templateCategory");
    const subjectInput = document.getElementById("templateSubject");
    const bodyInput = document.getElementById("templateBody");
    const variablesHost = document.getElementById("templateVariables");
    const title = document.getElementById("templateModalTitle");
    const kicker = document.getElementById("templateModalKicker");
    const deleteButton = document.getElementById("deleteTemplateButton");
    const saveButton = document.getElementById("saveTemplateButton");
    const message = document.getElementById("templatesMessage");

    if (!modal || !form || !idInput || !nameInput || !audienceInput
        || !categoryInput || !subjectInput || !bodyInput || !variablesHost
        || !title || !kicker || !deleteButton || !saveButton) return;

    const inputs = [nameInput, audienceInput, categoryInput, subjectInput, bodyInput];
    let activeTemplate = null;

    const showMessage = (text, isError = false) => {
        if (!message) return;
        message.textContent = text;
        message.classList.toggle("error", isError);
        message.hidden = !text;
        message.scrollIntoView({ behavior: "smooth", block: "nearest" });
    };

    const setBusy = (busy) => {
        saveButton.disabled = busy;
        deleteButton.disabled = busy;
        saveButton.textContent = busy ? "Saving..." : "Save template";
    };

    const setEditorAccess = (editable) => {
        inputs.forEach((input) => { input.disabled = !editable; });
        saveButton.hidden = !editable;
        deleteButton.hidden = !editable || !activeTemplate;
    };

    const renderVariables = () => {
        variablesHost.replaceChildren();
        variables.forEach((value) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = value;
            button.disabled = !canManage;
            button.addEventListener("click", () => {
                const start = bodyInput.selectionStart ?? bodyInput.value.length;
                const end = bodyInput.selectionEnd ?? start;
                bodyInput.setRangeText(value, start, end, "end");
                bodyInput.focus();
            });
            variablesHost.appendChild(button);
        });
    };

    const openModal = (template = null) => {
        activeTemplate = template;
        form.reset();
        idInput.value = template?.id || "";
        nameInput.value = template?.name || "";
        audienceInput.value = template?.audience || "Candidate";
        categoryInput.value = template?.category || "General";
        subjectInput.value = template?.subject || "";
        bodyInput.value = template?.body || "";

        title.textContent = template?.name || "New letter template";
        kicker.textContent = template
            ? template.isDefault ? "DEFAULT LETTER TEMPLATE" : "COMPANY LETTER TEMPLATE"
            : "NEW COMPANY TEMPLATE";
        form.action = template
            ? `/Employer/Company/Templates/${template.id}/Update`
            : "/Employer/Company/Templates";

        setEditorAccess(canManage);
        modal.hidden = false;
        document.body.classList.add("template-modal-open");
        window.setTimeout(() => (canManage ? nameInput : modal.querySelector("[data-close-template]"))?.focus(), 0);
    };

    const closeModal = () => {
        modal.hidden = true;
        document.body.classList.remove("template-modal-open");
        activeTemplate = null;
    };

    renderVariables();

    document.querySelectorAll("[data-new-template]").forEach((button) => {
        button.addEventListener("click", () => openModal());
    });

    document.querySelectorAll("[data-template-id]").forEach((button) => {
        button.addEventListener("click", () => {
            const template = byId.get(button.dataset.templateId || "");
            if (template) openModal(template);
        });
    });

    modal.querySelectorAll("[data-close-template]").forEach((button) => {
        button.addEventListener("click", closeModal);
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !modal.hidden) closeModal();
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (!canManage) return;

        setBusy(true);
        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                credentials: "same-origin",
                headers: { Accept: "application/json" }
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                throw new Error(result.message || "Template could not be saved.");
            }

            closeModal();
            showMessage(result.message || "Template saved.");
            window.setTimeout(() => window.location.reload(), 450);
        } catch (error) {
            showMessage(error.message || "Template could not be saved.", true);
        } finally {
            setBusy(false);
        }
    });

    deleteButton.addEventListener("click", async () => {
        if (!canManage || !activeTemplate) return;
        const scopeText = activeTemplate.isDefault
            ? "This default template will be removed only for your company."
            : "This custom template will be permanently deleted.";
        if (!window.confirm(`${scopeText}\n\nContinue?`)) return;

        setBusy(true);
        try {
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
            const data = new FormData();
            data.append("__RequestVerificationToken", token);
            const response = await fetch(
                `/Employer/Company/Templates/${activeTemplate.id}/Delete`,
                {
                    method: "POST",
                    body: data,
                    credentials: "same-origin",
                    headers: { Accept: "application/json" }
                });
            const result = await response.json();
            if (!response.ok || !result.success) {
                throw new Error(result.message || "Template could not be deleted.");
            }

            closeModal();
            showMessage(result.message || "Template deleted.");
            window.setTimeout(() => window.location.reload(), 450);
        } catch (error) {
            showMessage(error.message || "Template could not be deleted.", true);
        } finally {
            setBusy(false);
        }
    });
})();
