# Student Management System

A production-style **Student Management System** built with **ASP.NET Core Web API (.NET 10)**, **Entity Framework Core**, **SQL Server**, and a responsive **Bootstrap 5 + Vanilla JavaScript** frontend.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Features](#features)
- [Architecture & Folder Structure](#architecture--folder-structure)
- [Prerequisites](#prerequisites)
- [Setup & Configuration](#setup--configuration)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. SQL Server & User Secrets Configuration](#2-sql-server--user-secrets-configuration)
  - [3. Database Migration](#3-database-migration)
  - [4. Initial Admin Provisioning](#4-initial-admin-provisioning)
  - [5. Running the Application](#5-running-the-application)
- [API Endpoint Summary](#api-endpoint-summary)
- [Security Highlights](#security-highlights)
- [Testing Instructions](#testing-instructions)
- [Screenshots](#screenshots)

---

## Project Overview

This application provides an authenticated administrator portal for managing student lifecycles—registration, search, filtering, updates, deletion, and real-time analytics—backed by a clean layered ASP.NET Core Web API and SQL Server database.

---

## Technology Stack

- **Backend:** C#, ASP.NET Core Web API (`.NET 10`)
- **Database & ORM:** SQL Server, Entity Framework Core (`10.0.15`), Code-First Migrations
- **Authentication:** JWT Bearer + `SameSite=Strict` `HttpOnly` Cookie, ASP.NET Core `PasswordHasher<AdminUser>` (PBKDF2)
- **API Documentation:** Swagger / OpenAPI (`Swashbuckle.AspNetCore`)
- **Frontend:** HTML5, CSS3, Bootstrap 5, Bootstrap Icons, Vanilla JavaScript (ES Modules), Chart.js
- **Testing:** xUnit + EF Core InMemory (`StudentManagementSystem.Tests`)

---

## Features

- **Admin Authentication:** Login via username or email, password visibility toggle, `HttpOnly` cookie session for the web UI, and Bearer token support for Swagger/API clients.
- **Dashboard Analytics:** Total, Active, Inactive, and Graduated KPI cards, year-over-year intake comparison, course-wise distribution chart, monthly registration chart, and latest 5 registrations table.
- **Student Management:** Full CRUD (`Create`, `Read`, `Update`, `Delete`) with modal forms, confirmation dialogs, and server-side pagination.
- **Search & Filtering:** Database-side search by first name, last name, full name, or enrollment number, plus course and status filters.
- **Validation & Error Handling:** Data Annotations + `IValidatableObject` rules, SQL Server unique-constraint conflict handling (`409 Conflict`), and global RFC 7807 `ProblemDetails` exception middleware.
- **Responsive Design:** Fixed desktop sidebar and collapsible mobile/tablet offcanvas navigation.

---

## Architecture & Folder Structure

The backend follows a clean layered architecture where Controllers delegate to Service interfaces, Services encapsulate business logic and EF Core queries, and `ExceptionHandlingMiddleware` standardizes error responses.

```text
StudentManagementSystem/
├── StudentManagementSystem.slnx
├── README.md
├── src/
│   └── StudentManagementSystem.API/
│       ├── Configuration/            # Strongly-typed JWT & cookie options
│       ├── Controllers/              # AuthController, StudentsController, DashboardController
│       ├── Data/
│       │   ├── AppDbContext.cs       # EF Core DbContext
│       │   ├── DbSeeder.cs           # Safe opt-in admin provisioning
│       │   ├── Configurations/       # Fluent API table, constraint & index configs
│       │   └── Migrations/           # EF Core Code-First migrations
│       ├── DTOs/                     # Auth, Student, Dashboard & PagedResult DTOs
│       ├── Exceptions/               # Domain exceptions (401, 404, 409)
│       ├── Interfaces/               # IAuthService, IStudentService, IDashboardService
│       ├── Middleware/               # Global ExceptionHandlingMiddleware
│       ├── Models/                   # Student, AdminUser, StudentStatus
│       ├── Services/                 # Business logic & database aggregations
│       ├── wwwroot/                  # Static frontend (login.html, index.html, students.html, css/, js/)
│       ├── appsettings.json          # Non-sensitive defaults only
│       ├── appsettings.Example.json  # Safe template for secret keys
│       └── Program.cs                # DI, AuthN/AuthZ, Swagger & static hosting
└── tests/
    └── StudentManagementSystem.Tests/
        └── Services/                 # Unit tests for Auth, Student & Dashboard services
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`10.0.x`)
- SQL Server (`SQLEXPRESS`, `LocalDB`, or SQL Server Developer/Container)
- `dotnet-ef` CLI tool (if applying migrations via CLI):
  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.15
  ```

---

## Setup & Configuration

### 1. Clone the Repository

```bash
git clone https://github.com/Piyush-Kumar345/StudentManagementSystem.git
cd StudentManagementSystem
```

### 2. SQL Server & User Secrets Configuration

Never commit database passwords, JWT signing keys, or admin passwords. Use `.NET User Secrets` for local development (or environment variables in production). See `src/StudentManagementSystem.API/appsettings.Example.json` for the expected keys.

```bash
# Set your SQL Server connection string (example using Windows Authentication with SQLEXPRESS)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=StudentManagementSystemDb;Trusted_Connection=True;TrustServerCertificate=True;" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj

# Set a strong random JWT signing key (minimum 32 characters / 256 bits)
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_CRYPTOGRAPHICALLY_RANDOM_SECRET_32_CHARS_MIN" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
```

### 3. Database Migration

Apply the EF Core migrations to create the `StudentManagementSystemDb` database, `Admins` and `Students` tables, constraints, and indexes:

```bash
dotnet ef database update --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj --startup-project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
```

### 4. Initial Admin Provisioning

This project does **not** include hardcoded default admin passwords or a public admin registration endpoint. On startup, `DbSeeder` checks whether the `Admins` table is empty and provisions an initial admin **only** when `AdminSeed` configuration is supplied via User Secrets or environment variables (minimum 8-character password):

```bash
dotnet user-secrets set "AdminSeed:Username" "admin" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
dotnet user-secrets set "AdminSeed:Email" "admin@example.com" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
dotnet user-secrets set "AdminSeed:FullName" "System Administrator" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
dotnet user-secrets set "AdminSeed:Password" "ChooseAStrongPasswordHere!" --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
```

*(After your initial admin is created in SQL Server, you can remove `AdminSeed:Password` from User Secrets if desired.)*

### 5. Running the Application

```bash
dotnet run --project src/StudentManagementSystem.API/StudentManagementSystem.API.csproj
```

- **Web Portal:** `https://localhost:7061/` (or `http://localhost:5126/`)
- **Login Page:** `https://localhost:7061/login.html`
- **Swagger UI (Development):** `https://localhost:7061/swagger`

---

## API Endpoint Summary

### Authentication (`/api/auth`)
| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Public | Authenticates admin, returns JWT & sets `HttpOnly` cookie |
| `POST` | `/api/auth/logout` | Public | Clears the `HttpOnly` authentication cookie |
| `GET` | `/api/auth/me` | `Admin` | Returns the authenticated admin profile |

### Students (`/api/students`)
| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/students` | `Admin` | Paginated list with `?search=&course=&status=&page=1&pageSize=10` |
| `GET` | `/api/students/{id}` | `Admin` | Get student details by ID |
| `POST` | `/api/students` | `Admin` | Register a new student (`409` on duplicate enrollment/email) |
| `PUT` | `/api/students/{id}` | `Admin` | Update an existing student record |
| `DELETE` | `/api/students/{id}` | `Admin` | Delete a student record |

### Dashboard (`/api/dashboard`)
| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/dashboard/stats` | `Admin` | Aggregated counts, course breakdown, monthly trends & 5 recent students |

---

## Security Highlights

- **Password Storage:** Passwords are hashed using ASP.NET Core `PasswordHasher<AdminUser>` (salted PBKDF2); password hashes are never returned in DTOs.
- **Token Transport:** Browser sessions use an `HttpOnly`, `SameSite=Strict` cookie (`sms_auth_token`) with same-origin checks on unsafe HTTP methods, keeping tokens inaccessible to JavaScript (`document.cookie` / `localStorage`). API clients and Swagger continue to work with standard `Authorization: Bearer <token>` headers.
- **Authorization Boundary:** All `/api/students`, `/api/dashboard`, and `/api/auth/me` endpoints enforce `[Authorize(Roles = "Admin")]` on the server; frontend redirects are strictly UX convenience.
- **XSS Prevention:** Dynamic student data in the DOM is rendered using `textContent` rather than `innerHTML`.

---

## Testing Instructions

Run the complete build and xUnit test suite from the repository root:

```bash
dotnet build StudentManagementSystem.slnx
dotnet test StudentManagementSystem.slnx
```

---

## Screenshots

> *Add screenshots after running the application locally and capturing your preferred views.*

- **Admin Login Page:** `<!-- ![Admin Login](docs/screenshots/login.png) -->`
- **Dashboard Analytics:** `<!-- ![Admin Dashboard](docs/screenshots/dashboard.png) -->`
- **Student Management & Modal Form:** `<!-- ![Student Management](docs/screenshots/students.png) -->`
- **Swagger / OpenAPI:** `<!-- ![Swagger UI](docs/screenshots/swagger.png) -->`
