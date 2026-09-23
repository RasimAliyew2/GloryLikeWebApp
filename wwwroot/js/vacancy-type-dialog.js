(() => {
    "use strict";

    const dialog = document.getElementById("vacancyTypeDialog");
    if (!dialog || typeof dialog.showModal !== "function") return;

    const choices = Array.from(dialog.querySelectorAll("[data-vacancy-type]"));
    let opener = null;

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("a[data-create-vacancy]");
        if (!trigger || event.defaultPrevented || event.button !== 0
            || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

        // Keep the originating URL, including a linked hiring-plan row.
        choices.forEach((choice) => {
            const target = new URL(trigger.href, window.location.href);
            target.searchParams.set("vacancyType", choice.dataset.vacancyType);
            choice.href = target.toString();
        });

        event.preventDefault();
        // A hiring-plan action menu closes on click; restore focus to its visible button.
        opener = trigger.closest(".plan-actions")?.querySelector("[data-plan-menu]") ?? trigger;
        dialog.showModal();
        choices[0]?.focus();
    });

    dialog.querySelectorAll("[data-close-vacancy-type]").forEach((button) => {
        button.addEventListener("click", () => dialog.close());
    });

    // Native dialog supplies Escape handling and keeps keyboard focus inside it.
    dialog.addEventListener("close", () => opener?.focus());
})();
