(function () {
    "use strict";

    function init() {
    // Dark mode toggle
    var themeToggle = document.querySelector("[data-theme-toggle]");
    var savedTheme = localStorage.getItem("admin-theme") || "light";
    document.body.setAttribute("data-theme", savedTheme);

    function toggleTheme() {
        var current = document.body.getAttribute("data-theme");
        var next = current === "dark" ? "light" : "dark";
        document.body.setAttribute("data-theme", next);
        localStorage.setItem("admin-theme", next);
    }

    if (themeToggle) {
        themeToggle.addEventListener("click", toggleTheme);
    }

    // Hamburger menu toggle
    var hamburger = document.querySelector("[data-hamburger]");
    var sidebar = document.querySelector("[data-sidebar]");
    var overlay = document.querySelector("[data-sidebar-overlay]");

    function openSidebar() {
        if (!sidebar) return;
        sidebar.classList.add("is-open");
        if (hamburger) hamburger.classList.add("is-active");
        if (overlay) {
            overlay.classList.add("is-visible");
            overlay.style.display = "block";
        }
        document.body.style.overflow = "hidden";
    }

    function closeSidebar() {
        if (!sidebar) return;
        sidebar.classList.remove("is-open");
        if (hamburger) hamburger.classList.remove("is-active");
        if (overlay) {
            overlay.classList.remove("is-visible");
            overlay.style.display = "none";
        }
        document.body.style.overflow = "";
    }

    function toggleSidebar() {
        if (sidebar && sidebar.classList.contains("is-open")) {
            closeSidebar();
        } else {
            openSidebar();
        }
    }

    if (hamburger) {
        hamburger.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleSidebar();
        });
    }

    if (overlay) {
        overlay.addEventListener("click", closeSidebar);
    }

    // Close sidebar on nav link click (mobile)
    document.querySelectorAll(".admin-nav-link").forEach(function (link) {
        link.addEventListener("click", function () {
            if (window.innerWidth < 768) {
                closeSidebar();
            }
        });
    });

    // Modal confirmation
    var modalOverlay = document.querySelector("[data-modal-overlay]");
    var modalMessage = document.querySelector("[data-modal-message]");
    var modalConfirm = document.querySelector("[data-modal-confirm]");
    var modalCancel = document.querySelector("[data-modal-cancel]");
    var modalClose = document.querySelector("[data-modal-close]");
    var pendingForm = null;

    function openModal(message) {
        if (modalMessage) modalMessage.textContent = message;
        if (modalOverlay) modalOverlay.classList.add("is-visible");
        document.body.style.overflow = "hidden";
    }

    function closeModal() {
        if (modalOverlay) modalOverlay.classList.remove("is-visible");
        document.body.style.overflow = "";
        pendingForm = null;
    }

    if (modalCancel) modalCancel.addEventListener("click", closeModal);
    if (modalClose) modalClose.addEventListener("click", closeModal);
    if (modalOverlay) {
        modalOverlay.addEventListener("click", function (e) {
            if (e.target === modalOverlay) closeModal();
        });
    }

    document.querySelectorAll("[data-modal-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            pendingForm = form;
            openModal(form.dataset.modalConfirm);
        });
    });

    if (modalConfirm) {
        modalConfirm.addEventListener("click", function () {
            if (pendingForm) {
                pendingForm.dataset.confirmed = "true";
                setLoading(pendingForm);
                pendingForm.submit();
            }
        });
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

    document.querySelectorAll("[data-loading-form]:not([data-modal-confirm])")
        .forEach(function (form) {
            form.addEventListener("submit", function () {
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
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
