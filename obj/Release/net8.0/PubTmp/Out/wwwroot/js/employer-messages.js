(() => {
    const contextNode = document.getElementById("messagesPageContext");
    if (!contextNode)
        return;

    let context;
    try {
        context = JSON.parse(contextNode.textContent || "{}");
    } catch {
        return;
    }

    const csrfToken = document.querySelector(
        "#messagesSecurityToken input[name='__RequestVerificationToken']")?.value || "";
    const threadRoot = document.getElementById("messagesThreadBody");
    const title = document.getElementById("messagesThreadTitle");
    const candidateLink = document.getElementById("messagesCandidateLink");
    const form = document.getElementById("messagesReplyForm");
    const input = document.getElementById("messagesReplyInput");
    const sendButton = document.getElementById("messagesReplySend");
    const feedback = document.getElementById("messagesReplyFeedback");
    const mentionMenu = document.getElementById("messagesMentionMenu");
    const search = document.getElementById("messageSearch");
    let selected = null;
    let sequence = 0;

    const request = async (url, options = {}) => {
        const response = await fetch(url, {
            credentials: "same-origin",
            ...options,
            headers: {
                "Content-Type": "application/json",
                ...(options.method && options.method !== "GET" && csrfToken
                    ? { RequestVerificationToken: csrfToken }
                    : {}),
                ...(options.headers || {})
            }
        });
        let payload = null;
        try { payload = await response.json(); } catch { payload = null; }
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

    const renderThread = messages => {
        threadRoot.replaceChildren();
        if (!messages.length) {
            const empty = document.createElement("div");
            empty.className = "messages-thread-empty";
            const text = document.createElement("p");
            text.textContent = "No messages in this conversation yet.";
            empty.append(text);
            threadRoot.append(empty);
            return;
        }

        messages.forEach(item => {
            const article = document.createElement("article");
            article.className = "candidate-message-bubble";
            if (Number(item.senderUserId) === Number(context.actorUserId))
                article.classList.add("mine");

            const header = document.createElement("header");
            const name = document.createElement("strong");
            name.textContent = item.senderDisplayName || "Team member";
            const time = document.createElement("time");
            time.textContent = formatTime(item.createdAtUtc);
            header.append(name, time);

            const paragraph = document.createElement("p");
            appendMessageText(
                paragraph,
                item.body || "",
                item.candidateDisplayName || selected.candidateName,
                item.candidateUserId || selected.candidateUserId);
            article.append(header, paragraph);
            threadRoot.append(article);
        });
        threadRoot.scrollTop = threadRoot.scrollHeight;
    };

    const markRead = async () => {
        if (!selected)
            return;
        try {
            await request("/Employer/Messages/Read", {
                method: "POST",
                body: JSON.stringify({
                    otherUserId: selected.otherUserId,
                    candidateUserId: selected.candidateUserId
                })
            });
            selected.button.querySelector(".message-conversation-unread")?.remove();
            const remainingUnread = Array.from(
                document.querySelectorAll(".message-conversation-unread"))
                .reduce((total, item) => total + (Number(item.textContent) || 0), 0);
            const summary = document.querySelector(
                ".messages-unread-summary strong");
            if (summary)
                summary.textContent = String(remainingUnread);
            window.refreshEmployerMessageCount?.();
        } catch {
            // A read-receipt failure does not block the conversation.
        }
    };

    const loadThread = async () => {
        if (!selected)
            return;
        const current = ++sequence;
        threadRoot.innerHTML = '<div class="messages-thread-empty"><p>Loading conversation…</p></div>';
        feedback.textContent = "";
        try {
            const result = await request(
                `/Employer/Messages/Thread?otherUserId=${encodeURIComponent(selected.otherUserId)}&candidateUserId=${encodeURIComponent(selected.candidateUserId)}`,
                { method: "GET" });
            if (sequence !== current)
                return;
            renderThread(result.messages || []);
            await markRead();
        } catch (error) {
            if (sequence !== current)
                return;
            threadRoot.replaceChildren();
            const message = document.createElement("div");
            message.className = "messages-thread-empty";
            message.textContent = error.message;
            threadRoot.append(message);
        }
    };

    const chooseConversation = button => {
        document.querySelectorAll("[data-message-conversation]").forEach(item =>
            item.classList.toggle("active", item === button));
        selected = {
            button,
            otherUserId: Number(button.dataset.otherUserId),
            otherName: button.dataset.otherName || "Team member",
            candidateUserId: Number(button.dataset.candidateUserId),
            candidateName: button.dataset.candidateName || "Candidate"
        };
        title.textContent = selected.otherName;
        candidateLink.hidden = false;
        candidateLink.href = `/Employer/Candidates/${selected.candidateUserId}`;
        candidateLink.textContent = `View @${selected.candidateName}`;
        input.disabled = false;
        sendButton.disabled = false;
        input.value = `@${selected.candidateName} `;
        loadThread();
    };

    const conversationButtons = Array.from(
        document.querySelectorAll("[data-message-conversation]"));
    conversationButtons.forEach(button =>
        button.addEventListener("click", () => chooseConversation(button)));

    search?.addEventListener("input", () => {
        const query = search.value.trim().toLowerCase();
        conversationButtons.forEach(button => {
            button.hidden = query.length > 0
                && !(button.dataset.search || "").includes(query);
        });
    });

    const mentionChoices = () => {
        if (!selected)
            return [];
        return [
            { displayName: selected.candidateName, label: "Candidate" },
            ...(context.teamMembers || []).map(item => ({
                displayName: item.displayName,
                label: item.role || "Team member"
            }))
        ];
    };

    const hideMentions = () => {
        mentionMenu.hidden = true;
        mentionMenu.replaceChildren();
    };

    input?.addEventListener("input", () => {
        const cursor = input.selectionStart ?? input.value.length;
        const beforeCursor = input.value.slice(0, cursor);
        const atIndex = beforeCursor.lastIndexOf("@");
        if (atIndex < 0 || (atIndex > 0 && !/\s/.test(beforeCursor[atIndex - 1]))) {
            hideMentions();
            return;
        }
        const query = beforeCursor.slice(atIndex + 1).toLowerCase();
        const matches = /\n/.test(query)
            ? []
            : mentionChoices().filter(item =>
                item.displayName.toLowerCase().includes(query)).slice(0, 8);
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
                const suffix = input.value.slice(cursor);
                input.value = `${input.value.slice(0, atIndex)}@${item.displayName} ${suffix}`;
                const position = atIndex + item.displayName.length + 2;
                input.setSelectionRange(position, position);
                hideMentions();
                input.focus();
            });
            mentionMenu.append(button);
        });
        mentionMenu.hidden = false;
    });

    form?.addEventListener("submit", async event => {
        event.preventDefault();
        if (!selected)
            return;
        const body = input.value.trim();
        if (!body) {
            feedback.textContent = "Write a reply first.";
            return;
        }
        sendButton.disabled = true;
        feedback.textContent = "";
        hideMentions();
        try {
            await request("/Employer/Messages/Send", {
                method: "POST",
                body: JSON.stringify({
                    recipientUserId: selected.otherUserId,
                    candidateUserId: selected.candidateUserId,
                    body
                })
            });
            input.value = `@${selected.candidateName} `;
            await loadThread();
        } catch (error) {
            feedback.textContent = error.message;
        } finally {
            sendButton.disabled = false;
            input.focus();
        }
    });

    if (conversationButtons.length)
        chooseConversation(conversationButtons[0]);
})();
