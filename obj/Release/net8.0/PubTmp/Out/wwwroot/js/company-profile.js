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
    const benefitSuggestions =
        document.getElementById("companyBenefitSuggestions");
    const benefitCatalogElement =
        document.getElementById("companyBenefitCatalog");
    const locationList =
        document.getElementById("companyLocationList");
    const addLocationButton =
        document.getElementById("addCompanyLocationButton");
    const websiteInput =
        document.getElementById("website");
    const logoInput =
        document.getElementById("companyLogo");
    const logoDataUrlInput =
        document.getElementById("companyLogoDataUrl");
    const logoPreview =
        document.getElementById("companyLogoUploadPreview");
    const cropDialog =
        document.getElementById("companyLogoCropDialog");
    const cropCanvas =
        document.getElementById("companyLogoCropCanvas");
    const cropStage =
        document.getElementById("companyLogoCropStage");
    const cropZoom =
        document.getElementById("companyLogoCropZoom");
    const companyName =
        document.getElementById("companyName");
    const companyNameError =
        document.getElementById("companyNameError");
    const toast =
        document.getElementById("companyToast");
    const initialDataElement =
        document.getElementById("companyProfileInitialData");

    let toastTimer = null;
    let galleryObjectUrls = [];
    let benefitCatalog = [];
    let cropImage = null;
    let cropSourceUrl = "";
    let cropZoomValue = 1;
    let cropOffsetX = 0;
    let cropOffsetY = 0;
    let cropDragging = false;
    let cropPointerX = 0;
    let cropPointerY = 0;

    try
    {
        benefitCatalog = JSON.parse(
            benefitCatalogElement?.textContent || "[]");
    }
    catch
    {
        benefitCatalog = [];
    }

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

    const renderBenefitSuggestions = rawQuery => {
        if (!benefitSuggestions)
            return;

        const query = String(rawQuery || "").trim().toLowerCase();
        benefitSuggestions.replaceChildren();

        if (!query)
        {
            benefitSuggestions.hidden = true;
            return;
        }

        const selected = new Set(
            getBenefits().map(item => item.toLowerCase()));
        const matches = benefitCatalog
            .filter(item => String(item).toLowerCase().includes(query))
            .filter(item => !selected.has(String(item).toLowerCase()))
            .slice(0, 8);

        matches.forEach(benefit => {
            const button = document.createElement("button");
            button.type = "button";
            button.dataset.benefitSuggestion = benefit;
            button.textContent = benefit;
            button.addEventListener("click", () => {
                addBenefit(benefit);
                benefitInput.value = "";
                renderBenefitSuggestions("");
                benefitInput.focus();
            });
            benefitSuggestions.append(button);
        });

        benefitSuggestions.hidden = matches.length === 0;
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
            renderBenefitSuggestions(benefitInput?.value);
            updateCompletion();
        });

        chip.append(label, remove);
        benefitList.append(chip);
        renderBenefitSuggestions(benefitInput?.value);
        updateCompletion();
    };

    const locationField = (
        labelText,
        fieldName,
        value,
        maxLength,
        placeholder) => {
        const wrapper = document.createElement("div");
        wrapper.className = "company-field";

        const label = document.createElement("label");
        label.textContent = labelText;

        const input = document.createElement("input");
        input.type = "text";
        input.dataset.locationField = fieldName;
        input.value = value || "";
        input.maxLength = maxLength;
        input.placeholder = placeholder;
        input.dataset.profileField = "";

        wrapper.append(label, input);
        return wrapper;
    };

    const reindexLocations = () => {
        if (!locationList)
            return;

        Array.from(locationList.children).forEach((card, index) => {
            card.dataset.locationIndex = String(index);
            const title = card.querySelector("[data-location-title]");
            if (title)
                title.textContent = `Location ${index + 1}`;

            card.querySelectorAll("[data-location-field]")
                .forEach(input => {
                    input.name = `Locations[${index}].${input.dataset.locationField}`;
                });
        });

        updateCompletion();
    };

    const addLocation = (location = {}) => {
        if (!locationList)
            return;

        if (locationList.children.length >= 20)
        {
            showToast("You can add up to 20 locations.", true);
            return;
        }

        const card = document.createElement("article");
        card.className = "company-location-card";

        const header = document.createElement("header");
        const heading = document.createElement("strong");
        heading.dataset.locationTitle = "";
        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "company-location-remove";
        remove.setAttribute("aria-label", "Remove location");
        remove.textContent = "×";
        remove.addEventListener("click", () => {
            card.remove();
            if (locationList.children.length === 0)
                addLocation();
            reindexLocations();
        });
        header.append(heading, remove);

        const id = document.createElement("input");
        id.type = "hidden";
        id.dataset.locationField = "Id";
        id.value = Number(location.id) > 0 ? String(location.id) : "";

        const grid = document.createElement("div");
        grid.className = "company-location-grid";
        grid.append(
            locationField(
                "Location name",
                "Name",
                location.name,
                120,
                "e.g. Head Office or Ganjlik Branch"),
            locationField(
                "Address",
                "Address",
                location.address,
                240,
                "Street, building, office"),
            locationField(
                "Country",
                "Country",
                location.country,
                100,
                "Azerbaijan"),
            locationField(
                "City",
                "City",
                location.city,
                100,
                "Baku"));

        card.append(header, id, grid);
        locationList.append(card);
        reindexLocations();
    };

    const normalizeWebsite = () => {
        if (!websiteInput)
            return;

        const value = websiteInput.value.trim();
        if (!value)
            return;

        if (value.startsWith("//"))
            websiteInput.value = `https:${value}`;
        else if (!/^[a-z][a-z\d+.-]*:\/\//i.test(value))
            websiteInput.value = `https://${value}`;
    };

    const setLogoDataUrl = dataUrl => {
        if (logoDataUrlInput)
            logoDataUrlInput.value = dataUrl || "";

        if (logoPreview)
        {
            logoPreview.src = dataUrl || "";
            logoPreview.hidden = !dataUrl;
        }

        const tile = logoInput?.closest(".company-upload-tile");
        tile?.classList.toggle("has-file", Boolean(dataUrl));
        updateCompletion();
    };

    const closeCropDialog = () => {
        if (cropDialog?.open)
            cropDialog.close();

        if (cropSourceUrl)
        {
            URL.revokeObjectURL(cropSourceUrl);
            cropSourceUrl = "";
        }

        cropImage = null;
        if (logoInput)
            logoInput.value = "";
    };

    const drawCrop = () => {
        if (!cropCanvas || !cropImage)
            return;

        const context = cropCanvas.getContext("2d");
        const size = cropCanvas.width;
        const baseScale = Math.max(
            size / cropImage.naturalWidth,
            size / cropImage.naturalHeight);
        const scale = baseScale * cropZoomValue;
        const width = cropImage.naturalWidth * scale;
        const height = cropImage.naturalHeight * scale;
        const maxOffsetX = Math.max(0, (width - size) / 2);
        const maxOffsetY = Math.max(0, (height - size) / 2);
        cropOffsetX = Math.max(-maxOffsetX, Math.min(maxOffsetX, cropOffsetX));
        cropOffsetY = Math.max(-maxOffsetY, Math.min(maxOffsetY, cropOffsetY));

        context.clearRect(0, 0, size, size);
        context.fillStyle = "#171923";
        context.fillRect(0, 0, size, size);
        context.drawImage(
            cropImage,
            (size - width) / 2 + cropOffsetX,
            (size - height) / 2 + cropOffsetY,
            width,
            height);
    };

    const openLogoCrop = file => {
        if (!cropDialog || !cropCanvas)
            return;

        if (cropSourceUrl)
            URL.revokeObjectURL(cropSourceUrl);

        cropSourceUrl = URL.createObjectURL(file);
        const image = new Image();
        image.onload = () => {
            cropImage = image;
            cropZoomValue = 1;
            cropOffsetX = 0;
            cropOffsetY = 0;
            if (cropZoom)
                cropZoom.value = "1";
            drawCrop();
            cropDialog.showModal();
        };
        image.onerror = () => {
            closeCropDialog();
            showToast("The selected logo could not be opened.", true);
        };
        image.src = cropSourceUrl;
    };

    const canvasBlob = (canvas, quality) => new Promise(resolve => {
        canvas.toBlob(resolve, "image/jpeg", quality);
    });

    const blobToDataUrl = blob => new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result || ""));
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });

    const createCroppedLogo = async () => {
        if (!cropCanvas || !cropImage)
            return null;

        const sizes = [512, 448, 384];
        const qualities = [0.9, 0.82, 0.74, 0.66, 0.58];

        for (const size of sizes)
        {
            const output = document.createElement("canvas");
            output.width = size;
            output.height = size;
            const context = output.getContext("2d");
            context.drawImage(cropCanvas, 0, 0, size, size);

            for (const quality of qualities)
            {
                const blob = await canvasBlob(output, quality);
                if (blob && blob.size <= 350 * 1024)
                    return blobToDataUrl(blob);
            }
        }

        return null;
    };

    const restoreProfile = () => {
        try
        {
            const raw = initialDataElement?.textContent?.trim();
            if (!raw)
            {
                addLocation();
                return;
            }

            const profile = JSON.parse(raw);

            Object.entries(profile || {}).forEach(([name, value]) => {
                if ([
                    "benefits",
                    "locations",
                    "logoDataUrl",
                    "updatedAtUtc"
                ].includes(name))
                    return;

                const element = form.elements.namedItem(name);

                if (element instanceof HTMLInputElement
                    || element instanceof HTMLSelectElement
                    || element instanceof HTMLTextAreaElement)
                {
                    element.value = String(value ?? "");
                }
            });

            (profile.benefits || []).forEach(addBenefit);

            const locations = Array.isArray(profile.locations)
                ? profile.locations
                : [];

            if (locations.length > 0)
            {
                locations.forEach(addLocation);
            }
            else if (profile.companyAddress
                || profile.companyCountry
                || profile.companyCity)
            {
                addLocation({
                    address: profile.companyAddress,
                    country: profile.companyCountry,
                    city: profile.companyCity
                });
            }
            else
            {
                addLocation();
            }

            setLogoDataUrl(profile.logoDataUrl || "");
        }
        catch
        {
            if (locationList?.children.length === 0)
                addLocation();
            showToast("Company profile data could not be loaded.", true);
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

    const saveProfile = async () => {
        normalizeWebsite();

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

        const formData = new FormData(form);
        formData.delete("companyLogo");
        formData.delete("companyCover");
        formData.delete("companyGalleryFiles");
        formData.delete("Benefits");

        getBenefits().forEach(benefit => {
            formData.append("Benefits", benefit);
        });

        saveButton.disabled = true;
        const originalText = saveButton.textContent;
        saveButton.textContent = "Saving...";

        try
        {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });
            const result = await response.json();

            if (!response.ok || !result?.success)
            {
                throw new Error(
                    result?.message || "Company profile could not be saved.");
            }

            showToast(
                result.message || "Company profile saved for the whole team.");
        }
        catch (error)
        {
            showToast(
                error instanceof Error
                    ? error.message
                    : "Company profile could not be saved.",
                true);
        }
        finally
        {
            saveButton.disabled = false;
            saveButton.textContent = originalText;
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
            else if (input.id === "companyLogo")
                setLogoDataUrl(logoDataUrlInput?.value);
            else
                updateSingleUpload(input);

            updateCompletion();
            return;
        }

        if (input.id === "companyLogo")
        {
            const file = input.files?.[0];
            if (file)
                openLogoCrop(file);
            return;
        }

        if (input.id === "companyGalleryFiles")
            renderGallery(input);
        else
            updateSingleUpload(input);

        updateCompletion();
    });

    cropZoom?.addEventListener("input", () => {
        cropZoomValue = Number(cropZoom.value || 1);
        drawCrop();
    });

    cropStage?.addEventListener("pointerdown", event => {
        if (!cropImage)
            return;

        cropDragging = true;
        cropPointerX = event.clientX;
        cropPointerY = event.clientY;
        cropStage.setPointerCapture(event.pointerId);
    });

    cropStage?.addEventListener("pointermove", event => {
        if (!cropDragging)
            return;

        cropOffsetX += event.clientX - cropPointerX;
        cropOffsetY += event.clientY - cropPointerY;
        cropPointerX = event.clientX;
        cropPointerY = event.clientY;
        drawCrop();
    });

    const stopCropDrag = () => {
        cropDragging = false;
    };

    cropStage?.addEventListener("pointerup", stopCropDrag);
    cropStage?.addEventListener("pointercancel", stopCropDrag);

    document.getElementById("companyLogoCropSave")
        ?.addEventListener("click", async event => {
            const button = event.currentTarget;
            button.disabled = true;
            const originalText = button.textContent;
            button.textContent = "Saving...";

            try
            {
                const dataUrl = await createCroppedLogo();
                if (!dataUrl)
                    throw new Error("The cropped logo could not be reduced below 350KB.");

                setLogoDataUrl(dataUrl);
                const title = logoInput
                    ?.closest(".company-upload-tile")
                    ?.querySelector("[data-upload-title]");
                if (title)
                    title.textContent = "Logo ready";
                closeCropDialog();
            }
            catch (error)
            {
                showToast(
                    error instanceof Error
                        ? error.message
                        : "The cropped logo could not be saved.",
                    true);
            }
            finally
            {
                button.disabled = false;
                button.textContent = originalText;
            }
        });

    ["companyLogoCropClose", "companyLogoCropCancel"]
        .forEach(id => document.getElementById(id)
            ?.addEventListener("click", closeCropDialog));

    cropDialog?.addEventListener("cancel", event => {
        event.preventDefault();
        closeCropDialog();
    });

    addLocationButton?.addEventListener("click", () => addLocation());
    websiteInput?.addEventListener("blur", normalizeWebsite);

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

    benefitInput?.addEventListener("input", () => {
        renderBenefitSuggestions(benefitInput.value);
    });

    benefitInput?.addEventListener("focus", () => {
        renderBenefitSuggestions(benefitInput.value);
    });

    document.addEventListener("click", event => {
        if (event.target === benefitInput
            || benefitSuggestions?.contains(event.target))
            return;

        if (benefitSuggestions)
            benefitSuggestions.hidden = true;
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
