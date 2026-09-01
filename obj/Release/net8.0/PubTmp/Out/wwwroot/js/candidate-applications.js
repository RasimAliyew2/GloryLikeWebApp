(() => {
    const highlighted = document.querySelector('.application-card[data-highlighted="true"]');
    if (!highlighted) return;
    window.requestAnimationFrame(() => highlighted.scrollIntoView({ behavior: "smooth", block: "center" }));
})();
