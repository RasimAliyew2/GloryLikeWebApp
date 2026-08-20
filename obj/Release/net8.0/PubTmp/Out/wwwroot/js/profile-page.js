(() => {
    const input = document.getElementById("profilePhotoInput");
    const hidden = document.getElementById("profileImageDataUrl");
    const preview = document.getElementById("profilePhotoPreview");
    const remove = document.getElementById("profilePhotoRemove");
    const error = document.getElementById("profilePhotoError");

    if (!input || !hidden || !preview) return;

    const allowedTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
    const maxSourceBytes = 5 * 1024 * 1024;
    const maxSavedBytes = 500 * 1024;

    const setError = (message = "") => {
        if (error) error.textContent = message;
    };

    const showImage = (dataUrl) => {
        preview.textContent = "";
        const image = document.createElement("img");
        image.src = dataUrl;
        image.alt = "Profile preview";
        preview.appendChild(image);
    };

    const loadImage = (file) => new Promise((resolve, reject) => {
        const image = new Image();
        const objectUrl = URL.createObjectURL(file);
        image.onload = () => {
            URL.revokeObjectURL(objectUrl);
            resolve(image);
        };
        image.onerror = () => {
            URL.revokeObjectURL(objectUrl);
            reject(new Error("The selected image could not be read."));
        };
        image.src = objectUrl;
    });

    const canvasBlob = (canvas, quality) => new Promise((resolve) => {
        canvas.toBlob(resolve, "image/jpeg", quality);
    });

    const blobToDataUrl = (blob) => new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(new Error("The optimized image could not be read."));
        reader.readAsDataURL(blob);
    });

    const optimizeSquare = async (file) => {
        const image = await loadImage(file);
        const sourceSize = Math.min(image.naturalWidth, image.naturalHeight);
        const sourceX = Math.max(0, (image.naturalWidth - sourceSize) / 2);
        const sourceY = Math.max(0, (image.naturalHeight - sourceSize) / 2);

        for (const size of [512, 448, 384]) {
            const canvas = document.createElement("canvas");
            canvas.width = size;
            canvas.height = size;
            const context = canvas.getContext("2d", { alpha: false });
            context.fillStyle = "#ffffff";
            context.fillRect(0, 0, size, size);
            context.drawImage(image, sourceX, sourceY, sourceSize, sourceSize, 0, 0, size, size);

            for (const quality of [0.88, 0.78, 0.68, 0.58]) {
                const blob = await canvasBlob(canvas, quality);
                if (blob && blob.size <= maxSavedBytes) return blob;
            }
        }

        throw new Error("The photo could not be optimized below 500 KB. Choose another image.");
    };

    input.addEventListener("change", async () => {
        const file = input.files?.[0];
        if (!file) return;

        setError();
        if (!allowedTypes.has(file.type)) {
            input.value = "";
            setError("Only JPG, PNG and WEBP images are supported.");
            return;
        }
        if (file.size > maxSourceBytes) {
            input.value = "";
            setError("The source image cannot exceed 5 MB.");
            return;
        }

        try {
            const blob = await optimizeSquare(file);
            const dataUrl = await blobToDataUrl(blob);
            hidden.value = String(dataUrl);
            showImage(String(dataUrl));
        } catch (exception) {
            input.value = "";
            setError(exception?.message || "The photo could not be prepared.");
        }
    });

    remove?.addEventListener("click", () => {
        hidden.value = "";
        input.value = "";
        preview.textContent = "No photo";
        setError();
    });
})();
