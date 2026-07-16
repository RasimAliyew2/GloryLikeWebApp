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
