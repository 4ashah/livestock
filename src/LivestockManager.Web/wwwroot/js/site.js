(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initializeTooltips();
        initializeConfirmDialogs();
        initializeAutoDismissAlerts();
    });

    function initializeTooltips() {
        if (typeof bootstrap !== "undefined" && bootstrap.Tooltip) {
            var tooltipTriggerList = [].slice.call(
                document.querySelectorAll('[data-bs-toggle="tooltip"]')
            );
            tooltipTriggerList.forEach(function (el) {
                new bootstrap.Tooltip(el);
            });
        }
    }

    function initializeConfirmDialogs() {
        document.querySelectorAll("[data-confirm]").forEach(function (el) {
            el.addEventListener("click", function (e) {
                var message = el.getAttribute("data-confirm") || "Are you sure?";
                if (!window.confirm(message)) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    return false;
                }
                return true;
            });
        });
    }

    function initializeAutoDismissAlerts() {
        var autoDismissAlerts = document.querySelectorAll("[data-alert-autodismiss]");
        autoDismissAlerts.forEach(function (alert) {
            var delay = parseInt(alert.getAttribute("data-alert-autodismiss"), 10) || 5000;
            setTimeout(function () {
                if (typeof bootstrap !== "undefined" && bootstrap.Alert) {
                    var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                    if (bsAlert) bsAlert.close();
                } else {
                    alert.style.display = "none";
                }
            }, delay);
        });
    }
})();
