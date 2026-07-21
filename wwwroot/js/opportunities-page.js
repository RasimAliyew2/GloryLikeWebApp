(() => {
    const cards = Array.from(
        document.querySelectorAll(
            "[data-opportunity-card]"));

    const scoreButtons = Array.from(
        document.querySelectorAll(
            "[data-score-filter]"));

    const filterEmpty =
        document.getElementById(
            "clientFilterEmpty");

    const antiForgeryToken =
        document.querySelector(
            "#opportunityApplyToken input[name='__RequestVerificationToken']")
            ?.value
        ?? "";

    const savedStorageKey =
        "glorylike.saved-opportunities";

    const readSavedIds = () => {
        try {
            const value =
                localStorage.getItem(
                    savedStorageKey);

            const parsed = value
                ? JSON.parse(value)
                : [];

            return new Set(
                Array.isArray(parsed)
                    ? parsed.map(String)
                    : []);
        } catch {
            return new Set();
        }
    };

    const writeSavedIds = savedIds => {
        try {
            localStorage.setItem(
                savedStorageKey,
                JSON.stringify(
                    Array.from(savedIds)));
        } catch {
            // Browser localStorage disabled olduqda
            // page functionality davam edir.
        }
    };

    const savedIds = readSavedIds();

    const setExpanded = (
        card,
        expanded) => {

        card.classList.toggle(
            "expanded",
            expanded);

        card.dataset.expanded =
            String(expanded);

        const label =
            card.querySelector(
                "[data-toggle-label]");

        if (label) {
            label.textContent =
                expanded
                    ? "Hide"
                    : "Details";
        }

        const toggle =
            card.querySelector(
                "[data-toggle-opportunity]");

        toggle?.setAttribute(
            "aria-expanded",
            String(expanded));
    };

    cards.forEach(card => {
        const initiallyExpanded =
            card.dataset.expanded
            === "true";

        setExpanded(
            card,
            initiallyExpanded);

        card
            .querySelector(
                "[data-toggle-opportunity]")
            ?.addEventListener(
                "click",
                () => {
                    setExpanded(
                        card,
                        !card.classList.contains(
                            "expanded"));
                });

        const id =
            String(
                card.dataset.opportunityId
                ?? "");

        const bookmark =
            card.querySelector(
                "[data-bookmark]");

        const renderBookmark = () => {
            const isSaved =
                savedIds.has(id);

            bookmark?.classList.toggle(
                "saved",
                isSaved);

            if (bookmark) {
                bookmark.textContent =
                    isSaved
                        ? "♥"
                        : "♡";

                bookmark.setAttribute(
                    "aria-label",
                    isSaved
                        ? "Remove saved opportunity"
                        : "Save opportunity");
            }
        };

        renderBookmark();

        bookmark?.addEventListener(
            "click",
            () => {
                if (savedIds.has(id))
                    savedIds.delete(id);
                else
                    savedIds.add(id);

                writeSavedIds(savedIds);
                renderBookmark();
            });
    });

    const applyScoreFilter =
        minimumScore => {

            let visibleCount = 0;

            cards.forEach(card => {
                const score =
                    Number(
                        card.dataset.score
                        ?? 0);

                const visible =
                    minimumScore === null
                    || score >= minimumScore;

                card.hidden = !visible;

                if (visible)
                    visibleCount++;
            });

            if (filterEmpty) {
                filterEmpty.hidden =
                    visibleCount > 0;
            }
        };

    scoreButtons.forEach(button => {
        button.addEventListener(
            "click",
            () => {
                scoreButtons.forEach(
                    item => item.classList
                        .remove("active"));

                button.classList.add(
                    "active");

                const rawValue =
                    button.dataset.scoreFilter;

                applyScoreFilter(
                    rawValue === "all"
                        ? null
                        : Number(rawValue));
            });
    });

    document
        .querySelectorAll(
            "[data-apply-role]")
        .forEach(button => {
            button.addEventListener(
                "click",
                async () => {
                    const control = button.closest(
                        "[data-application-control]");
                    const card = button.closest(
                        "[data-opportunity-card]");
                    const errorElement = control?.querySelector(
                        "[data-application-error]");
                    const responseElement = control?.querySelector(
                        ".application-response");
                    const applyUrl = button.dataset.applyUrl ?? "";

                    if (!control
                        || control.dataset.applied === "true"
                        || !applyUrl)
                    {
                        return;
                    }

                    if (errorElement) {
                        errorElement.hidden = true;
                        errorElement.textContent = "";
                    }

                    button.disabled = true;
                    button.classList.add("loading");

                    try {
                        const response = await fetch(applyUrl, {
                            method: "POST",
                            headers: {
                                "Accept": "application/json",
                                "RequestVerificationToken": antiForgeryToken,
                                "X-Requested-With": "XMLHttpRequest"
                            },
                            credentials: "same-origin"
                        });

                        let payload = {};

                        try {
                            payload = await response.json();
                        } catch {
                            payload = {};
                        }

                        if (!response.ok || payload.success !== true) {
                            throw new Error(
                                payload.message
                                || "Application could not be saved.");
                        }

                        if (responseElement) {
                            responseElement.textContent =
                                payload.statusText
                                || "No response yet";
                        }

                        control.dataset.applied = "true";
                        control.classList.remove("just-applied");

                        // Reflow animasiyanın hər uğurlu keçiddə başlamasını təmin edir.
                        void control.offsetWidth;
                        control.classList.add("just-applied");

                        if (card) {
                            card.dataset.applicationState = "applied";
                        }
                    } catch (error) {
                        if (errorElement) {
                            errorElement.textContent =
                                error instanceof Error
                                    ? error.message
                                    : "Application could not be saved.";
                            errorElement.hidden = false;
                        }

                        button.disabled = false;
                    } finally {
                        button.classList.remove("loading");
                    }
                });
        });
})();
