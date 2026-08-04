(() => {
    const searchInput =
        document.getElementById("talentRadarSearch");

    const cards = Array.from(
        document.querySelectorAll("[data-radar-card]"));

    const emptyState =
        document.getElementById("talentRadarSearchEmpty");

    if (!searchInput || cards.length === 0)
        return;

    const normalize = value =>
        (value ?? "")
            .trim()
            .toLocaleLowerCase();

    const applySearch = () => {
        const query = normalize(searchInput.value);
        let visibleCount = 0;

        cards.forEach(card => {
            const searchValue = normalize(
                card.dataset.searchValue);
            const visible =
                query.length === 0
                || searchValue.includes(query);

            card.hidden = !visible;

            if (visible)
                visibleCount += 1;
        });

        if (emptyState)
            emptyState.hidden = visibleCount > 0;
    };

    searchInput.addEventListener("input", applySearch);
})();
