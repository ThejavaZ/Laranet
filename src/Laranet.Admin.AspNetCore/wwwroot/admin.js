(function () {
    "use strict";

    function setLoading(form) {
        if (form.dataset.loadingStarted === "true") {
            return;
        }

        form.dataset.loadingStarted = "true";
        form.classList.add("is-loading");

        form.querySelectorAll("[data-loading-button], button[type='submit']")
            .forEach(function (button) {
                button.disabled = true;
                button.classList.add("is-loading");
                button.dataset.originalText = button.textContent;
                button.textContent = "Working…";
            });
    }

    document.querySelectorAll("[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.dataset.confirm)) {
                event.preventDefault();
                return;
            }

            setLoading(form);
        });
    });

    document.querySelectorAll("[data-loading-form]:not([data-confirm])")
        .forEach(function (form) {
            form.addEventListener("submit", function () {
                setLoading(form);
            });
        });

    document.querySelectorAll("[data-loading-link]").forEach(function (link) {
        link.addEventListener("click", function () {
            link.classList.add("is-loading");
        });
    });

    document.querySelectorAll("[data-toast]").forEach(function (toast) {
        var close = toast.querySelector("[data-toast-close]");
        var remove = function () {
            toast.classList.add("is-dismissed");
            window.setTimeout(function () {
                toast.remove();
            }, 180);
        };

        if (close) {
            close.addEventListener("click", remove);
        }

        window.setTimeout(remove, 4500);
    });
})();
