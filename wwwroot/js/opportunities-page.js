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
                () => {
                    const title =
                        button.dataset.roleTitle
                        ?? "this role";

                    window.alert(
                        `${title} üçün Apply əməliyyatı `
                        + "hazırkı Mobile App-də olduğu kimi "
                        + "təsdiq mesajıdır. Applications SQL API "
                        + "qoşulduqda real müraciət burada saxlanacaq.");
                });
        });
})();
