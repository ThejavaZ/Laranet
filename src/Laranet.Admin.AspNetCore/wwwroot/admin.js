(function () {
    "use strict";
    console.log("[admin.js] loaded");

    // Dark mode toggle
    var themeToggle = document.querySelector("[data-theme-toggle]");
    var savedTheme = localStorage.getItem("admin-theme") || "light";
    document.body.setAttribute("data-theme", savedTheme);
    console.log("[admin.js] theme:", savedTheme);

    function toggleTheme() {
        var current = document.body.getAttribute("data-theme");
        var next = current === "dark" ? "light" : "dark";
        document.body.setAttribute("data-theme", next);
        localStorage.setItem("admin-theme", next);
        console.log("[admin.js] theme toggled to:", next);
    }

    if (themeToggle) {
        themeToggle.addEventListener("click", toggleTheme);
    } else {
        console.warn("[admin.js] themeToggle not found");
    }

    // Hamburger menu toggle
    var hamburger = document.querySelector("[data-hamburger]");
    var sidebar = document.querySelector("[data-sidebar]");
    var overlay = document.querySelector("[data-sidebar-overlay]");

    console.log("[admin.js] hamburger:", hamburger, "sidebar:", sidebar, "overlay:", overlay);

    function openSidebar() {
        if (!sidebar) { console.warn("[admin.js] no sidebar"); return; }
        sidebar.classList.add("is-open");
        if (hamburger) hamburger.classList.add("is-active");
        if (overlay) {
            overlay.classList.add("is-visible");
            overlay.style.display = "block";
        }
        document.body.style.overflow = "hidden";
        console.log("[admin.js] sidebar opened");
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
        console.log("[admin.js] sidebar closed");
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
            console.log("[admin.js] hamburger clicked");
            e.preventDefault();
            e.stopPropagation();
            toggleSidebar();
        });
    } else {
        console.warn("[admin.js] hamburger not found");
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
        console.log("[admin.js] opening modal:", message);
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

    document.querySelectorAll("[data-confirm-form]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (form.dataset.confirmed === "true") {
                return;
            }
            console.log("[admin.js] modal form submit intercepted");
            event.preventDefault();
            pendingForm = form;
            openModal(form.dataset.confirmForm);
        });
    });

    if (modalConfirm) {
        modalConfirm.addEventListener("click", function () {
            console.log("[admin.js] modal confirm clicked");
            if (pendingForm) {
                pendingForm.dataset.confirmed = "true";
                pendingForm.submit();
            }
        });
    } else {
        console.warn("[admin.js] modalConfirm not found");
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

    document.querySelectorAll("[data-loading-form]:not([data-confirm-form])")
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
    console.log("[admin.js] initialized");
})();
