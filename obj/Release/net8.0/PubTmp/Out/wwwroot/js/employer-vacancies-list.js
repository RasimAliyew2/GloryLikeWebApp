(() => {
    "use strict";

    const searchInput = document.getElementById("vacancySearchInput");
    const filterButtons = Array.from(
        document.querySelectorAll("[data-vacancy-filter]"));
    const rows = Array.from(
        document.querySelectorAll("[data-vacancy-row]"));
    const filterEmptyState = document.getElementById("vacanciesFilterEmpty");
    const liveStatus = document.getElementById("vacanciesListStatus");

    let selectedStatus = "all";
    let liveStatusTimer;

    const closeMenus = (exceptButton) => {
        document
            .querySelectorAll("[data-vacancy-menu-button]")
            .forEach((button) => {
                if (button === exceptButton) {
                    return;
                }

                button.setAttribute("aria-expanded", "false");
                const menu = button.parentElement?.querySelector(
                    "[data-vacancy-menu]");

                if (menu) {
                    menu.hidden = true;
                }
            });
    };

    const announce = (message) => {
        if (!liveStatus) {
            return;
        }

        window.clearTimeout(liveStatusTimer);
        liveStatus.textContent = message;
        liveStatus.classList.add("visible");

        liveStatusTimer = window.setTimeout(() => {
            liveStatus.classList.remove("visible");
        }, 2200);
    };

    const applyFilters = () => {
        const query = searchInput?.value.trim().toLocaleLowerCase() ?? "";
        let visibleCount = 0;

        rows.forEach((row) => {
            const rowStatus = row.dataset.status ?? "other";
            const rowSearchValue = row.dataset.searchValue ?? "";
            const matchesStatus = selectedStatus === "all"
                || rowStatus === selectedStatus;
            const matchesQuery = query.length === 0
                || rowSearchValue.includes(query);
            const isVisible = matchesStatus && matchesQuery;

            row.hidden = !isVisible;

            if (isVisible) {
                visibleCount += 1;
            }
        });

        if (filterEmptyState) {
            filterEmptyState.hidden = visibleCount !== 0;
        }

        closeMenus();
    };

    filterButtons.forEach((button) => {
        button.addEventListener("click", () => {
            selectedStatus = button.dataset.vacancyFilter ?? "all";

            filterButtons.forEach((item) => {
                item.classList.toggle("active", item === button);
            });

            applyFilters();
        });
    });

    searchInput?.addEventListener("input", applyFilters);

    rows.forEach((row) => {
        const openDetail = () => {
            const url = row.dataset.vacancyUrl ?? "";

            if (url) {
                window.location.assign(url);
            }
        };

        row.addEventListener("click", (event) => {
            if (event.target.closest("a, button, [data-vacancy-menu]")) {
                return;
            }

            openDetail();
        });

        row.addEventListener("keydown", (event) => {
            if (event.key !== "Enter" && event.key !== " ") {
                return;
            }

            if (event.target.closest("a, button, [data-vacancy-menu]")) {
                return;
            }

            event.preventDefault();
            openDetail();
        });
    });

    document
        .querySelectorAll("[data-vacancy-menu-button]")
        .forEach((button) => {
            button.addEventListener("click", (event) => {
                event.stopPropagation();

                const menu = button.parentElement?.querySelector(
                    "[data-vacancy-menu]");

                if (!menu) {
                    return;
                }

                const willOpen = menu.hidden;
                closeMenus(willOpen ? button : undefined);
                menu.hidden = !willOpen;
                button.setAttribute("aria-expanded", String(willOpen));
            });
        });

    document
        .querySelectorAll("[data-copy-vacancy-id]")
        .forEach((button) => {
            button.addEventListener("click", async () => {
                const vacancyId = button.dataset.copyVacancyId ?? "";

                if (!vacancyId) {
                    announce("Vacancy ID is not available.");
                    return;
                }

                try {
                    await navigator.clipboard.writeText(vacancyId);
                    announce("Vacancy ID copied.");
                } catch {
                    announce(`Vacancy ID: ${vacancyId}`);
                }

                closeMenus();
            });
        });

    document.addEventListener("click", () => closeMenus());
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            closeMenus();
        }
    });

    applyFilters();
})();
