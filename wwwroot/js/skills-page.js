(() => {
    const panels = Array.from(
        document.querySelectorAll("[data-panel]"));

    const backdrop = document.querySelector(
        "[data-panel-backdrop]");

    const closePanels = () => {
        panels.forEach(panel => {
            panel.classList.remove("open");
            panel.setAttribute("aria-hidden", "true");
        });

        backdrop?.classList.remove("visible");
        document.body.style.overflow = "";
    };

    document
        .querySelectorAll("[data-open-panel]")
        .forEach(button => {
            button.addEventListener("click", () => {
                const targetName =
                    button.getAttribute("data-open-panel");

                const panel = document.querySelector(
                    `[data-panel="${targetName}"]`);

                if (!panel) return;

                closePanels();

                panel.classList.add("open");
                panel.setAttribute("aria-hidden", "false");
                backdrop?.classList.add("visible");
                document.body.style.overflow = "hidden";

                const firstField =
                    panel.querySelector("select, input");

                window.setTimeout(
                    () => firstField?.focus(),
                    180);
            });
        });

    document
        .querySelectorAll("[data-close-panel]")
        .forEach(button => {
            button.addEventListener(
                "click",
                closePanels);
        });

    backdrop?.addEventListener(
        "click",
        closePanels);

    document.addEventListener("keydown", event => {
        if (event.key === "Escape")
            closePanels();
    });
})();
