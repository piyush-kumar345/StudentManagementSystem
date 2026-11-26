/**
 * Shared API client, authentication session guard, and safe DOM helpers.
 * Authentication relies on the backend's SameSite=Strict HttpOnly cookie (never localStorage JWTs).
 */

export class ApiError extends Error {
    constructor(message, status, errors = null) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.errors = errors;
    }
}

export async function apiRequest(url, options = {}) {
    const {
        method = "GET",
        body = null,
        redirectOnUnauthorized = true
    } = options;

    const headers = {
        Accept: "application/json"
    };

    const fetchOptions = {
        method,
        credentials: "same-origin",
        headers
    };

    if (body !== null) {
        headers["Content-Type"] = "application/json";
        fetchOptions.body = JSON.stringify(body);
    }

    let response;
    try {
        response = await fetch(url, fetchOptions);
    } catch {
        throw new ApiError("Unable to reach the server. Please check your connection.", 0);
    }

    if (response.status === 401) {
        if (redirectOnUnauthorized && !window.location.pathname.endsWith("/login.html")) {
            window.location.replace("/login.html");
        }
        const problem = await tryParseJson(response);
        throw new ApiError(
            problem?.detail || problem?.title || "Your session has expired. Please sign in again.",
            401
        );
    }

    if (response.status === 204) {
        return null;
    }

    const payload = await tryParseJson(response);

    if (!response.ok) {
        const message =
            payload?.detail ||
            payload?.title ||
            `Request failed with status ${response.status}.`;
        throw new ApiError(message, response.status, payload?.errors || null);
    }

    return payload;
}

async function tryParseJson(response) {
    const contentType = response.headers.get("content-type") || "";
    if (!contentType.includes("json")) {
        return null;
    }
    try {
        return await response.json();
    } catch {
        return null;
    }
}

/**
 * Verifies the server session via GET /api/auth/me (backend remains the security boundary)
 * and initializes the topbar admin profile + logout button.
 */
export async function initAuthenticatedShell() {
    const admin = await apiRequest("/api/auth/me", { redirectOnUnauthorized: true });

    const nameEl = document.getElementById("adminDisplayName");
    const roleEl = document.getElementById("adminDisplayRole");
    if (nameEl) {
        nameEl.textContent = admin.fullName || admin.username;
    }
    if (roleEl) {
        roleEl.textContent = admin.role || "Admin";
    }

    const logoutButtons = document.querySelectorAll("[data-action='logout']");
    logoutButtons.forEach((btn) => {
        btn.addEventListener("click", async () => {
            btn.disabled = true;
            try {
                await apiRequest("/api/auth/logout", {
                    method: "POST",
                    redirectOnUnauthorized: false
                });
            } finally {
                window.location.replace("/login.html");
            }
        });
    });

    return admin;
}

export function clearElement(element) {
    while (element.firstChild) {
        element.removeChild(element.firstChild);
    }
}

export function createTextCell(value, className = "") {
    const td = document.createElement("td");
    if (className) {
        td.className = className;
    }
    td.textContent = value ?? "";
    return td;
}

export function createStatusBadge(status) {
    const badge = document.createElement("span");
    const normalized = String(status || "Active").toLowerCase();
    const statusClass =
        normalized === "inactive"
            ? "status-inactive"
            : normalized === "graduated"
                ? "status-graduated"
                : "status-active";

    badge.className = `status-badge ${statusClass}`;
    badge.textContent = status || "Active";
    return badge;
}

export function formatDate(isoDate) {
    if (!isoDate) {
        return "—";
    }
    const parsed = new Date(isoDate);
    if (Number.isNaN(parsed.getTime())) {
        return String(isoDate);
    }
    return parsed.toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric"
    });
}

export function showAlert(alertEl, message) {
    if (!alertEl) {
        return;
    }
    alertEl.textContent = message;
    alertEl.classList.remove("d-none");
}

export function hideAlert(alertEl) {
    if (!alertEl) {
        return;
    }
    alertEl.textContent = "";
    alertEl.classList.add("d-none");
}
