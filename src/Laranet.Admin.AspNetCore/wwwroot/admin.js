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
    var modalDialog = document.querySelector("[data-modal-dialog]");
    var modalMessage = document.querySelector("[data-modal-message]");
    var modalConfirm = document.querySelector("[data-modal-confirm]");
    var modalCancel = document.querySelector("[data-modal-cancel]");
    var modalClose = document.querySelector("[data-modal-close]");
    var pendingForm = null;
    var lastFocusedElement = null;

    function isModalOpen() {
        return modalOverlay !== null &&
            modalOverlay.classList.contains("is-visible");
    }

    function getFocusableElements() {
        if (!modalDialog) return [];

        return Array.from(modalDialog.querySelectorAll(
            'a[href], button:not([disabled]), input:not([disabled]), ' +
            'select:not([disabled]), textarea:not([disabled]), ' +
            '[tabindex]:not([tabindex="-1"])'
        )).filter(function (element) {
            return element.getClientRects().length > 0;
        });
    }

    function openModal(message) {
        console.log("[admin.js] opening modal:", message);
        lastFocusedElement = document.activeElement;
        if (modalMessage) modalMessage.textContent = message;
        if (modalOverlay) {
            modalOverlay.classList.add("is-visible");
            modalOverlay.setAttribute("aria-hidden", "false");
        }
        document.body.style.overflow = "hidden";

        var focusTarget = modalConfirm || getFocusableElements()[0] || modalDialog;
        if (focusTarget) focusTarget.focus();
    }

    function closeModal() {
        if (modalOverlay) {
            modalOverlay.classList.remove("is-visible");
            modalOverlay.setAttribute("aria-hidden", "true");
        }
        document.body.style.overflow = "";
        pendingForm = null;

        if (lastFocusedElement && document.contains(lastFocusedElement)) {
            lastFocusedElement.focus();
        }
        lastFocusedElement = null;
    }

    if (modalCancel) modalCancel.addEventListener("click", closeModal);
    if (modalClose) modalClose.addEventListener("click", closeModal);
    if (modalOverlay) {
        modalOverlay.addEventListener("click", function (e) {
            if (e.target === modalOverlay) closeModal();
        });
    }

    document.addEventListener("keydown", function (event) {
        if (!isModalOpen()) return;

        if (event.key === "Escape") {
            event.preventDefault();
            closeModal();
            return;
        }

        if (event.key !== "Tab") return;

        var focusable = getFocusableElements();
        if (focusable.length === 0) {
            event.preventDefault();
            if (modalDialog) modalDialog.focus();
            return;
        }

        var first = focusable[0];
        var last = focusable[focusable.length - 1];
        var active = document.activeElement;

        if (event.shiftKey && (active === first || active === modalDialog)) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && active === last) {
            event.preventDefault();
            first.focus();
        }
    });

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

    // Table skeleton: show while a list navigation is in flight
    var tableSkeleton = document.querySelector("[data-table-skeleton]");
    var tableWrap = document.querySelector(".admin-table-wrap");
    var tableFooter = document.querySelector(".admin-footer");

    function showTableSkeleton() {
        if (!tableSkeleton) return;

        tableSkeleton.hidden = false;
        tableSkeleton.removeAttribute("aria-hidden");

        if (tableWrap) tableWrap.hidden = true;
        if (tableFooter) tableFooter.hidden = true;
    }

    document
        .querySelectorAll("a.admin-sort-link, .admin-pagination a")
        .forEach(function (link) {
            link.addEventListener("click", showTableSkeleton);
        });

    document
        .querySelectorAll("form.admin-search, form.admin-page-size")
        .forEach(function (form) {
            form.addEventListener("submit", showTableSkeleton);
        });

    // Bulk selection: select-all reflects the current page selection
    var selectAll = document.querySelector("[data-select-all]");
    if (selectAll) {
        var rowCheckboxes = Array.from(
            document.querySelectorAll('input[name="keys"][form="bulk-form"]')
        ).filter(function (checkbox) {
            return checkbox !== selectAll;
        });

        var syncSelectAll = function () {
            var checkedCount = rowCheckboxes.filter(function (checkbox) {
                return checkbox.checked;
            }).length;

            selectAll.checked =
                rowCheckboxes.length > 0 && checkedCount === rowCheckboxes.length;
            selectAll.indeterminate =
                checkedCount > 0 && checkedCount < rowCheckboxes.length;
        };

        selectAll.addEventListener("change", function () {
            rowCheckboxes.forEach(function (checkbox) {
                checkbox.checked = selectAll.checked;
            });
            syncSelectAll();
        });

        rowCheckboxes.forEach(function (checkbox) {
            checkbox.addEventListener("change", syncSelectAll);
        });

        syncSelectAll();
    }

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

    console.log("[admin.js] initialized");
})();
