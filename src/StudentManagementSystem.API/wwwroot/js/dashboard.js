import {
    apiRequest,
    clearElement,
    createStatusBadge,
    createTextCell,
    formatDate,
    initAuthenticatedShell
} from "./api.js";

const loadingEl = document.getElementById("dashboardLoading");
const contentEl = document.getElementById("dashboardContent");
const errorEl = document.getElementById("dashboardError");
const errorTextEl = document.getElementById("dashboardErrorText");
const retryBtn = document.getElementById("retryDashboardBtn");

let courseChartInstance = null;
let monthlyChartInstance = null;

function renderKpis(stats) {
    document.getElementById("statTotalStudents").textContent = String(stats.totalStudents ?? 0);
    document.getElementById("statActiveStudents").textContent = String(stats.activeStudents ?? 0);
    document.getElementById("statInactiveStudents").textContent = String(stats.inactiveStudents ?? 0);
    document.getElementById("statGraduatedStudents").textContent = String(stats.graduatedStudents ?? 0);
    document.getElementById("statYearComparison").textContent =
        `Enrolled in ${stats.currentYear}: ${stats.enrolledThisYear ?? 0}`;
    document.getElementById("statLastYear").textContent =
        `Enrolled in ${stats.currentYear - 1}: ${stats.enrolledLastYear ?? 0}`;
}

function renderCourseChart(distribution) {
    const emptyEl = document.getElementById("courseChartEmpty");
    const wrapperEl = document.getElementById("courseChartWrapper");
    const canvas = document.getElementById("courseChart");

    if (courseChartInstance) {
        courseChartInstance.destroy();
        courseChartInstance = null;
    }

    if (!distribution || distribution.length === 0) {
        emptyEl.classList.remove("d-none");
        wrapperEl.classList.add("d-none");
        return;
    }

    emptyEl.classList.add("d-none");
    wrapperEl.classList.remove("d-none");

    const palette = ["#4f46e5", "#0ea5e9", "#10b981", "#f59e0b", "#ec4899", "#8b5cf6", "#64748b"];
    courseChartInstance = new window.Chart(canvas, {
        type: "bar",
        data: {
            labels: distribution.map((item) => item.course),
            datasets: [
                {
                    label: "Students",
                    data: distribution.map((item) => item.studentCount),
                    backgroundColor: distribution.map((_, idx) => palette[idx % palette.length]),
                    borderRadius: 6
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: "y",
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    ticks: { precision: 0 }
                }
            }
        }
    });
}

function renderMonthlyChart(monthlyEnrollments, currentYear) {
    const titleEl = document.getElementById("monthlyChartTitle");
    titleEl.textContent = `Monthly Registrations (${currentYear})`;

    const canvas = document.getElementById("monthlyChart");
    if (monthlyChartInstance) {
        monthlyChartInstance.destroy();
        monthlyChartInstance = null;
    }

    monthlyChartInstance = new window.Chart(canvas, {
        type: "bar",
        data: {
            labels: (monthlyEnrollments || []).map((m) => m.monthName),
            datasets: [
                {
                    label: "Registered Students",
                    data: (monthlyEnrollments || []).map((m) => m.count),
                    backgroundColor: "rgba(79, 70, 229, 0.85)",
                    borderColor: "#4f46e5",
                    borderWidth: 1,
                    borderRadius: 6
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { precision: 0 }
                }
            }
        }
    });
}

function renderRecentStudents(recentStudents) {
    const tbody = document.getElementById("recentStudentsBody");
    const emptyEl = document.getElementById("recentStudentsEmpty");
    const tableWrapper = document.getElementById("recentStudentsTableWrapper");

    clearElement(tbody);

    if (!recentStudents || recentStudents.length === 0) {
        emptyEl.classList.remove("d-none");
        tableWrapper.classList.add("d-none");
        return;
    }

    emptyEl.classList.add("d-none");
    tableWrapper.classList.remove("d-none");

    recentStudents.forEach((student) => {
        const row = document.createElement("tr");
        row.appendChild(createTextCell(student.enrollmentNumber, "fw-semibold"));
        row.appendChild(createTextCell(student.fullName));
        row.appendChild(createTextCell(student.course));
        row.appendChild(createTextCell(String(student.enrollmentYear)));

        const statusCell = document.createElement("td");
        statusCell.appendChild(createStatusBadge(student.status));
        row.appendChild(statusCell);

        row.appendChild(createTextCell(formatDate(student.createdAtUtc), "text-secondary small"));
        tbody.appendChild(row);
    });
}

async function loadDashboard() {
    errorEl.classList.add("d-none");
    loadingEl.classList.remove("d-none");
    contentEl.classList.add("d-none");

    try {
        const stats = await apiRequest("/api/dashboard/stats");
        renderKpis(stats);
        renderCourseChart(stats.courseDistribution);
        renderMonthlyChart(stats.monthlyEnrollments, stats.currentYear);
        renderRecentStudents(stats.recentStudents);

        loadingEl.classList.add("d-none");
        contentEl.classList.remove("d-none");
    } catch (err) {
        loadingEl.classList.add("d-none");
        errorTextEl.textContent = err.message || "Failed to load dashboard statistics.";
        errorEl.classList.remove("d-none");
    }
}

retryBtn.addEventListener("click", loadDashboard);

async function bootstrap() {
    try {
        await initAuthenticatedShell();
        await loadDashboard();
    } catch {
        // Redirected to login on 401; otherwise error banner handles state.
    }
}

bootstrap();
