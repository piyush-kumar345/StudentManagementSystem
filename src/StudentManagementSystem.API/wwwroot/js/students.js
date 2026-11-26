import {
    apiRequest,
    clearElement,
    createStatusBadge,
    createTextCell,
    hideAlert,
    initAuthenticatedShell,
    showAlert
} from "./api.js";

const searchInput = document.getElementById("searchInput");
const courseFilter = document.getElementById("courseFilter");
const statusFilter = document.getElementById("statusFilter");
const pageSizeSelect = document.getElementById("pageSizeSelect");
const resetFiltersBtn = document.getElementById("resetFiltersBtn");
const addStudentBtn = document.getElementById("addStudentBtn");
const emptyAddStudentBtn = document.getElementById("emptyAddStudentBtn");

const loadingEl = document.getElementById("studentsLoading");
const emptyEl = document.getElementById("studentsEmpty");
const tableWrapperEl = document.getElementById("studentsTableWrapper");
const tableBodyEl = document.getElementById("studentsTableBody");
const paginationSummaryEl = document.getElementById("paginationSummary");
const paginationControlsEl = document.getElementById("paginationControls");

const successAlertEl = document.getElementById("studentsSuccessAlert");
const successTextEl = document.getElementById("studentsSuccessText");
const closeSuccessBtn = document.getElementById("closeSuccessAlertBtn");
const errorAlertEl = document.getElementById("studentsErrorAlert");

// Student Add/Edit Modal elements
const studentModalEl = document.getElementById("studentModal");
const studentModal = new window.bootstrap.Modal(studentModalEl);
const studentModalTitle = document.getElementById("studentModalTitle");
const studentForm = document.getElementById("studentForm");
const studentFormError = document.getElementById("studentFormError");
const saveStudentBtn = document.getElementById("saveStudentBtn");
const saveStudentSpinner = document.getElementById("saveStudentSpinner");
const saveStudentText = document.getElementById("saveStudentText");

// Delete Confirmation Modal elements
const deleteModalEl = document.getElementById("deleteStudentModal");
const deleteModal = new window.bootstrap.Modal(deleteModalEl);
const deleteStudentNameEl = document.getElementById("deleteStudentName");
const deleteStudentEnrollmentEl = document.getElementById("deleteStudentEnrollment");
const confirmDeleteBtn = document.getElementById("confirmDeleteBtn");
const deleteStudentSpinner = document.getElementById("deleteStudentSpinner");

const formFields = {
    firstName: document.getElementById("firstName"),
    lastName: document.getElementById("lastName"),
    enrollmentNumber: document.getElementById("enrollmentNumber"),
    email: document.getElementById("email"),
    phoneNumber: document.getElementById("phoneNumber"),
    dateOfBirth: document.getElementById("dateOfBirth"),
    gender: document.getElementById("gender"),
    enrollmentYear: document.getElementById("enrollmentYear"),
    status: document.getElementById("status"),
    course: document.getElementById("course"),
    address: document.getElementById("address")
};

const state = {
    page: 1,
    pageSize: 10,
    search: "",
    course: "",
    status: "",
    editingStudentId: null,
    deletingStudentId: null
};

let searchDebounceTimer = null;

function getTodayIsoDate() {
    return new Date().toISOString().split("T")[0];
}

function getMaxEnrollmentYear() {
    return new Date().getUTCFullYear() + 1;
}

function showSuccess(message) {
    successTextEl.textContent = message;
    successAlertEl.classList.remove("d-none");
}

function hideSuccess() {
    successAlertEl.classList.add("d-none");
}

closeSuccessBtn.addEventListener("click", hideSuccess);

async function loadCourseOptions() {
    const previousValue = courseFilter.value;
    try {
        const stats = await apiRequest("/api/dashboard/stats");
        clearElement(courseFilter);

        const defaultOpt = document.createElement("option");
        defaultOpt.value = "";
        defaultOpt.textContent = "All Courses";
        courseFilter.appendChild(defaultOpt);

        (stats.courseDistribution || []).forEach((item) => {
            const opt = document.createElement("option");
            opt.value = item.course;
            opt.textContent = `${item.course} (${item.studentCount})`;
            courseFilter.appendChild(opt);
        });

        if (previousValue && Array.from(courseFilter.options).some((o) => o.value === previousValue)) {
            courseFilter.value = previousValue;
        }
    } catch {
        // Non-blocking: table remains usable even if course dropdown metadata fails.
    }
}

async function loadStudents() {
    hideAlert(errorAlertEl);
    loadingEl.classList.remove("d-none");
    emptyEl.classList.add("d-none");
    tableWrapperEl.classList.add("d-none");

    const params = new URLSearchParams({
        page: String(state.page),
        pageSize: String(state.pageSize)
    });

    if (state.search) {
        params.set("search", state.search);
    }
    if (state.course) {
        params.set("course", state.course);
    }
    if (state.status) {
        params.set("status", state.status);
    }

    try {
        const result = await apiRequest(`/api/students?${params.toString()}`);
        loadingEl.classList.add("d-none");

        if (!result.items || result.items.length === 0) {
            emptyEl.classList.remove("d-none");
            return;
        }

        renderStudentsTable(result.items);
        renderPagination(result);
        tableWrapperEl.classList.remove("d-none");
    } catch (err) {
        loadingEl.classList.add("d-none");
        showAlert(errorAlertEl, err.message || "Unable to load students.");
    }
}

function renderStudentsTable(items) {
    clearElement(tableBodyEl);

    items.forEach((student) => {
        const row = document.createElement("tr");
        row.appendChild(createTextCell(student.enrollmentNumber, "fw-semibold"));
        row.appendChild(createTextCell(student.fullName));
        row.appendChild(createTextCell(student.email, "text-secondary small"));
        row.appendChild(createTextCell(student.course));
        row.appendChild(createTextCell(String(student.enrollmentYear)));

        const statusCell = document.createElement("td");
        statusCell.appendChild(createStatusBadge(student.status));
        row.appendChild(statusCell);

        const actionsCell = document.createElement("td");
        actionsCell.className = "text-end text-nowrap";

        const editBtn = document.createElement("button");
        editBtn.type = "button";
        editBtn.className = "btn btn-sm btn-outline-primary me-1";
        editBtn.title = "Edit student";
        const editIcon = document.createElement("i");
        editIcon.className = "bi bi-pencil-square";
        editBtn.appendChild(editIcon);
        editBtn.addEventListener("click", () => openEditModal(student));

        const deleteBtn = document.createElement("button");
        deleteBtn.type = "button";
        deleteBtn.className = "btn btn-sm btn-outline-danger";
        deleteBtn.title = "Delete student";
        const deleteIcon = document.createElement("i");
        deleteIcon.className = "bi bi-trash";
        deleteBtn.appendChild(deleteIcon);
        deleteBtn.addEventListener("click", () => openDeleteModal(student));

        actionsCell.appendChild(editBtn);
        actionsCell.appendChild(deleteBtn);
        row.appendChild(actionsCell);

        tableBodyEl.appendChild(row);
    });
}

function renderPagination(result) {
    const startRecord = (result.pageNumber - 1) * result.pageSize + 1;
    const endRecord = Math.min(result.pageNumber * result.pageSize, result.totalRecords);
    paginationSummaryEl.textContent =
        `Showing ${startRecord}–${endRecord} of ${result.totalRecords} students`;

    clearElement(paginationControlsEl);

    const addPageItem = (label, targetPage, disabled, active = false) => {
        const li = document.createElement("li");
        li.className = `page-item${disabled ? " disabled" : ""}${active ? " active" : ""}`;

        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "page-link";
        btn.textContent = label;
        btn.disabled = disabled;
        if (!disabled && !active) {
            btn.addEventListener("click", () => {
                state.page = targetPage;
                loadStudents();
            });
        }

        li.appendChild(btn);
        paginationControlsEl.appendChild(li);
    };

    addPageItem("Previous", result.pageNumber - 1, !result.hasPreviousPage);

    const totalPages = result.totalPages || 1;
    for (let p = 1; p <= totalPages; p++) {
        addPageItem(String(p), p, false, p === result.pageNumber);
    }

    addPageItem("Next", result.pageNumber + 1, !result.hasNextPage);
}

function clearFormValidationState() {
    studentForm.classList.remove("was-validated");
    hideAlert(studentFormError);
    Object.values(formFields).forEach((input) => {
        input.setCustomValidity("");
        input.classList.remove("is-invalid");
    });
}

function configureDateAndYearLimits() {
    formFields.dateOfBirth.max = getTodayIsoDate();
    formFields.enrollmentYear.max = String(getMaxEnrollmentYear());
}

function openAddModal() {
    state.editingStudentId = null;
    clearFormValidationState();
    studentForm.reset();
    configureDateAndYearLimits();
    formFields.status.value = "Active";
    formFields.enrollmentYear.value = String(new Date().getUTCFullYear());
    studentModalTitle.textContent = "Register New Student";
    saveStudentText.textContent = "Register Student";
    studentModal.show();
}

function openEditModal(student) {
    state.editingStudentId = student.id;
    clearFormValidationState();
    configureDateAndYearLimits();

    formFields.firstName.value = student.firstName || "";
    formFields.lastName.value = student.lastName || "";
    formFields.enrollmentNumber.value = student.enrollmentNumber || "";
    formFields.email.value = student.email || "";
    formFields.phoneNumber.value = student.phoneNumber || "";
    formFields.dateOfBirth.value = student.dateOfBirth || "";
    formFields.gender.value = student.gender || "";
    formFields.enrollmentYear.value = student.enrollmentYear ?? "";
    formFields.status.value = student.status || "Active";
    formFields.course.value = student.course || "";
    formFields.address.value = student.address || "";

    studentModalTitle.textContent = "Edit Student Details";
    saveStudentText.textContent = "Update Student";
    studentModal.show();
}

function validateStudentForm() {
    clearFormValidationState();

    const today = getTodayIsoDate();
    const maxYear = getMaxEnrollmentYear();

    const firstName = formFields.firstName.value.trim();
    const lastName = formFields.lastName.value.trim();
    const enrollmentNumber = formFields.enrollmentNumber.value.trim();
    const email = formFields.email.value.trim();
    const phone = formFields.phoneNumber.value.trim();
    const dob = formFields.dateOfBirth.value;
    const gender = formFields.gender.value.trim();
    const yearValue = Number(formFields.enrollmentYear.value);
    const course = formFields.course.value.trim();
    const address = formFields.address.value.trim();

    if (!firstName) {
        formFields.firstName.setCustomValidity("First name is required.");
    }
    if (!lastName) {
        formFields.lastName.setCustomValidity("Last name is required.");
    }
    if (enrollmentNumber.length < 2 || enrollmentNumber.length > 30) {
        formFields.enrollmentNumber.setCustomValidity("Enrollment number must be 2–30 characters.");
    }
    if (!email) {
        formFields.email.setCustomValidity("Email is required.");
    }
    if (phone && !/^[+\d\s().-]{5,20}$/.test(phone)) {
        formFields.phoneNumber.setCustomValidity("Phone number format is invalid.");
    }
    if (!dob || dob < "1900-01-01" || dob > today) {
        formFields.dateOfBirth.setCustomValidity("Date of birth must be between 1900-01-01 and today.");
    }
    if (!gender) {
        formFields.gender.setCustomValidity("Gender is required.");
    }
    if (!Number.isInteger(yearValue) || yearValue < 1950 || yearValue > maxYear) {
        formFields.enrollmentYear.setCustomValidity(`Enrollment year must be between 1950 and ${maxYear}.`);
    } else if (dob) {
        const birthYear = Number(dob.slice(0, 4));
        if (yearValue < birthYear) {
            formFields.enrollmentYear.setCustomValidity("Enrollment year cannot be earlier than birth year.");
            document.getElementById("enrollmentYearError").textContent =
                "Enrollment year cannot be earlier than the student's birth year.";
        }
    }
    if (!course) {
        formFields.course.setCustomValidity("Course is required.");
    }
    if (address.length > 300) {
        formFields.address.setCustomValidity("Address cannot exceed 300 characters.");
    }

    studentForm.classList.add("was-validated");
    return studentForm.checkValidity();
}

function applyServerValidationErrors(errors) {
    if (!errors) {
        return;
    }
    const messages = [];
    Object.entries(errors).forEach(([key, fieldErrors]) => {
        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
        const input = formFields[normalizedKey];
        const errorText = Array.isArray(fieldErrors) ? fieldErrors.join(" ") : String(fieldErrors);
        messages.push(errorText);
        if (input) {
            input.setCustomValidity(errorText);
            input.classList.add("is-invalid");
            const feedbackEl = document.getElementById(`${normalizedKey}Error`);
            if (feedbackEl) {
                feedbackEl.textContent = errorText;
            }
        }
    });
    if (messages.length > 0) {
        showAlert(studentFormError, messages.join(" "));
    }
}

studentForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!validateStudentForm()) {
        return;
    }

    const payload = {
        enrollmentNumber: formFields.enrollmentNumber.value.trim(),
        firstName: formFields.firstName.value.trim(),
        lastName: formFields.lastName.value.trim(),
        email: formFields.email.value.trim(),
        phoneNumber: formFields.phoneNumber.value.trim() || null,
        dateOfBirth: formFields.dateOfBirth.value,
        gender: formFields.gender.value,
        course: formFields.course.value.trim(),
        enrollmentYear: Number(formFields.enrollmentYear.value),
        address: formFields.address.value.trim() || null,
        status: formFields.status.value
    };

    const isEdit = state.editingStudentId !== null;
    saveStudentBtn.disabled = true;
    saveStudentSpinner.classList.remove("d-none");

    try {
        if (isEdit) {
            await apiRequest(`/api/students/${state.editingStudentId}`, {
                method: "PUT",
                body: payload
            });
            showSuccess("Student record updated successfully.");
        } else {
            await apiRequest("/api/students", {
                method: "POST",
                body: payload
            });
            state.page = 1;
            showSuccess("Student registered successfully.");
        }

        studentModal.hide();
        await Promise.all([loadStudents(), loadCourseOptions()]);
    } catch (err) {
        if (err.errors) {
            applyServerValidationErrors(err.errors);
        } else {
            showAlert(studentFormError, err.message || "Unable to save student record.");
        }
    } finally {
        saveStudentBtn.disabled = false;
        saveStudentSpinner.classList.add("d-none");
    }
});

function openDeleteModal(student) {
    state.deletingStudentId = student.id;
    deleteStudentNameEl.textContent = student.fullName;
    deleteStudentEnrollmentEl.textContent = `Enrollment #: ${student.enrollmentNumber}`;
    deleteModal.show();
}

confirmDeleteBtn.addEventListener("click", async () => {
    if (state.deletingStudentId === null) {
        return;
    }

    confirmDeleteBtn.disabled = true;
    deleteStudentSpinner.classList.remove("d-none");

    try {
        await apiRequest(`/api/students/${state.deletingStudentId}`, {
            method: "DELETE"
        });
        deleteModal.hide();
        showSuccess("Student record deleted successfully.");
        await Promise.all([loadStudents(), loadCourseOptions()]);
    } catch (err) {
        deleteModal.hide();
        showAlert(errorAlertEl, err.message || "Unable to delete student.");
    } finally {
        state.deletingStudentId = null;
        confirmDeleteBtn.disabled = false;
        deleteStudentSpinner.classList.add("d-none");
    }
});

// Filter & search handlers
searchInput.addEventListener("input", () => {
    clearTimeout(searchDebounceTimer);
    searchDebounceTimer = setTimeout(() => {
        state.search = searchInput.value.trim();
        state.page = 1;
        loadStudents();
    }, 300);
});

courseFilter.addEventListener("change", () => {
    state.course = courseFilter.value;
    state.page = 1;
    loadStudents();
});

statusFilter.addEventListener("change", () => {
    state.status = statusFilter.value;
    state.page = 1;
    loadStudents();
});

pageSizeSelect.addEventListener("change", () => {
    state.pageSize = Number(pageSizeSelect.value);
    state.page = 1;
    loadStudents();
});

resetFiltersBtn.addEventListener("click", () => {
    searchInput.value = "";
    courseFilter.value = "";
    statusFilter.value = "";
    pageSizeSelect.value = "10";
    state.search = "";
    state.course = "";
    state.status = "";
    state.pageSize = 10;
    state.page = 1;
    loadStudents();
});

addStudentBtn.addEventListener("click", openAddModal);
emptyAddStudentBtn.addEventListener("click", openAddModal);

Object.values(formFields).forEach((input) => {
    input.addEventListener("input", () => {
        input.setCustomValidity("");
        input.classList.remove("is-invalid");
    });
});

async function bootstrap() {
    try {
        await initAuthenticatedShell();
        await Promise.all([loadCourseOptions(), loadStudents()]);
    } catch {
        // Redirected on 401; otherwise error banner handles state.
    }
}

bootstrap();
