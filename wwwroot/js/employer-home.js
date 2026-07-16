(() => {
    const sidebar =
        document.getElementById("employerSidebar");

    const menuButton =
        document.getElementById("employerMenuButton");

    const backdrop =
        document.getElementById("employerBackdrop");

    const closeSidebar = () => {
        sidebar?.classList.remove("open");
        backdrop?.classList.remove("visible");
    };

    menuButton?.addEventListener("click", () => {
        sidebar?.classList.toggle("open");
        backdrop?.classList.toggle("visible");
    });

    backdrop?.addEventListener(
        "click",
        closeSidebar);

    document.addEventListener("keydown", event => {
        if (event.key === "Escape")
            closeSidebar();
    });

    document
        .querySelector(".create-vacancy-button")
        ?.addEventListener("click", () => {
            window.alert(
                "Create vacancy UI hazırdır. "
                + "Vacancy SQL API əlavə ediləndə "
                + "bu düymə real formu açacaq.");
        });
})();
