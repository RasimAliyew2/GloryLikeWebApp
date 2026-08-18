(() => {
    "use strict";

    const form = document.getElementById("screeningApplicationForm");
    const submit = document.getElementById("screeningSubmitButton");

    if (!form || !submit)
        return;

    form.addEventListener("submit", event => {
        const unansweredMultiple = Array.from(
            form.querySelectorAll("[data-multiple-choice]"))
            .find(group => !group.querySelector("input:checked"));

        if (unansweredMultiple) {
            event.preventDefault();
            unansweredMultiple.closest("[data-screening-question]")
                ?.classList.add("invalid");
            unansweredMultiple.querySelector("input")?.focus();
            window.alert("Bütün multiple choice suallarında ən azı bir seçim edin.");
            return;
        }

        submit.disabled = true;
        submit.classList.add("loading");
        submit.textContent = "Submitting...";
    });
})();
