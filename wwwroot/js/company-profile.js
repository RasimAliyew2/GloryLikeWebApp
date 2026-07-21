(() => {
    const form = document.getElementById("companyProfileForm");

    if (!form)
        return;

    const completionBar =
        document.getElementById("companyCompletionBar");
    const completionValue =
        document.getElementById("companyCompletionValue");
    const saveButton =
        document.getElementById("companySaveButton");
    const previewButton =
        document.getElementById("companyPreviewButton");
    const previewDialog =
        document.getElementById("companyPreviewDialog");
    const previewClose =
        document.getElementById("companyPreviewClose");
    const benefitInput =
        document.getElementById("benefitInput");
    const addBenefitButton =
        document.getElementById("addBenefitButton");
    const benefitList =
        document.getElementById("companyBenefitList");
    const companyName =
        document.getElementById("companyName");
    const companyNameError =
        document.getElementById("companyNameError");
    const toast =
        document.getElementById("companyToast");
    const storageKey =
        form.dataset.storageKey || "skillmatch-company-profile";

    let toastTimer = null;
    let galleryObjectUrls = [];

    const showToast = (message, isError = false) => {
        if (!toast)
            return;

        window.clearTimeout(toastTimer);
        toast.textContent = message;
        toast.classList.toggle("error", isError);
        toast.classList.add("visible");

        toastTimer = window.setTimeout(() => {
            toast.classList.remove("visible");
        }, 2800);
    };

    const getBenefits = () => {
        if (!benefitList)
            return [];

        return Array.from(
            benefitList.querySelectorAll("[data-benefit-value]"))
            .map(item => item.dataset.benefitValue)
            .filter(Boolean);
    };

    const isFieldFilled = field => {
        if (field instanceof HTMLInputElement && field.type === "file")
            return Boolean(field.files?.length);

        if (field === benefitList)
            return getBenefits().length > 0;

        return Boolean(field.value?.trim());
    };

    const updateCompletion = () => {
        const fields = Array.from(
            form.querySelectorAll("[data-profile-field]"));
        const filled = fields.filter(isFieldFilled).length;
        const percentage = fields.length === 0
            ? 0
            : Math.round((filled / fields.length) * 100);

        if (completionBar)
            completionBar.style.width = `${percentage}%`;

        if (completionValue)
            completionValue.textContent = `${percentage}% Filled`;
    };

    const setSuggestionState = benefit => {
        document.querySelectorAll("[data-benefit-suggestion]")
            .forEach(button => {
                if (button.dataset.benefitSuggestion !== benefit)
                    return;

                const isSelected = getBenefits().includes(benefit);
                button.disabled = isSelected;
                button.hidden = isSelected;
            });
    };

    const addBenefit = rawValue => {
        if (!benefitList)
            return;

        const value = rawValue?.trim();
        if (!value)
            return;

        const existing = getBenefits().some(
            benefit => benefit.toLowerCase() === value.toLowerCase());

        if (existing)
        {
            showToast("This benefit is already added.", true);
            return;
        }

        if (getBenefits().length >= 12)
        {
            showToast("You can add up to 12 benefits.", true);
            return;
        }

        const chip = document.createElement("span");
        chip.className = "company-benefit-chip";
        chip.dataset.benefitValue = value;

        const label = document.createElement("span");
        label.textContent = value;

        const remove = document.createElement("button");
        remove.type = "button";
        remove.setAttribute("aria-label", `Remove ${value}`);
        remove.textContent = "×";
        remove.addEventListener("click", () => {
            chip.remove();
            setSuggestionState(value);
            updateCompletion();
        });

        chip.append(label, remove);
        benefitList.append(chip);
        setSuggestionState(value);
        updateCompletion();
    };

    const serializeProfile = () => {
        const fields = {};

        Array.from(form.elements).forEach(element => {
            if (!(element instanceof HTMLInputElement)
                && !(element instanceof HTMLSelectElement)
                && !(element instanceof HTMLTextAreaElement))
            {
                return;
            }

            if (!element.name || element.type === "file")
                return;

            fields[element.name] = element.value;
        });

        return {
            fields,
            benefits: getBenefits(),
            savedAt: new Date().toISOString()
        };
    };

    const restoreProfile = () => {
        try
        {
            const saved = window.localStorage.getItem(storageKey);
            if (!saved)
                return;

            const profile = JSON.parse(saved);

            Object.entries(profile.fields || {}).forEach(([name, value]) => {
                const element = form.elements.namedItem(name);

                if (element instanceof HTMLInputElement
                    || element instanceof HTMLSelectElement
                    || element instanceof HTMLTextAreaElement)
                {
                    element.value = String(value ?? "");
                }
            });

            (profile.benefits || []).forEach(addBenefit);
        }
        catch
        {
            window.localStorage.removeItem(storageKey);
        }
    };

    const validateCompanyName = () => {
        const isValid = Boolean(companyName?.value.trim());

        companyName?.setAttribute(
            "aria-invalid",
            isValid ? "false" : "true");

        if (companyNameError)
        {
            companyNameError.textContent = isValid
                ? ""
                : "Company name is required.";
        }

        return isValid;
    };

    const saveProfile = () => {
        if (!validateCompanyName())
        {
            companyName?.focus();
            showToast("Enter the company name before saving.", true);
            return;
        }

        if (!form.checkValidity())
        {
            form.reportValidity();
            return;
        }

        try
        {
            window.localStorage.setItem(
                storageKey,
                JSON.stringify(serializeProfile()));
            showToast("Company profile saved in this browser.");
        }
        catch
        {
            showToast("Company profile could not be saved.", true);
        }
    };

    const initialsFrom = value => {
        const parts = value
            .trim()
            .split(/\s+/)
            .filter(Boolean)
            .slice(0, 2);

        const initials = parts
            .map(part => part[0]?.toUpperCase())
            .join("");

        return initials || "CO";
    };

    const openPreview = () => {
        const name = companyName?.value.trim() || "Company name";
        const type = document.getElementById("companyType")?.value;
        const activity = document.getElementById("activityScope")?.value;
        const description = document
            .getElementById("companyDescription")
            ?.value.trim();

        const previewName = document.getElementById("companyPreviewName");
        const previewInitials = document.getElementById("companyPreviewInitials");
        const previewMeta = document.getElementById("companyPreviewMeta");
        const previewDescription = document.getElementById("companyPreviewDescription");
        const previewBenefits = document.getElementById("companyPreviewBenefits");

        if (previewName)
            previewName.textContent = name;
        if (previewInitials)
            previewInitials.textContent = initialsFrom(name);
        if (previewMeta)
        {
            previewMeta.textContent = [type, activity]
                .filter(Boolean)
                .join(" · ") || "Company profile preview";
        }
        if (previewDescription)
        {
            previewDescription.textContent = description
                || "Add a company description to see it here.";
        }
        if (previewBenefits)
        {
            previewBenefits.replaceChildren();

            getBenefits().forEach(benefit => {
                const item = document.createElement("span");
                item.textContent = benefit;
                previewBenefits.append(item);
            });
        }

        if (previewDialog?.showModal)
            previewDialog.showModal();
    };

    const validateFiles = input => {
        const files = Array.from(input.files || []);
        const maxSize = Number(input.dataset.maxSize || 0);
        const invalid = files.find(file => maxSize > 0 && file.size > maxSize);

        if (invalid)
        {
            input.value = "";
            showToast(`${invalid.name} exceeds the allowed file size.`, true);
            return false;
        }

        if (input.multiple && files.length > 8)
        {
            input.value = "";
            showToast("You can upload up to 8 gallery photos.", true);
            return false;
        }

        return true;
    };

    const updateSingleUpload = input => {
        const tile = input.closest(".company-upload-tile");
        const title = tile?.querySelector("[data-upload-title]");
        const file = input.files?.[0];
        const fallback = input.dataset.uploadKind || "File";

        tile?.classList.toggle("has-file", Boolean(file));

        if (title)
            title.textContent = file?.name || fallback;
    };

    const clearGalleryPreviews = () => {
        galleryObjectUrls.forEach(url => URL.revokeObjectURL(url));
        galleryObjectUrls = [];

        document.querySelectorAll(".company-gallery-preview")
            .forEach(item => item.remove());
    };

    const renderGallery = input => {
        const gallery = document.getElementById("companyGallery");

        if (!gallery)
            return;

        clearGalleryPreviews();

        Array.from(input.files || []).forEach(file => {
            const url = URL.createObjectURL(file);
            galleryObjectUrls.push(url);

            const preview = document.createElement("div");
            preview.className = "company-gallery-preview";

            const image = document.createElement("img");
            image.src = url;
            image.alt = file.name;

            preview.append(image);
            gallery.insertBefore(preview, gallery.lastElementChild);
        });
    };

    form.addEventListener("input", event => {
        if (event.target === companyName)
            validateCompanyName();

        updateCompletion();
    });

    form.addEventListener("change", event => {
        const input = event.target;

        if (!(input instanceof HTMLInputElement) || input.type !== "file")
        {
            updateCompletion();
            return;
        }

        if (!validateFiles(input))
        {
            if (input.id === "companyGalleryFiles")
                clearGalleryPreviews();
            else
                updateSingleUpload(input);

            updateCompletion();
            return;
        }

        if (input.id === "companyGalleryFiles")
            renderGallery(input);
        else
            updateSingleUpload(input);

        updateCompletion();
    });

    addBenefitButton?.addEventListener("click", () => {
        addBenefit(benefitInput?.value);

        if (benefitInput)
        {
            benefitInput.value = "";
            benefitInput.focus();
        }
    });

    benefitInput?.addEventListener("keydown", event => {
        if (event.key !== "Enter")
            return;

        event.preventDefault();
        addBenefitButton?.click();
    });

    document.querySelectorAll("[data-benefit-suggestion]")
        .forEach(button => {
            button.addEventListener("click", () => {
                addBenefit(button.dataset.benefitSuggestion);
            });
        });

    document.querySelectorAll("[data-future-tab]")
        .forEach(button => {
            button.addEventListener("click", () => {
                showToast("This company section will be available next.");
            });
        });

    saveButton?.addEventListener("click", saveProfile);
    previewButton?.addEventListener("click", openPreview);
    previewClose?.addEventListener("click", () => previewDialog?.close());

    previewDialog?.addEventListener("click", event => {
        if (event.target === previewDialog)
            previewDialog.close();
    });

    restoreProfile();
    updateCompletion();
})();
