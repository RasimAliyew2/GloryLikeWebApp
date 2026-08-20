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
