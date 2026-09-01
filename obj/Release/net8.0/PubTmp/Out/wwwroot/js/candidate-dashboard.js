(() => {
    const sidebar = document.getElementById("dashboardSidebar");
    const button = document.getElementById("mobileMenuButton");
    const backdrop = document.getElementById("sidebarBackdrop");

    if (!sidebar || !button || !backdrop) return;

    const close = () => {
        sidebar.classList.remove("open");
        backdrop.classList.remove("visible");
    };

    button.addEventListener("click", () => {
        sidebar.classList.toggle("open");
        backdrop.classList.toggle("visible");
    });

    backdrop.addEventListener("click", close);

    window.addEventListener("resize", () => {
        if (window.innerWidth > 900) close();
    });
})();

(() => {
    const numbers = [...document.querySelectorAll("[data-animate-number]")];
    const rings = [...document.querySelectorAll("[data-score-ring]")];
    const progressBars = [...document.querySelectorAll("[data-progress-target]")];

    if (numbers.length === 0 && rings.length === 0 && progressBars.length === 0) return;

    const duration = window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 500;
    const easeOutCubic = (value) => 1 - Math.pow(1 - value, 3);
    const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
    const targets = numbers.map((element) => ({
        element,
        target: Number(element.dataset.target || 0),
        suffix: element.dataset.suffix || "",
        decimals: Number(element.dataset.decimals || 0)
    }));

    const render = (progress) => {
        const easedProgress = easeOutCubic(progress);

        targets.forEach(({ element, target, suffix, decimals }) => {
            const current = target * easedProgress;
            element.textContent = `${current.toFixed(decimals)}${suffix}`;
        });

        rings.forEach((ring) => {
            const target = clamp(Number(ring.dataset.target || 0), 0, 100);
            ring.style.setProperty("--score", String(target * easedProgress));
        });

        progressBars.forEach((bar) => {
            const target = clamp(Number(bar.dataset.progressTarget || 0), 0, 100);
            bar.style.width = `${target * easedProgress}%`;
        });
    };

    if (duration === 0) {
        render(1);
        return;
    }

    const startedAt = performance.now();
    const animate = (now) => {
        const progress = clamp((now - startedAt) / duration, 0, 1);
        render(progress);
        if (progress < 1) requestAnimationFrame(animate);
    };

    requestAnimationFrame(animate);
})();

(() => {
    const dialog = document.querySelector("[data-score-details-dialog]");
    const openButton = document.querySelector("[data-open-score-details]");
    const closeButton = dialog?.querySelector("[data-close-score-details]");

    if (!dialog || !openButton || !closeButton) return;

    openButton.addEventListener("click", () => dialog.showModal());
    closeButton.addEventListener("click", () => dialog.close());

    dialog.addEventListener("click", (event) => {
        const bounds = dialog.getBoundingClientRect();
        const clickedOutside = event.clientX < bounds.left
            || event.clientX > bounds.right
            || event.clientY < bounds.top
            || event.clientY > bounds.bottom;

        if (clickedOutside) dialog.close();
    });
})();

(() => {
    const target = document.getElementById("candidateCurrentLocation");
    if (!target) return;

    const endpoint = target.dataset.locationUrl;
    const cacheKey = "bothfind.candidateLocation.v1";
    const cacheDurationMs = 24 * 60 * 60 * 1000;

    const display = (value) => {
        target.textContent = `⌖ ${value}`;
    };

    try {
        const cached = JSON.parse(localStorage.getItem(cacheKey) || "null");
        if (cached?.displayName
            && Number.isFinite(cached.savedAt)
            && Date.now() - cached.savedAt < cacheDurationMs) {
            display(cached.displayName);
            return;
        }
    } catch {
        localStorage.removeItem(cacheKey);
    }

    if (!endpoint || !navigator.geolocation) {
        display("Location unavailable");
        return;
    }

    navigator.geolocation.getCurrentPosition(
        async ({ coords }) => {
            const latitude = Number(coords.latitude.toFixed(3));
            const longitude = Number(coords.longitude.toFixed(3));
            const url = new URL(endpoint, window.location.origin);
            url.searchParams.set("latitude", String(latitude));
            url.searchParams.set("longitude", String(longitude));

            try {
                const response = await fetch(url, {
                    credentials: "same-origin",
                    headers: { Accept: "application/json" }
                });
                const result = await response.json();
                if (!response.ok || !result?.success || !result?.displayName) {
                    throw new Error(result?.message || "Location could not be resolved.");
                }

                display(result.displayName);
                localStorage.setItem(cacheKey, JSON.stringify({
                    displayName: result.displayName,
                    savedAt: Date.now()
                }));
            } catch {
                display("Location unavailable");
            }
        },
        () => display("Location unavailable"),
        {
            enableHighAccuracy: false,
            timeout: 8000,
            maximumAge: 30 * 60 * 1000
        });
})();
