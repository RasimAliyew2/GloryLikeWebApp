(() => {
    "use strict";

    const form = document.getElementById("companyProfileForm");
    if (!form) return;

    const byId = id => document.getElementById(id);
    const initialDataElement = byId("companyProfileInitialData");
    const benefitCatalogElement = byId("companyBenefitCatalog");
    const pageContextElement = byId("companyPageContext");
    const completionBar = byId("companyCompletionBar");
    const completionValue = byId("companyCompletionValue");
    const saveButton = byId("companySaveButton");
    const previewButton = byId("companyPreviewButton");
    const previewDialog = byId("companyPreviewDialog");
    const previewRoot = byId("companyPreviewRoot");
    const benefitInput = byId("benefitInput");
    const benefitList = byId("companyBenefitList");
    const benefitSuggestions = byId("companyBenefitSuggestions");
    const locationList = byId("companyLocationList");
    const websiteInput = byId("website");
    const companyNameInput = byId("companyName");
    const companyNameError = byId("companyNameError");
    const layoutList = byId("companyLayoutList");
    const layoutInput = byId("companyAboutPageLayoutJson");
    const customHtmlInput = byId("companyAboutPageCustomHtml");
    const useCustomInput = byId("companyUseCustomAboutPageHtml");
    const aboutMode = byId("companyAboutMode");
    const htmlDialog = byId("companyHtmlDialog");
    const htmlEditor = byId("companyHtmlEditor");
    const customHtmlEnabled = byId("companyCustomHtmlEnabled");
    const htmlPreviewRoot = byId("companyHtmlPreviewRoot");
    const aiPrompt = byId("companyAiPrompt");
    const aiMessage = byId("companyAiMessage");
    const shareDialog = byId("companyShareDialog");
    const shareLinkInput = byId("companyShareLink");
    const shareButton = byId("companyShareButton");
    const toast = byId("companyToast");

    let benefitCatalog = [];
    let companyOwnerUserId = 0;
    let toastTimer = 0;
    let galleryObjectUrls = [];
    let htmlPreviewTimer = 0;
    const defaultHtmlTemplate = htmlEditor?.value || "";

    try { benefitCatalog = JSON.parse(benefitCatalogElement?.textContent || "[]"); }
    catch { benefitCatalog = []; }
    try { companyOwnerUserId = Number(JSON.parse(pageContextElement?.textContent || "{}").companyOwnerUserId || 0); }
    catch { companyOwnerUserId = 0; }

    const showToast = (message, isError = false) => {
        if (!toast) return;
        window.clearTimeout(toastTimer);
        toast.textContent = message;
        toast.classList.toggle("error", isError);
        toast.classList.add("visible");
        toastTimer = window.setTimeout(() => toast.classList.remove("visible"), 3200);
    };

    const getBenefits = () => Array.from(
        benefitList?.querySelectorAll("[data-benefit-value]") || [])
        .map(item => item.dataset.benefitValue || "")
        .filter(Boolean);

    const getLocations = () => Array.from(locationList?.children || []).map(card => {
        const value = field => card.querySelector(`[data-location-field="${field}"]`)?.value?.trim() || "";
        return {
            id: Number(value("Id")) || null,
            name: value("Name"),
            address: value("Address"),
            country: value("Country"),
            city: value("City")
        };
    }).filter(item => item.name || item.address || item.country || item.city);

    const isFieldFilled = field => {
        if (field instanceof HTMLInputElement && field.type === "file")
            return Boolean(field.files?.length);
        if (field === benefitList) return getBenefits().length > 0;
        return Boolean(field.value?.trim());
    };

    const updateCompletion = () => {
        const fields = Array.from(form.querySelectorAll("[data-profile-field]"));
        const filled = fields.filter(isFieldFilled).length;
        const percentage = fields.length ? Math.round((filled / fields.length) * 100) : 0;
        if (completionBar) completionBar.style.width = `${percentage}%`;
        if (completionValue) completionValue.textContent = `${percentage}% Filled`;
    };

    const renderBenefitSuggestions = queryValue => {
        if (!benefitSuggestions) return;
        const query = String(queryValue || "").trim().toLowerCase();
        benefitSuggestions.replaceChildren();
        if (!query) { benefitSuggestions.hidden = true; return; }
        const selected = new Set(getBenefits().map(item => item.toLowerCase()));
        benefitCatalog
            .filter(item => String(item).toLowerCase().includes(query))
            .filter(item => !selected.has(String(item).toLowerCase()))
            .slice(0, 8)
            .forEach(value => {
                const button = document.createElement("button");
                button.type = "button";
                button.textContent = value;
                button.addEventListener("click", () => {
                    addBenefit(value);
                    benefitInput.value = "";
                    renderBenefitSuggestions("");
                    benefitInput.focus();
                });
                benefitSuggestions.append(button);
            });
        benefitSuggestions.hidden = benefitSuggestions.childElementCount === 0;
    };

    const addBenefit = rawValue => {
        const value = String(rawValue || "").trim();
        if (!benefitList || !value) return;
        if (getBenefits().some(item => item.toLowerCase() === value.toLowerCase())) {
            showToast("This benefit is already added.", true); return;
        }
        if (getBenefits().length >= 12) { showToast("You can add up to 12 benefits.", true); return; }
        const chip = document.createElement("span");
        chip.className = "company-benefit-chip";
        chip.dataset.benefitValue = value;
        const label = document.createElement("span");
        label.textContent = value;
        const remove = document.createElement("button");
        remove.type = "button";
        remove.textContent = "×";
        remove.setAttribute("aria-label", `Remove ${value}`);
        remove.addEventListener("click", () => { chip.remove(); updateCompletion(); });
        chip.append(label, remove);
        benefitList.append(chip);
        updateCompletion();
    };

    const createLocationField = (labelText, fieldName, value, maxLength, placeholder) => {
        const wrapper = document.createElement("div");
        wrapper.className = "company-field";
        const label = document.createElement("label");
        label.textContent = labelText;
        const input = document.createElement("input");
        input.type = fieldName === "Id" ? "hidden" : "text";
        input.dataset.locationField = fieldName;
        input.value = value || "";
        input.maxLength = maxLength;
        input.placeholder = placeholder || "";
        if (fieldName !== "Id") input.dataset.profileField = "";
        wrapper.append(label, input);
        return fieldName === "Id" ? input : wrapper;
    };

    const reindexLocations = () => {
        Array.from(locationList?.children || []).forEach((card, index) => {
            card.querySelector("[data-location-title]").textContent = `Location ${index + 1}`;
            card.querySelectorAll("[data-location-field]").forEach(input => {
                input.name = `Locations[${index}].${input.dataset.locationField}`;
            });
        });
        updateCompletion();
    };

    const addLocation = (location = {}) => {
        if (!locationList) return;
        if (locationList.childElementCount >= 20) { showToast("You can add up to 20 locations.", true); return; }
        const card = document.createElement("article");
        card.className = "company-location-card";
        const header = document.createElement("header");
        const title = document.createElement("strong");
        title.dataset.locationTitle = "";
        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "company-location-remove";
        remove.textContent = "×";
        remove.setAttribute("aria-label", "Remove location");
        remove.addEventListener("click", () => { card.remove(); reindexLocations(); });
        header.append(title, remove);
        const grid = document.createElement("div");
        grid.className = "company-location-grid";
        grid.append(
            createLocationField("Location name", "Name", location.name, 120, "e.g. Head Office"),
            createLocationField("Address", "Address", location.address, 240, "Street, building, office"),
            createLocationField("Country", "Country", location.country, 100, "Azerbaijan"),
            createLocationField("City", "City", location.city, 100, "Baku"));
        card.append(header, createLocationField("", "Id", Number(location.id) > 0 ? String(location.id) : "", 20), grid);
        locationList.append(card);
        reindexLocations();
    };

    const normalizeWebsite = () => {
        if (!websiteInput) return;
        const value = websiteInput.value.trim();
        if (!value) return;
        if (value.startsWith("//")) websiteInput.value = `https:${value}`;
        else if (!/^[a-z][a-z\d+.-]*:\/\//i.test(value)) websiteInput.value = `https://${value}`;
    };

    const canvasBlob = (canvas, quality) => new Promise(resolve => canvas.toBlob(resolve, "image/jpeg", quality));
    const blobToDataUrl = blob => new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result || ""));
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });

    const createCropper = options => {
        const input = byId(options.inputId);
        const hidden = byId(options.hiddenId);
        const preview = byId(options.previewId);
        const dialog = byId(options.dialogId);
        const canvas = byId(options.canvasId);
        const stage = byId(options.stageId);
        const zoom = byId(options.zoomId);
        let image = null;
        let objectUrl = "";
        let zoomValue = 1;
        let offsetX = 0;
        let offsetY = 0;
        let dragging = false;
        let pointerX = 0;
        let pointerY = 0;

        const setDataUrl = value => {
            if (hidden) hidden.value = value || "";
            if (preview) { preview.src = value || ""; preview.hidden = !value; }
            input?.closest(".company-upload-tile")?.classList.toggle("has-file", Boolean(value));
            updateCompletion();
        };

        const close = () => {
            if (dialog?.open) dialog.close();
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = "";
            image = null;
            if (input) input.value = "";
        };

        const draw = () => {
            if (!canvas || !image) return;
            const context = canvas.getContext("2d");
            const width = canvas.width;
            const height = canvas.height;
            const baseScale = Math.max(width / image.naturalWidth, height / image.naturalHeight);
            const scale = baseScale * zoomValue;
            const drawWidth = image.naturalWidth * scale;
            const drawHeight = image.naturalHeight * scale;
            const maxX = Math.max(0, (drawWidth - width) / 2);
            const maxY = Math.max(0, (drawHeight - height) / 2);
            offsetX = Math.max(-maxX, Math.min(maxX, offsetX));
            offsetY = Math.max(-maxY, Math.min(maxY, offsetY));
            context.fillStyle = "#171923";
            context.fillRect(0, 0, width, height);
            context.drawImage(image, (width - drawWidth) / 2 + offsetX, (height - drawHeight) / 2 + offsetY, drawWidth, drawHeight);
        };

        const open = file => {
            if (!dialog || !canvas) return;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = URL.createObjectURL(file);
            const loaded = new Image();
            loaded.onload = () => {
                image = loaded; zoomValue = 1; offsetX = 0; offsetY = 0;
                if (zoom) zoom.value = "1";
                draw(); dialog.showModal();
            };
            loaded.onerror = () => { close(); showToast(`${options.label} could not be opened.`, true); };
            loaded.src = objectUrl;
        };

        const createOutput = async () => {
            if (!canvas || !image) return "";
            const output = document.createElement("canvas");
            output.width = options.outputWidth;
            output.height = options.outputHeight;
            output.getContext("2d").drawImage(canvas, 0, 0, output.width, output.height);
            for (const quality of [0.92, 0.86, 0.8, 0.72, 0.64, 0.56, 0.48]) {
                const blob = await canvasBlob(output, quality);
                if (blob && blob.size <= options.maxBytes) return blobToDataUrl(blob);
            }
            return "";
        };

        zoom?.addEventListener("input", () => { zoomValue = Number(zoom.value || 1); draw(); });
        stage?.addEventListener("pointerdown", event => {
            if (!image) return;
            dragging = true; pointerX = event.clientX; pointerY = event.clientY;
            stage.setPointerCapture(event.pointerId);
        });
        stage?.addEventListener("pointermove", event => {
            if (!dragging) return;
            const ratioX = canvas.width / Math.max(stage.clientWidth, 1);
            const ratioY = canvas.height / Math.max(stage.clientHeight, 1);
            offsetX += (event.clientX - pointerX) * ratioX;
            offsetY += (event.clientY - pointerY) * ratioY;
            pointerX = event.clientX; pointerY = event.clientY; draw();
        });
        ["pointerup", "pointercancel"].forEach(name => stage?.addEventListener(name, () => { dragging = false; }));
        byId(options.saveId)?.addEventListener("click", async event => {
            const button = event.currentTarget;
            button.disabled = true;
            const original = button.textContent;
            button.textContent = "Saving...";
            try {
                const value = await createOutput();
                if (!value) throw new Error(`${options.label} could not be reduced below ${options.limitLabel}.`);
                setDataUrl(value);
                const title = input?.closest(".company-upload-tile")?.querySelector("[data-upload-title]");
                if (title) title.textContent = `${options.label} ready`;
                close();
            } catch (error) { showToast(error.message || `${options.label} could not be saved.`, true); }
            finally { button.disabled = false; button.textContent = original; }
        });
        options.closeIds.forEach(id => byId(id)?.addEventListener("click", close));
        dialog?.addEventListener("cancel", event => { event.preventDefault(); close(); });

        return { input, open, setDataUrl };
    };

    const logoCropper = createCropper({
        inputId: "companyLogo", hiddenId: "companyLogoDataUrl", previewId: "companyLogoUploadPreview",
        dialogId: "companyLogoCropDialog", canvasId: "companyLogoCropCanvas", stageId: "companyLogoCropStage",
        zoomId: "companyLogoCropZoom", saveId: "companyLogoCropSave",
        closeIds: ["companyLogoCropClose", "companyLogoCropCancel"],
        outputWidth: 512, outputHeight: 512, maxBytes: 350 * 1024, label: "Logo", limitLabel: "350KB"
    });
    const coverCropper = createCropper({
        inputId: "companyCover", hiddenId: "companyCoverImageDataUrl", previewId: "companyCoverUploadPreview",
        dialogId: "companyCoverCropDialog", canvasId: "companyCoverCropCanvas", stageId: "companyCoverCropStage",
        zoomId: "companyCoverCropZoom", saveId: "companyCoverCropSave",
        closeIds: ["companyCoverCropClose", "companyCoverCropCancel"],
        outputWidth: 1600, outputHeight: 600, maxBytes: 700 * 1024, label: "Cover", limitLabel: "700KB"
    });

    const validateFiles = input => {
        const files = Array.from(input.files || []);
        const maxSize = Number(input.dataset.maxSize || 0);
        const invalid = files.find(file => maxSize && file.size > maxSize);
        if (invalid) { input.value = ""; showToast(`${invalid.name} exceeds the allowed file size.`, true); return false; }
        if (input.multiple && files.length > 8) { input.value = ""; showToast("You can upload up to 8 gallery photos.", true); return false; }
        return true;
    };

    const renderGallery = input => {
        const gallery = byId("companyGallery");
        galleryObjectUrls.forEach(url => URL.revokeObjectURL(url));
        galleryObjectUrls = [];
        document.querySelectorAll(".company-gallery-preview").forEach(item => item.remove());
        Array.from(input.files || []).forEach(file => {
            const url = URL.createObjectURL(file);
            galleryObjectUrls.push(url);
            const preview = document.createElement("div");
            preview.className = "company-gallery-preview";
            const image = document.createElement("img");
            image.src = url; image.alt = file.name;
            preview.append(image);
            gallery.insertBefore(preview, gallery.lastElementChild);
        });
    };

    const layoutKeys = () => Array.from(layoutList?.querySelectorAll("[data-layout-key]") || [])
        .map(item => item.dataset.layoutKey);
    const syncLayout = () => {
        if (layoutInput) layoutInput.value = JSON.stringify(layoutKeys());
        Array.from(layoutList?.children || []).forEach((item, index, all) => {
            item.querySelector("[data-layout-up]").disabled = index === 0;
            item.querySelector("[data-layout-down]").disabled = index === all.length - 1;
        });
    };
    const restoreLayout = raw => {
        let keys = [];
        try { keys = JSON.parse(raw || "[]"); } catch { keys = []; }
        if (!Array.isArray(keys)) keys = [];
        keys.forEach(key => {
            const item = layoutList?.querySelector(`[data-layout-key="${CSS.escape(key)}"]`);
            if (item) layoutList.append(item);
        });
        syncLayout();
    };

    const collectProfile = () => {
        const value = id => byId(id)?.value?.trim() || "";
        return {
            companyName: value("companyName"), companyType: value("companyType"), activityScope: value("activityScope"),
            foundationYear: value("foundationYear"), employeeCount: value("employeeCount"), website: value("website"),
            pageLanguage: value("pageLanguage"), companyVideo: value("companyVideo"), companyDescription: value("companyDescription"),
            companyCulture: value("companyCulture"), whyWorkWithUs: value("whyWorkWithUs"), benefits: getBenefits(),
            logoDataUrl: value("companyLogoDataUrl"), coverImageDataUrl: value("companyCoverImageDataUrl"), locations: getLocations(),
            linkedInUrl: value("linkedInUrl"), instagramUrl: value("instagramUrl"), facebookUrl: value("facebookUrl"),
            youtubeUrl: value("youtubeUrl"), telegramUrl: value("telegramUrl"), tiktokUrl: value("tiktokUrl"),
            aboutPageLayoutJson: layoutInput?.value || "[]", aboutPageCustomHtml: customHtmlInput?.value || "",
            useCustomAboutPageHtml: useCustomInput?.value === "true"
        };
    };

    const slugify = value => String(value || "company").normalize("NFKD")
        .replace(/[\u0300-\u036f]/g, "").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || "company";
    const vacanciesUrl = () => `/companies/${companyOwnerUserId}/vacancies`;
    const renderPage = (root, profile) => window.CompanyPageRenderer?.render(root, profile, {
        vacanciesUrl: vacanciesUrl(), vacancyCount: 0
    });

    const updateAboutMode = () => {
        const active = useCustomInput?.value === "true";
        if (aboutMode) aboutMode.textContent = active ? "Custom HTML" : "Standard layout";
        aboutMode?.classList.toggle("custom", active);
    };

    const openPreview = () => {
        renderPage(previewRoot, collectProfile());
        previewDialog?.showModal();
    };

    const renderHtmlPreview = () => {
        const profile = collectProfile();
        profile.aboutPageCustomHtml = htmlEditor?.value || "";
        profile.useCustomAboutPageHtml = true;
        renderPage(htmlPreviewRoot, profile);
    };
    const scheduleHtmlPreview = () => {
        window.clearTimeout(htmlPreviewTimer);
        htmlPreviewTimer = window.setTimeout(renderHtmlPreview, 180);
    };

    const selectCustomizerTab = tab => {
        const ai = tab === "ai";
        byId("companyHtmlTab")?.classList.toggle("active", !ai);
        byId("companyAiTab")?.classList.toggle("active", ai);
        byId("companyHtmlPanel").hidden = ai;
        byId("companyAiPanel").hidden = !ai;
        if (ai) aiPrompt?.focus(); else htmlEditor?.focus();
    };
    const buildCurrentStandardHtml = () => {
        const documentValue = new DOMParser().parseFromString(
            `<div id="standard-template-root">${defaultHtmlTemplate}</div>`,
            "text/html");
        const root = documentValue.getElementById("standard-template-root");
        const sections = new Map(Array.from(root?.querySelectorAll("[data-company-section]") || [])
            .map(section => [section.dataset.companySection, section]));
        const parent = sections.values().next().value?.parentElement;
        if (parent) layoutKeys().forEach(key => { const section = sections.get(key); if (section) parent.append(section); });
        return root?.innerHTML || defaultHtmlTemplate;
    };
    const openCustomizer = tab => {
        htmlEditor.value = customHtmlInput?.value.trim()
            ? customHtmlInput.value
            : buildCurrentStandardHtml();
        customHtmlEnabled.checked = useCustomInput?.value === "true";
        if (aiMessage) { aiMessage.textContent = ""; aiMessage.classList.remove("error", "success"); }
        selectCustomizerTab(tab);
        renderHtmlPreview();
        htmlDialog?.showModal();
    };
    const cancelCustomizer = () => {
        htmlEditor.value = customHtmlInput?.value.trim()
            ? customHtmlInput.value
            : buildCurrentStandardHtml();
        customHtmlEnabled.checked = useCustomInput?.value === "true";
        htmlDialog?.close();
    };

    const runAi = async () => {
        const prompt = aiPrompt?.value.trim() || "";
        if (prompt.length < 3) { aiMessage.textContent = "Describe the design you want."; aiMessage.className = "company-ai-message error"; return; }
        const button = byId("companyRunAiButton");
        const original = button.textContent;
        button.disabled = true; button.textContent = "✦ Designing...";
        aiMessage.textContent = "AI is checking the request and preparing a safe layout...";
        aiMessage.className = "company-ai-message";
        const data = new FormData();
        data.append("Prompt", prompt);
        data.append("CurrentHtml", htmlEditor.value);
        data.append("__RequestVerificationToken", form.querySelector('[name="__RequestVerificationToken"]')?.value || "");
        try {
            const response = await fetch(htmlDialog.dataset.aiUrl, { method: "POST", body: data, headers: { "X-Requested-With": "XMLHttpRequest" } });
            const result = await response.json();
            if (!response.ok || !result?.success || !result?.allowed) throw new Error(result?.message || "AI could not create a safe design.");
            htmlEditor.value = result.html || htmlEditor.value;
            customHtmlEnabled.checked = true;
            aiMessage.textContent = result.message || "Safe design is ready. Review it and click Apply design.";
            aiMessage.className = "company-ai-message success";
            renderHtmlPreview();
        } catch (error) {
            aiMessage.textContent = error.message || "AI design could not be generated.";
            aiMessage.className = "company-ai-message error";
        } finally { button.disabled = false; button.textContent = original; }
    };

    const validateCompanyName = () => {
        const valid = Boolean(companyNameInput?.value.trim());
        companyNameInput?.setAttribute("aria-invalid", valid ? "false" : "true");
        if (companyNameError) companyNameError.textContent = valid ? "" : "Company name is required.";
        return valid;
    };

    const saveProfile = async () => {
        normalizeWebsite(); syncLayout();
        if (!validateCompanyName()) { companyNameInput?.focus(); showToast("Enter the company name before saving.", true); return; }
        if (!form.checkValidity()) { form.reportValidity(); return; }
        const formData = new FormData(form);
        ["companyLogo", "companyCover", "companyGalleryFiles", "Benefits"].forEach(name => formData.delete(name));
        getBenefits().forEach(value => formData.append("Benefits", value));
        saveButton.disabled = true;
        const original = saveButton.textContent;
        saveButton.textContent = "Saving...";
        try {
            const response = await fetch(form.action, { method: "POST", body: formData, headers: { "X-Requested-With": "XMLHttpRequest" } });
            const result = await response.json();
            if (!response.ok || !result?.success) throw new Error(result?.message || "Company profile could not be saved.");
            if (Number(result.companyOwnerUserId) > 0) companyOwnerUserId = Number(result.companyOwnerUserId);
            if (result.profile?.aboutPageCustomHtml != null) {
                customHtmlInput.value = result.profile.aboutPageCustomHtml;
                htmlEditor.value = result.profile.aboutPageCustomHtml || htmlEditor.value;
            }
            shareButton.disabled = companyOwnerUserId <= 0;
            showToast(result.message || "Company profile saved for the whole team.");
        } catch (error) { showToast(error.message || "Company profile could not be saved.", true); }
        finally { saveButton.disabled = false; saveButton.textContent = original; }
    };

    const restoreProfile = () => {
        let profile = {};
        try { profile = JSON.parse(initialDataElement?.textContent || "{}"); }
        catch { showToast("Company profile data could not be loaded.", true); }
        const skip = new Set(["benefits", "locations", "logoDataUrl", "coverImageDataUrl", "aboutPageLayoutJson", "aboutPageCustomHtml", "useCustomAboutPageHtml", "updatedAtUtc"]);
        Object.entries(profile).forEach(([name, value]) => {
            if (skip.has(name)) return;
            const element = form.elements.namedItem(name);
            if (element instanceof HTMLInputElement || element instanceof HTMLSelectElement || element instanceof HTMLTextAreaElement)
                element.value = String(value ?? "");
        });
        (profile.benefits || []).forEach(addBenefit);
        const locations = Array.isArray(profile.locations) ? profile.locations : [];
        if (locations.length) locations.forEach(addLocation);
        else if (profile.companyAddress || profile.companyCountry || profile.companyCity)
            addLocation({ address: profile.companyAddress, country: profile.companyCountry, city: profile.companyCity });
        logoCropper.setDataUrl(profile.logoDataUrl || "");
        coverCropper.setDataUrl(profile.coverImageDataUrl || "");
        restoreLayout(profile.aboutPageLayoutJson);
        customHtmlInput.value = profile.aboutPageCustomHtml || htmlEditor?.value || "";
        if (profile.aboutPageCustomHtml) htmlEditor.value = profile.aboutPageCustomHtml;
        useCustomInput.value = profile.useCustomAboutPageHtml ? "true" : "false";
        customHtmlEnabled.checked = profile.useCustomAboutPageHtml === true;
        updateAboutMode(); updateCompletion();
    };

    form.addEventListener("input", event => {
        if (event.target === companyNameInput) validateCompanyName();
        updateCompletion();
    });
    form.addEventListener("change", event => {
        const input = event.target;
        if (!(input instanceof HTMLInputElement) || input.type !== "file") { updateCompletion(); return; }
        if (!validateFiles(input)) { updateCompletion(); return; }
        const file = input.files?.[0];
        if (input.id === "companyLogo" && file) logoCropper.open(file);
        else if (input.id === "companyCover" && file) coverCropper.open(file);
        else if (input.id === "companyGalleryFiles") renderGallery(input);
    });

    layoutList?.addEventListener("click", event => {
        const button = event.target.closest("button");
        const item = button?.closest("[data-layout-key]");
        if (!item) return;
        if (button.matches("[data-layout-up]") && item.previousElementSibling) item.previousElementSibling.before(item);
        if (button.matches("[data-layout-down]") && item.nextElementSibling) item.nextElementSibling.after(item);
        syncLayout();
    });
    byId("addCompanyLocationButton")?.addEventListener("click", () => addLocation());
    websiteInput?.addEventListener("blur", normalizeWebsite);
    byId("addBenefitButton")?.addEventListener("click", () => { addBenefit(benefitInput?.value); benefitInput.value = ""; benefitInput.focus(); });
    benefitInput?.addEventListener("keydown", event => { if (event.key === "Enter") { event.preventDefault(); byId("addBenefitButton")?.click(); } });
    benefitInput?.addEventListener("input", () => renderBenefitSuggestions(benefitInput.value));
    benefitInput?.addEventListener("focus", () => renderBenefitSuggestions(benefitInput.value));
    document.addEventListener("click", event => {
        if (event.target !== benefitInput && !benefitSuggestions?.contains(event.target)) benefitSuggestions.hidden = true;
    });

    saveButton?.addEventListener("click", saveProfile);
    previewButton?.addEventListener("click", openPreview);
    byId("companyPreviewClose")?.addEventListener("click", () => previewDialog?.close());
    previewDialog?.addEventListener("click", event => { if (event.target === previewDialog) previewDialog.close(); });

    shareButton?.addEventListener("click", () => {
        if (companyOwnerUserId <= 0) { showToast("Save the company profile before sharing.", true); return; }
        shareLinkInput.value = `${window.location.origin}/companies/${companyOwnerUserId}/${slugify(companyNameInput?.value)}`;
        shareDialog?.showModal();
    });
    ["companyShareClose", "companyShareCloseIcon"].forEach(id => byId(id)?.addEventListener("click", () => shareDialog?.close()));
    byId("companyCopyShareLink")?.addEventListener("click", async () => {
        try { await navigator.clipboard.writeText(shareLinkInput.value); showToast("Public company link copied."); }
        catch { shareLinkInput.select(); document.execCommand("copy"); showToast("Public company link copied."); }
    });

    byId("companyCustomizeHtmlButton")?.addEventListener("click", () => openCustomizer("html"));
    byId("companyCustomizeAiButton")?.addEventListener("click", () => openCustomizer("ai"));
    byId("companyHtmlTab")?.addEventListener("click", () => selectCustomizerTab("html"));
    byId("companyAiTab")?.addEventListener("click", () => selectCustomizerTab("ai"));
    htmlEditor?.addEventListener("input", scheduleHtmlPreview);
    byId("companyRunAiButton")?.addEventListener("click", runAi);
    ["companyHtmlClose", "companyHtmlCancel"].forEach(id => byId(id)?.addEventListener("click", cancelCustomizer));
    byId("companyHtmlApply")?.addEventListener("click", () => {
        customHtmlInput.value = htmlEditor.value;
        useCustomInput.value = customHtmlEnabled.checked ? "true" : "false";
        updateAboutMode(); htmlDialog?.close();
        showToast("About page design applied. Press Save to publish it.");
    });
    htmlDialog?.addEventListener("cancel", event => { event.preventDefault(); cancelCustomizer(); });

    document.querySelectorAll("[data-future-tab]").forEach(button => button.addEventListener("click", () => showToast("This company section will be available next.")));
    restoreProfile();
})();
