# Business Requirements Document
## BRD-01: C# Basics

> **Current implementation note:** This document retains the detailed training curriculum and examples from the original guide. The solution targets .NET 9.0. Current controllers, entities, DTOs, and routes are authoritative where historical examples differ; examples for planned features are retained as exercises.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-01                                               |
| **Module**        | 1 of 7 — C# Basics                                   |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | None — entry-level module                            |
| **Est. Duration** | 3–5 days                                             |

---

## 🎯 THE PROJECT: Job Application Tracker — Internship & Career Management System

> All 7 modules collectively build the **Job Application Tracker**, a full-stack career-management system.
> Each module's exercises directly contribute working components to this system.

### Job Application Tracker System Description

The Job Application Tracker is a multi-tier web application that helps students and early-career developers manage every internship or job application in one organized workspace:

- Application lifecycle tracking (`Saved → Applied → OnlineAssessment → Interview → Offer / Rejected`)
- Kanban board view with drag-and-drop status management
- Dashboard metrics (total applications, response rate, interviews, offers, upcoming deadlines)
- Search and filter by company, position, status, employment type, and deadline
- Application timeline/events (interviews, follow-ups, status notes)
- CV-to-job matching powered by an LLM API

**Technology Stack:**
| Layer        | Technology                     |
|---|---|
| Backend API  | C# / ASP.NET Core 9 Web API    |
| ORM          | Entity Framework Core 9        |
| Database     | Microsoft SQL Server 2022      |
| Auth         | JWT Bearer Tokens              |
| Docs         | Swagger / OpenAPI 3.0          |
| Testing      | xUnit + Moq                    |
| Frontend     | Angular (standalone components)|

---

## 📋 MASTER API CATALOGUE — All APIs

> This catalogue is the full contract for the Job Application Tracker system.
> Each BRD implements the subset of APIs relevant to its module.

### Authentication (3 APIs)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 1 | POST | `/api/auth/signup` | Create a new user account |
| 2 | POST | `/api/auth/login` | Authenticate user, return JWT token |
| 3 | POST | `/api/auth/refresh-token` | Exchange refresh token for new access token |

### Applications (8 APIs)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 4 | GET | `/api/applications` | List user's applications (filterable, paginated) |
| 5 | GET | `/api/applications/{id}` | Get single application details and timeline |
| 6 | POST | `/api/applications` | Create a new application record |
| 7 | PUT | `/api/applications/{id}` | Update application details |
| 8 | PUT | `/api/applications/{id}/status` | Change application status |
| 9 | DELETE | `/api/applications/{id}` | Archive (soft-delete) an application |
| 10 | GET | `/api/applications/search` | Search by company, position, status, deadline |
| 11 | GET | `/api/applications/export` | Export application list as CSV |

### Application Timeline (2 APIs)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 12 | POST | `/api/applications/{id}/events` | Add an interview, follow-up, or status note |
| 13 | GET | `/api/applications/{id}/events` | Get full timeline for an application |

### Dashboard (1 API)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 14 | GET | `/api/dashboard/summary` | Return counts, response rate, and upcoming actions |

### CV Profile (2 APIs)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 15 | GET | `/api/cv-profile` | Get current user's CV profile |
| 16 | PUT | `/api/cv-profile` | Save or update the text-based CV profile |

### Matching (1 API)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 17 | POST | `/api/matching` | Compare a job description against the user's CV profile |

### Infrastructure (1 API)
| # | Method | Endpoint | Description |
|---|--------|----------|-------------|
| 18 | GET | `/api/health` | Health check — DB connectivity and API status |

---

## 1. Module Overview

This module introduces C# fundamentals **in the context of building the Job Application Tracker's utility and console-tooling layer**. Trainees build the helper classes, validators, and CLI diagnostic tools that underpin the entire application.

### 1.1 Business Justification

A shared library of strongly-typed utilities (validators, formatters, calculators) prevents code duplication across all modules and ensures consistent logic for response-rate calculation, input parsing, deadline formatting, and match-score display.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Set up and configure a .NET 9 development environment
- [ ] Understand `Program.cs`, namespaces, and the `Main` entry point
- [ ] Declare and use all primitive data types (`int`, `double`, `bool`, `char`, `string`, `decimal`)
- [ ] Apply implicit (`var`) and explicit type declarations correctly
- [ ] Write conditional logic using `if/else`, `switch`, and ternary operators
- [ ] Implement iteration using `for`, `foreach`, `while`, and `do-while` loops
- [ ] Use arrays and `List<T>` collections
- [ ] Define and call methods with parameters and return types
- [ ] Handle runtime exceptions using `try/catch/finally`
- [ ] Use string interpolation, formatting, and common `string` methods
- [ ] Understand value types vs. reference types and `null` handling

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- `JobTracker.Utilities` — Class Library project
- Console diagnostic CLI (manual testing tool before the API exists)

**APIs delivered this module:** `GET /api/health` (#18) — trainee builds the logic that backs this endpoint (health-check utility method).

---

## 4. Functional Requirements

### FR-01-001: Project Solution Setup
**Description:** Trainee creates the Job Application Tracker Visual Studio Solution structure.

**Deliverable Structure:**
```
JobTracker.sln
├── JobTracker.Utilities/        ← Built in this module
│   ├── ApplicationCalculator.cs
│   ├── InputValidator.cs
│   ├── StringFormatter.cs
│   └── ConsoleHelper.cs
├── JobTracker.Domain/           ← Scaffolded, built in BRD-02
├── JobTracker.DataAccess/       ← Scaffolded, built in BRD-05
├── JobTracker.Api/              ← Scaffolded, built in BRD-06
└── JobTracker.Tests/            ← Scaffolded, built in BRD-07
```

**Acceptance Criteria:**
- Solution file created with all 5 project stubs
- `JobTracker.Utilities` is a Class Library targeting .NET 9
- All projects reference correct project dependencies
- Solution builds with zero errors

---

### FR-01-002: ApplicationCalculator — Core Business Logic
**Description:** Trainee implements the application analytics utility used by the Dashboard API (#14).

**Requirements:**
```csharp
// File: JobTracker.Utilities/ApplicationCalculator.cs
public static class ApplicationCalculator
{
    // Calculate response rate: responded / total × 100
    public static decimal CalculateResponseRate(int totalApplications, int respondedCount)

    // Calculate days until deadline
    public static int DaysUntilDeadline(DateTime deadline)

    // Determine if a deadline is overdue
    public static bool IsOverdue(DateTime deadline)

    // Calculate follow-up priority score (0–10) based on days since application
    public static int GetFollowUpPriority(DateTime appliedDate, DateTime? followUpDate)
}
```

**Acceptance Criteria:**
- All 4 methods implemented correctly
- Edge cases handled: negative counts throw `ArgumentException`, null dates handled gracefully
- Each method has XML doc comments
- Verified with console test harness

---

### FR-01-003: InputValidator — Request Validation Utility
**Description:** Trainee implements validators used across all API input validation.

**Requirements:**
```csharp
// File: JobTracker.Utilities/InputValidator.cs
public static class InputValidator
{
    public static bool IsValidEmail(string email)
    public static bool IsValidUrl(string url)           // Must be a valid http/https URL
    public static bool IsValidCompanyName(string name)  // Not null, not empty, alpha + spaces + common chars
    public static bool IsValidDate(DateTime date)       // Date must be in the future for deadlines
    public static string SanitizeString(string input)   // Trim + remove harmful special chars
}
```

**Acceptance Criteria:**
- All 5 methods implemented and tested
- `IsValidEmail` uses regex pattern validation
- `IsValidUrl` checks for http/https scheme
- `SanitizeString` handles null input safely (return empty string)

---

### FR-01-004: StringFormatter — Output Formatting
**Description:** Trainee builds formatting helpers for API response display.

**Requirements:**
```csharp
// File: JobTracker.Utilities/StringFormatter.cs
public static class StringFormatter
{
    public static string FormatSalaryRange(decimal min, decimal max)   // "$50,000 – $70,000"
    public static string FormatApplicationStatus(string status)        // "Online Assessment"
    public static string FormatDeadline(DateTime deadline)             // "Due in 3 days" / "Overdue"
    public static string TruncateNotes(string text, int maxLength)
    public static string FormatResponseRate(decimal rate)              // "42.5%"
}
```

**Acceptance Criteria:**
- All 5 methods return correctly formatted strings
- `FormatSalaryRange` uses `decimal` type (not `double`) for precision
- `FormatDeadline` shows "Overdue" for past deadlines and "Today" for same-day deadlines
- `TruncateNotes` appends "..." when truncated
- Null-safe for all inputs

---

### FR-01-005: ConsoleHelper — CLI Diagnostic Tool
**Description:** Trainee builds a console menu that manually tests all utility methods, simulating what the health check API will do.

**Acceptance Criteria:**
- Menu-driven loop using `do-while`
- Options: Test ApplicationCalculator / Test Validator / Test Formatter / Exit
- Input is validated before processing
- Results displayed in formatted table using `string.PadRight()`
- `try/catch` wraps all menu actions

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **Static Utility Class** | `ApplicationCalculator`, `InputValidator`, `StringFormatter` |
| **Guard Clauses** | Validate at top of each method, throw early |
| **Fail-Fast** | Invalid inputs throw typed exceptions immediately |
| **Null Safety** | All string inputs checked for null before use |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-01-01 | All code compiles with zero errors and zero warnings |
| NFR-01-02 | All public members have XML doc comments |
| NFR-01-03 | No magic numbers — use named constants |
| NFR-01-04 | `decimal` used for all monetary values (never `double`) |
| NFR-01-05 | No expression-bodied members (`=>`) — use block bodies for all methods and properties |
| NFR-01-06 | String formatting utilities must be Unicode-aware (database uses NVARCHAR) |

---

## 7. Assessment & Completion Criteria

**Capstone Task:** Console CLI that:
1. Lets user enter company name, application date, and deadline
2. Validates all inputs using `InputValidator`
3. Calculates response rate and days until deadline using `ApplicationCalculator`
4. Displays formatted results using `StringFormatter`
5. Handles all invalid inputs with proper exception handling

**Pass Criteria:**
- [ ] All 4 utility classes implemented and functional
- [ ] Console app runs end-to-end without crashing
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| .NET 9 SDK installed | Environment | IT/DevOps |
| Visual Studio 2022 / VS Code | Tooling | IT |
| Internal Git repository access | Access | Team Lead |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
