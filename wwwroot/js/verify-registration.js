(() => {
    "use strict";

    const page =
        document.getElementById("verificationPage");
    const timer =
        document.getElementById("verificationTimer");
    const timerPanel =
        timer?.closest(".timer-panel");
    const codeInput =
        document.getElementById("verificationCode");
    const verifyButton =
        document.getElementById("verifyButton");
    const verificationForm =
        document.getElementById("verificationForm");
    const resendArea =
        document.getElementById("resendArea");
    const resendForm =
        document.getElementById("resendForm");
    const resendButton =
        document.getElementById("resendButton");

    if (!page
        || !timer
        || !timerPanel
        || !codeInput
        || !verifyButton
        || !verificationForm
        || !resendArea) {
        return;
    }

    const readSeconds = value => {
        const seconds = Number(value);
        return Number.isFinite(seconds)
            ? Math.max(0, Math.min(60, Math.ceil(seconds)))
            : 0;
    };
    const startedAt = Date.now();
    const expiresAt = startedAt
        + readSeconds(page.dataset.expiresInSeconds) * 1000;
    const resendAt = startedAt
        + readSeconds(page.dataset.resendInSeconds) * 1000;

    const formatSeconds = (seconds) => {
        const minutes =
            Math.floor(seconds / 60);
        const remainingSeconds =
            seconds % 60;

        return `${String(minutes).padStart(2, "0")}:${String(
            remainingSeconds).padStart(2, "0")}`;
    };

    const updateTimer = () => {
        const now = Date.now();
        const secondsRemaining =
            Math.max(
                0,
                Math.ceil((expiresAt - now) / 1000));
        const canResend =
            now >= resendAt;
        const expired =
            secondsRemaining === 0;

        timer.textContent =
            expired
                ? "Expired"
                : formatSeconds(secondsRemaining);
        timerPanel.classList.toggle(
            "expired",
            expired);

        codeInput.disabled = expired;
        verifyButton.disabled = expired;
        resendArea.classList.toggle(
            "is-hidden",
            !canResend);

        return !expired || !canResend;
    };

    codeInput.addEventListener("input", () => {
        codeInput.value =
            codeInput.value
                .replace(/\D/g, "")
                .slice(0, 6);
    });

    verificationForm.addEventListener("submit", (event) => {
        if (!/^\d{6}$/.test(codeInput.value)) {
            event.preventDefault();
            codeInput.focus();
            codeInput.setCustomValidity(
                "Enter the 6-digit verification code.");
            codeInput.reportValidity();
            codeInput.setCustomValidity("");
            return;
        }

        verifyButton.disabled = true;
        verifyButton.textContent = "Verifying...";
    });

    resendForm?.addEventListener("submit", () => {
        if (!resendButton)
            return;

        resendButton.disabled = true;
        resendButton.textContent = "Sending...";
    });

    updateTimer();
    window.setInterval(updateTimer, 500);
})();
