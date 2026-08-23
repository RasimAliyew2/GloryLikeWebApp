(() => {
    const contextNode = document.getElementById("candidateMessagingContext");
    if (!contextNode)
        return;

    let context;
    try {
        context = JSON.parse(contextNode.textContent || "{}");
    } catch {
        return;
    }

    const token = document.querySelector(
        "#candidateMessagingSecurityToken input[name='__RequestVerificationToken']")?.value || "";
    const panel = document.getElementById("candidateChatPanel");
    const openButton = document.getElementById("candidateChatOpen");
    const closeButton = document.getElementById("candidateChatClose");
    const form = document.getElementById("candidateChatForm");
    const input = document.getElementById("candidateChatInput");
    const sendButton = document.getElementById("candidateChatSend");
    const messagesRoot = document.getElementById("candidateChatMessages");
    const title = document.getElementById("candidateChatTitle");
    const feedback = document.getElementById("candidateChatFeedback");
    const mentionMenu = document.getElementById("candidateMentionMenu");
    const historyDialog = document.getElementById("candidateHistoryDialog");
    let selectedMember = null;
    let requestSequence = 0;

    const jsonRequest = async (url, options = {}) => {
        const response = await fetch(url, {
            credentials: "same-origin",
            ...options,
            headers: {
                "Content-Type": "application/json",
                ...(options.method && options.method !== "GET" && token
                    ? { RequestVerificationToken: token }
                    : {}),
                ...(options.headers || {})
            }
        });

        let payload = null;
        try {
            payload = await response.json();
        } catch {
            payload = null;
        }

        if (!response.ok)
            throw new Error(payload?.message || `Request failed. HTTP ${response.status}.`);

        return payload;
    };

    const formatTime = value => {
        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? ""
            : new Intl.DateTimeFormat(undefined, {
                day: "2-digit",
                month: "short",
                hour: "2-digit",
                minute: "2-digit"
            }).format(date);
    };

    const appendMessageText = (root, body, candidateName, candidateUserId) => {
        const mention = `@${candidateName}`;
        let cursor = 0;
        let index = body.indexOf(mention);

        while (index >= 0) {
            root.append(document.createTextNode(body.slice(cursor, index)));
            const link = document.createElement("a");
            link.href = `/Employer/Candidates/${candidateUserId}`;
            link.textContent = mention;
            root.append(link);
            cursor = index + mention.length;
            index = body.indexOf(mention, cursor);
        }

        root.append(document.createTextNode(body.slice(cursor)));
    };

    const renderMessages = items => {
        messagesRoot.replaceChildren();

        if (!items.length) {
            const empty = document.createElement("div");
            empty.className = "candidate-chat-placeholder";
            empty.textContent = "No messages yet. Start the conversation below.";
            messagesRoot.append(empty);
            return;
        }

        items.forEach(item => {
            const article = document.createElement("article");
            article.className = "candidate-message-bubble";
            if (Number(item.senderUserId) === Number(context.actorUserId))
                article.classList.add("mine");

            const header = document.createElement("header");
            const sender = document.createElement("strong");
            sender.textContent = item.senderDisplayName || "Team member";
            const time = document.createElement("time");
            time.textContent = formatTime(item.createdAtUtc);
            header.append(sender, time);

            const paragraph = document.createElement("p");
            appendMessageText(
                paragraph,
                item.body || "",
                item.candidateDisplayName || context.candidateName,
                item.candidateUserId || context.candidateUserId);
            article.append(header, paragraph);
            messagesRoot.append(article);
        });

        messagesRoot.scrollTop = messagesRoot.scrollHeight;
    };

    const markRead = async () => {
        if (!selectedMember)
            return;

        try {
            await jsonRequest("/Employer/Messages/Read", {
                method: "POST",
                body: JSON.stringify({
                    otherUserId: selectedMember.userId,
                    candidateUserId: context.candidateUserId
                })
            });
            window.refreshEmployerMessageCount?.();
        } catch {
            // Reading a thread remains usable if the read receipt fails.
        }
    };

    const loadThread = async () => {
        if (!selectedMember)
            return;

        const sequence = ++requestSequence;
        messagesRoot.innerHTML = '<div class="candidate-chat-placeholder">Loading conversation…</div>';
        feedback.textContent = "";

        try {
            const result = await jsonRequest(
                `/Employer/Messages/Thread?otherUserId=${encodeURIComponent(selectedMember.userId)}&candidateUserId=${encodeURIComponent(context.candidateUserId)}`,
                { method: "GET" });
            if (sequence !== requestSequence)
                return;
            renderMessages(result.messages || []);
            await markRead();
        } catch (error) {
            if (sequence !== requestSequence)
                return;
            messagesRoot.innerHTML = "";
            const message = document.createElement("div");
            message.className = "candidate-chat-placeholder";
            message.textContent = error.message;
            messagesRoot.append(message);
        }
    };

    const selectMember = button => {
        document.querySelectorAll("[data-chat-member]").forEach(item =>
            item.classList.toggle("active", item === button));
        selectedMember = {
            userId: Number(button.dataset.userId),
            displayName: button.dataset.displayName || "Team member"
        };
        title.textContent = selectedMember.displayName;
        input.disabled = false;
        sendButton.disabled = false;
        if (!input.value.trim())
            input.value = `@${context.candidateName} `;
        loadThread();
        input.focus();
    };

    document.querySelectorAll("[data-chat-member]").forEach(button =>
        button.addEventListener("click", () => selectMember(button)));

    openButton?.addEventListener("click", () => {
        panel.hidden = false;
        openButton.setAttribute("aria-expanded", "true");
    });

    closeButton?.addEventListener("click", () => {
        panel.hidden = true;
        openButton?.setAttribute("aria-expanded", "false");
        mentionMenu.hidden = true;
    });

    const mentionChoices = () => [
        {
            userId: Number(context.candidateUserId),
            displayName: context.candidateName,
            label: "Candidate"
        },
        ...(context.teamMembers || []).map(member => ({
            userId: Number(member.userId),
            displayName: member.displayName,
            label: member.role || "Team member"
        }))
    ];

    const hideMentions = () => {
        mentionMenu.hidden = true;
        mentionMenu.replaceChildren();
    };

    const showMentions = () => {
        const cursor = input.selectionStart ?? input.value.length;
        const beforeCursor = input.value.slice(0, cursor);
        const atIndex = beforeCursor.lastIndexOf("@");
        if (atIndex < 0 || (atIndex > 0 && !/\s/.test(beforeCursor[atIndex - 1]))) {
            hideMentions();
            return;
        }

        const query = beforeCursor.slice(atIndex + 1).toLowerCase();
        if (/\n/.test(query) || query.length > 80) {
            hideMentions();
            return;
        }

        const matches = mentionChoices()
            .filter(item => item.displayName.toLowerCase().includes(query))
            .slice(0, 8);
        if (!matches.length) {
            hideMentions();
            return;
        }

        mentionMenu.replaceChildren();
        matches.forEach(item => {
            const button = document.createElement("button");
            button.type = "button";
            const name = document.createElement("strong");
            name.textContent = item.displayName;
            const label = document.createElement("small");
            label.textContent = item.label;
            button.append(name, label);
            button.addEventListener("click", () => {
                const afterCursor = input.value.slice(cursor);
                input.value = `${input.value.slice(0, atIndex)}@${item.displayName} ${afterCursor}`;
                const nextPosition = atIndex + item.displayName.length + 2;
                input.setSelectionRange(nextPosition, nextPosition);
                hideMentions();
                input.focus();
            });
            mentionMenu.append(button);
        });
        mentionMenu.hidden = false;
    };

    input?.addEventListener("input", showMentions);
    input?.addEventListener("keydown", event => {
        if (event.key === "Escape")
            hideMentions();
    });

    form?.addEventListener("submit", async event => {
        event.preventDefault();
        if (!selectedMember)
            return;

        const body = input.value.trim();
        if (!body) {
            feedback.textContent = "Write a message first.";
            return;
        }

        sendButton.disabled = true;
        feedback.textContent = "";
        hideMentions();
        try {
            await jsonRequest("/Employer/Messages/Send", {
                method: "POST",
                body: JSON.stringify({
                    recipientUserId: selectedMember.userId,
                    candidateUserId: context.candidateUserId,
                    body
                })
            });
            input.value = `@${context.candidateName} `;
            await loadThread();
        } catch (error) {
            feedback.textContent = error.message;
        } finally {
            sendButton.disabled = false;
            input.focus();
        }
    });

    document.getElementById("candidateHistoryOpen")?.addEventListener("click", () =>
        historyDialog?.showModal());
    document.getElementById("candidateHistoryClose")?.addEventListener("click", () =>
        historyDialog?.close());
    historyDialog?.addEventListener("click", event => {
        if (event.target === historyDialog)
            historyDialog.close();
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && panel && !panel.hidden)
            panel.hidden = true;
    });
})();
