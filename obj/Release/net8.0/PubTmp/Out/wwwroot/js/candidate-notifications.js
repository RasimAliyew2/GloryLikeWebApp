(() => {
    const root = document.querySelector("[data-candidate-notifications]");
    if (!root) return;

    const toggle = root.querySelector("[data-notification-toggle]");
    const panel = root.querySelector("[data-notification-panel]");
    const badge = root.querySelector("[data-notification-count]");
    const summary = root.querySelector("[data-notification-summary]");
    const list = root.querySelector("[data-notification-list]");
    const token = root.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    let notifications = [];
    let unreadCount = 0;

    const updateBadge = () => {
        if (unreadCount > 0) {
            badge.textContent = unreadCount > 99 ? "99+" : String(unreadCount);
            badge.hidden = false;
            summary.textContent = `${unreadCount} unread notification${unreadCount === 1 ? "" : "s"}`;
        } else {
            badge.textContent = "";
            badge.hidden = true;
            summary.textContent = "No unread notifications";
        }
    };

    const formatTime = value => {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return "";
        const minutes = Math.max(0, Math.floor((Date.now() - date.getTime()) / 60000));
        if (minutes < 1) return "Just now";
        if (minutes < 60) return `${minutes}m ago`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `${hours}h ago`;
        return date.toLocaleDateString(undefined, { day: "2-digit", month: "short" });
    };

    const render = () => {
        list.replaceChildren();
        if (!notifications.length) {
            const state = document.createElement("div");
            state.className = "candidate-notification-state";
            state.textContent = "No notifications yet.";
            list.appendChild(state);
            return;
        }

        notifications.forEach(item => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `candidate-notification-item${item.isRead ? "" : " unread"}`;

            const marker = document.createElement("span");
            marker.className = "candidate-notification-marker";
            marker.textContent = "◇";

            const copy = document.createElement("span");
            copy.className = "candidate-notification-copy";
            const title = document.createElement("strong");
            title.textContent = item.title || "Application updated";
            const message = document.createElement("span");
            message.textContent = item.message || "Your application was updated.";
            const time = document.createElement("small");
            time.textContent = formatTime(item.createdAtUtc);
            copy.append(title, message, time);
            button.append(marker, copy);

            button.addEventListener("click", async () => {
                button.disabled = true;
                let targetUrl = item.targetUrl || `/Applications/${item.vacancyId}`;
                if (!item.isRead) {
                    try {
                        const response = await fetch(`/Candidate/Notifications/${item.notificationId}/Read`, {
                            method: "POST",
                            headers: { "RequestVerificationToken": token }
                        });
                        const result = await response.json();
                        if (!response.ok || !result.success) throw new Error(result.message || "Notification could not be opened.");
                        item.isRead = true;
                        unreadCount = Math.max(0, unreadCount - 1);
                        updateBadge();
                        targetUrl = result.redirectUrl || targetUrl;
                    } catch (error) {
                        button.disabled = false;
                        return;
                    }
                }
                window.location.assign(targetUrl);
            });
            list.appendChild(button);
        });
    };

    const load = async () => {
        try {
            const response = await fetch("/Candidate/Notifications", { headers: { "Accept": "application/json" } });
            const result = await response.json();
            if (!response.ok || !result.success) throw new Error();
            notifications = Array.isArray(result.notifications) ? result.notifications : [];
            unreadCount = Number(result.unreadCount) || 0;
            updateBadge();
            render();
        } catch {
            list.innerHTML = '<div class="candidate-notification-state error">Notifications could not be loaded.</div>';
        }
    };

    toggle.addEventListener("click", event => {
        event.stopPropagation();
        panel.hidden = !panel.hidden;
        toggle.setAttribute("aria-expanded", String(!panel.hidden));
    });
    panel.addEventListener("click", event => event.stopPropagation());
    document.addEventListener("click", () => {
        panel.hidden = true;
        toggle.setAttribute("aria-expanded", "false");
    });

    load();
    window.setInterval(() => {
        if (document.visibilityState === "visible") load();
    }, 30000);
})();
