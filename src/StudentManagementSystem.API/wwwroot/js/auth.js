import { apiRequest, hideAlert, showAlert } from "./api.js";

const loginForm = document.getElementById("loginForm");
const usernameInput = document.getElementById("usernameOrEmail");
const passwordInput = document.getElementById("password");
const togglePasswordBtn = document.getElementById("togglePasswordBtn");
const togglePasswordIcon = document.getElementById("togglePasswordIcon");
const submitBtn = document.getElementById("loginSubmitBtn");
const spinner = document.getElementById("loginBtnSpinner");
const btnText = document.getElementById("loginBtnText");
const errorAlert = document.getElementById("loginErrorAlert");

async function redirectIfAlreadyAuthenticated() {
    try {
        await apiRequest("/api/auth/me", { redirectOnUnauthorized: false });
        window.location.replace("/index.html");
    } catch {
        // Not authenticated yet; stay on login page.
    }
}

togglePasswordBtn.addEventListener("click", () => {
    const isHidden = passwordInput.type === "password";
    passwordInput.type = isHidden ? "text" : "password";
    togglePasswordBtn.setAttribute("aria-label", isHidden ? "Hide password" : "Show password");
    togglePasswordIcon.className = isHidden ? "bi bi-eye-slash" : "bi bi-eye";
});

function setLoading(isLoading) {
    submitBtn.disabled = isLoading;
    spinner.classList.toggle("d-none", !isLoading);
    btnText.textContent = isLoading ? "Signing in..." : "Sign In";
}

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideAlert(errorAlert);

    const usernameOrEmail = usernameInput.value.trim();
    const password = passwordInput.value;

    usernameInput.setCustomValidity(usernameOrEmail ? "" : "Required");
    passwordInput.setCustomValidity(password ? "" : "Required");
    loginForm.classList.add("was-validated");

    if (!usernameOrEmail || !password) {
        return;
    }

    setLoading(true);
    try {
        await apiRequest("/api/auth/login", {
            method: "POST",
            body: { usernameOrEmail, password },
            redirectOnUnauthorized: false
        });
        window.location.replace("/index.html");
    } catch (err) {
        showAlert(errorAlert, err.message || "Unable to sign in.");
    } finally {
        setLoading(false);
    }
});

redirectIfAlreadyAuthenticated();
