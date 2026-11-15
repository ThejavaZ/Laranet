(function () {
    "use strict";

    // Hamburger menu toggle
    var hamburger = document.querySelector("[data-hamburger]");
    var sidebar = document.querySelector("[data-sidebar]");
    var overlay = document.querySelector("[data-sidebar-overlay]");

    function toggleSidebar() {
        if (!sidebar) return;
        var isOpen = sidebar.classList.toggle("is-open");
        if (hamburger) hamburger.classList.toggle("is-active", isOpen);
        if (overlay) {
            overlay.classList.toggle("is-visible", isOpen);
            overlay.style.display = isOpen ? "block" : "none";
        }
        document.body.style.overflow = isOpen ? "hidden" : "";
    }

    if (hamburger) {
        hamburger.addEventListener("click", toggleSidebar);
    }

    if (overlay) {
        overlay.addEventListener("click", toggleSidebar);
    }

    // Loading states
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

    // Toasts
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

    // Responsive table: add data-label for mobile card view
    function setupResponsiveTables() {
        document.querySelectorAll(".admin-table").forEach(function (table) {
            var headers = Array.from(table.querySelectorAll("thead th")).map(function (th) {
                return th.textContent.trim();
            });

            table.querySelectorAll("tbody tr").forEach(function (row) {
                row.querySelectorAll("td").forEach(function (cell, index) {
                    if (headers[index]) {
                        cell.setAttribute("data-label", headers[index]);
                    }
                });
            });
        });
    }

    setupResponsiveTables();
})();
