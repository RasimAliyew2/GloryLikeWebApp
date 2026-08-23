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

    const installMessagesButton = () => {
        const account = document.querySelector(
            ".employer-topbar > .employer-account");
        if (!account || account.closest(".employer-account-area"))
            return;

        const area = document.createElement("div");
        area.className = "employer-account-area";
        const badge = account.querySelector(".demo-badge");
        account.parentElement?.replaceChild(area, account);
        if (badge)
            area.append(badge);

        const messagesLink = document.createElement("a");
        messagesLink.className = "employer-messages-button";
        messagesLink.href = "/Employer/Messages";
        messagesLink.setAttribute("aria-label", "Open company messages");
        if (window.location.pathname.toLowerCase() === "/employer/messages")
            messagesLink.classList.add("active");

        const icon = document.createElement("span");
        icon.className = "employer-messages-icon";
        icon.textContent = "✉";
        const label = document.createElement("span");
        label.className = "employer-messages-label";
        label.textContent = "Messages";
        const unread = document.createElement("span");
        unread.className = "employer-messages-unread";
        unread.hidden = true;
        messagesLink.append(icon, label, unread);

        area.append(messagesLink, account);

        const refresh = async () => {
            try {
                const response = await fetch(
                    "/Employer/Messages/UnreadCount",
                    { credentials: "same-origin" });
                if (!response.ok)
                    return;
                const result = await response.json();
                const count = Math.max(0, Number(result.unreadCount) || 0);
                unread.textContent = count > 99 ? "99+" : String(count);
                unread.hidden = count === 0;
                messagesLink.setAttribute(
                    "aria-label",
                    count > 0
                        ? `Open company messages, ${count} unread`
                        : "Open company messages");
            } catch {
                // The top bar remains usable when the counter endpoint is unavailable.
            }
        };

        window.refreshEmployerMessageCount = refresh;
        refresh();
        window.setInterval(refresh, 45000);
    };

    installMessagesButton();
})();
