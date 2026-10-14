# Business Requirements Document
## BRD-06: REST APIs in C# (ASP.NET Core Web API)

> **Current-source note:** This document provides the full REST API teaching template for the Job Application Tracker. The checked-in application targets ASP.NET Core 9. Current controller routes are authoritative; the historical API catalogue is retained as proposed extension material where a route is not yet implemented.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-06                                               |
| **Module**        | 6 of 7 — REST APIs in C#                             |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-05 completed (full data access layer)            |
| **Est. Duration** | 7–10 days                                            |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This is the core delivery module. Trainees implement **all 18 REST API endpoints** of the Job Application Tracker using ASP.NET Core 9 Web API.

**Module Contribution:** `JobTracker.Api` — the production-ready ASP.NET Core 9 Web API project

---

## 📋 All 18 APIs — Full Implementation Specification

### Auth Controller (`/api/auth`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 1 | POST | `/api/auth/signup` | `AuthController.Signup` | No |
| 2 | POST | `/api/auth/login` | `AuthController.Login` | No |
| 3 | POST | `/api/auth/refresh-token` | `AuthController.RefreshToken` | No |

### Applications Controller (`/api/applications`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 4 | GET | `/api/applications` | `GetApplications` | Yes |
| 5 | GET | `/api/applications/{id}` | `GetApplication` | Yes |
| 6 | POST | `/api/applications` | `CreateApplication` | Yes |
| 7 | PUT | `/api/applications/{id}` | `UpdateApplication` | Yes |
| 8 | PUT | `/api/applications/{id}/status` | `ChangeStatus` | Yes |
| 9 | DELETE | `/api/applications/{id}` | `ArchiveApplication` | Yes |
| 10 | GET | `/api/applications/search` | `SearchApplications` | Yes |
| 11 | GET | `/api/applications/export` | `ExportApplications` | Yes |

### Timeline Controller (`/api/applications/{id}/events`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 12 | POST | `/api/applications/{id}/events` | `AddEvent` | Yes |
| 13 | GET | `/api/applications/{id}/events` | `GetEvents` | Yes |

### Dashboard Controller (`/api/dashboard`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 14 | GET | `/api/dashboard/summary` | `GetSummary` | Yes |

### CV Profile Controller (`/api/cv-profile`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 15 | GET | `/api/cv-profile` | `GetProfile` | Yes |
| 16 | PUT | `/api/cv-profile` | `SaveProfile` | Yes |

### Matching Controller (`/api/matching`)

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 17 | POST | `/api/matching` | `MatchController.Match` | Yes |

### Infrastructure

| # | Method | Endpoint | Controller Action | Auth Required |
|---|--------|----------|-------------------|---------------|
| 18 | GET | `/api/health` | Health Check (built-in) | No |

---

## 1. Module Overview

Trainees implement the complete HTTP layer of the Job Application Tracker — all controllers, service interfaces, service implementations, validators, and middleware. By the end of this module, all 18 endpoints are reachable via Swagger and tested against a live SQL Server database.

### 1.1 Business Justification

The REST API is the contract between the Angular frontend and the SQL Server backend. Every status transition, ownership check, and dashboard calculation must be enforced here — not in the frontend. A clearly defined, consistently structured API makes both backend and frontend development predictable and testable.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Configure ASP.NET Core 9 Web API projects with `Program.cs` minimal hosting
- [ ] Write controllers with `[ApiController]`, `[Route]`, and HTTP verb attributes
- [ ] Implement service interfaces and concrete service classes
- [ ] Use `IHttpContextAccessor` to extract the authenticated user's `UserId` from JWT claims
- [ ] Apply JWT Bearer authentication with `[Authorize]`
- [ ] Use FluentValidation for automatic DTO validation
- [ ] Return consistent `ApiResponse<T>` and `PagedResult<T>` wrappers
- [ ] Implement global exception handling middleware
- [ ] Write CRUD endpoints with proper HTTP status codes
- [ ] Implement LLM provider integration with fallback mock provider
- [ ] Test all endpoints via Swagger UI

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- `JobTracker.Api` — ASP.NET Core 9 Web API project
- All 5 controllers: `AuthController`, `ApplicationsController`, `DashboardController`, `CvProfileController`, `MatchingController`
- All service interfaces and implementations in `JobTracker.Business`
- FluentValidation validators for all command DTOs
- JWT authentication + authorization middleware
- Global exception middleware
- LLM matching service with mock provider fallback
- Swagger / OpenAPI documentation

---

## 4. Functional Requirements

### FR-06-001: NuGet Packages

Install in `JobTracker.Api`:
- `Swashbuckle.AspNetCore`
- `FluentValidation.AspNetCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`
- `BCrypt.Net-Next`

---

### FR-06-002: Configure `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=JobTracker_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "YOUR_JWT_SECRET_MIN_256_BITS",
    "Issuer": "JobTrackerApi",
    "Audience": "JobTrackerClient",
    "ExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "Matching": {
    "Provider": "Mock",
    "OpenAiApiKey": "",
    "RequestTimeoutSeconds": 30,
    "MaxInputLength": 8000
  }
}
```

---

### FR-06-003: Configure `Program.cs`

Register:
- `JobTrackerDbContext` with SQL Server
- `IUnitOfWork` / `UnitOfWork`
- Repository interfaces
- Service interfaces
- JWT authentication with `JwtBearerDefaults`
- FluentValidation auto-validation
- Swagger with JWT bearer scheme
- `GlobalExceptionMiddleware`
- Health checks with SQL Server probe
- `IHttpContextAccessor` for current user claim extraction

---

### FR-06-004: Common Infrastructure Classes

```
JobTracker.Api/
├── Common/
│   ├── ApiResponse.cs         ← { success, data, message, errors }
│   └── PagedResult.cs         ← { items, totalCount, page, pageSize, totalPages, hasNext, hasPrev }
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
├── Configuration/
│   ├── AuthConfiguration.cs
│   └── DependencyInjection.cs
└── Extensions/
    └── ClaimsPrincipalExtensions.cs   ← GetCurrentUserId()
```

**Current User Extraction:**
```csharp
// Extensions/ClaimsPrincipalExtensions.cs
public static class ClaimsPrincipalExtensions
{
    public static int GetCurrentUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("User identity claim is missing.");
        }
        return userId;
    }
}
```

---

### FR-06-005: Auth Controller

```
POST /api/auth/signup
Body: { email, password, confirmPassword }
Response: 201 Created — { userId, email, token, refreshToken }
Rules:
  - Email must be unique
  - Password minimum 8 characters, at least 1 number
  - Hash password with BCrypt before storing

POST /api/auth/login
Body: { email, password }
Response: 200 OK — { token (15 min), refreshToken (7 days) }
Rules:
  - Return 401 for invalid credentials (same message for unknown email and wrong password)
  - Rotate refresh token on every successful login

POST /api/auth/refresh-token
Body: { refreshToken }
Response: 200 OK — { token, refreshToken }
Rules:
  - Validate token is not expired and belongs to an active user
  - Rotate refresh token
```

---

### FR-06-006: Applications Controller

All application endpoints read the current `UserId` from the JWT and filter all data by it. **No endpoint may return another user's applications.**

```
GET /api/applications
Query: ?status=Applied&employmentType=Internship&page=1&pageSize=20&sort=deadline
Response: 200 OK — PagedResult<ApplicationListDto>

GET /api/applications/{id}
Response: 200 OK — ApplicationDetailDto (with events)
Rules: Return 404 if id not found or belongs to another user

POST /api/applications
Body: CreateApplicationDto
Response: 201 Created — ApplicationDetailDto
Rules:
  - CompanyName and PositionTitle are required
  - JobUrl must be a valid https URL when provided
  - SalaryMax must be >= SalaryMin when both are provided
  - Deadline must be today or in the future

PUT /api/applications/{id}
Body: UpdateApplicationDto
Response: 200 OK — ApplicationDetailDto
Rules: Return 404 if not found or belongs to another user

PUT /api/applications/{id}/status
Body: { newStatus: "Interview", notes: "Scheduled for next Monday" }
Response: 200 OK — { applicationId, previousStatus, newStatus }
Rules:
  - Enforce status transition rules (mirrors domain model)
  - Auto-insert an ApplicationEvent with the matching EventType
  - Return 422 for invalid transitions

DELETE /api/applications/{id}
Response: 204 No Content
Rules: Soft-delete only — set IsArchived = 1

GET /api/applications/search
Query: ?q=Google&status=Interview&deadline=2026-12-31
Response: 200 OK — PagedResult<ApplicationListDto>

GET /api/applications/export
Response: 200 OK — text/csv file download
Content: All user's active applications in CSV format
```

---

### FR-06-007: Timeline Controller (nested under Applications)

```
POST /api/applications/{id}/events
Body: { eventType, eventDate, notes }
Response: 201 Created — EventDto
Rules:
  - applicationId must belong to current user
  - eventDate cannot be in the future

GET /api/applications/{id}/events
Response: 200 OK — List<EventDto> ordered by eventDate ASC
Rules: applicationId must belong to current user
```

---

### FR-06-008: Dashboard Controller

```
GET /api/dashboard/summary
Response: 200 OK — DashboardSummaryDto

DashboardSummaryDto:
{
  totalApplications: 42,
  savedCount: 5,
  appliedCount: 20,
  assessmentCount: 8,
  interviewCount: 6,
  offerCount: 1,
  rejectedCount: 2,
  responseRate: 80.95,            // (all except Saved) / total × 100
  upcomingDeadlines: [...],        // next 14 days
  upcomingFollowUps: [...]         // next 7 days
}
```

---

### FR-06-009: CV Profile Controller

```
GET /api/cv-profile
Response: 200 OK — CvProfileDto
Rules: Return 404 if user has not created a profile yet

PUT /api/cv-profile
Body: UpdateCvProfileDto { name, summary, skillsText, experienceText, educationText }
Response: 200 OK — CvProfileDto
Rules:
  - SkillsText is required (used by matching)
  - Create on first PUT, update on subsequent PUTs (upsert behavior)
```

---

### FR-06-010: Matching Controller

```
POST /api/matching
Body: { jobDescription: "...", cvProfileId: 12 }
Response: 200 OK — MatchResultDto

MatchResultDto:
{
  matchScore: 78,
  matchingSkills: ["TypeScript", "REST APIs", "SQL"],
  missingSkills: ["Docker", "Azure Functions"],
  suggestions: ["Mention REST API project in experience section."]
}

Rules:
  - Validate jobDescription is not empty and does not exceed 8000 characters
  - cvProfileId must belong to current user
  - Use mock provider if real provider is not configured or request times out
  - Validate LLM response is valid JSON before returning
  - Return 422 with clear message if provider is unavailable and mock is disabled
  - Never expose provider API key in response
  - Store match result in MatchResults table
```

**LLM Provider Abstraction:**
```csharp
// Business/Matching/ILlmProvider.cs
public interface ILlmProvider
{
    Task<MatchResultDto> MatchAsync(string jobDescription, string cvSkillsText);
}

// Business/Matching/MockLlmProvider.cs — deterministic, for tests
// Business/Matching/OpenAiLlmProvider.cs — real provider
```

---

### FR-06-011: Service Layer

Create in `JobTracker.Business/Services/`:

| Interface | Implementation | Responsibility |
|---|---|---|
| `IAuthService` | `AuthService` | Signup, login, token generation, refresh (BCrypt + JWT) |
| `IApplicationService` | `ApplicationService` | CRUD + ownership enforcement + status transitions |
| `IDashboardService` | `DashboardService` | Aggregation via DashboardRepository (Dapper) |
| `ICvProfileService` | `CvProfileService` | Get/upsert CV profile per user |
| `IMatchingService` | `MatchingService` | Delegate to LLM provider, store and return result |

---

### FR-06-012: FluentValidation Validators

| Validator | DTO | Key Rules |
|---|---|---|
| `SignupValidator` | `SignupDto` | Email format, password min 8 chars + 1 digit |
| `LoginValidator` | `LoginDto` | Email format, password not empty |
| `CreateApplicationValidator` | `CreateApplicationDto` | CompanyName required, JobUrl is valid URL, SalaryMax >= SalaryMin |
| `UpdateApplicationValidator` | `UpdateApplicationDto` | Same rules as create |
| `ChangeStatusValidator` | `ChangeStatusDto` | NewStatus must be a valid `ApplicationStatus` enum value |
| `AddEventValidator` | `CreateEventDto` | EventType required, EventDate not in future |
| `UpdateCvProfileValidator` | `UpdateCvProfileDto` | SkillsText required, Name required |
| `MatchRequestValidator` | `MatchRequestDto` | JobDescription required, max 8000 chars |

---

### FR-06-013: Authorization Roles

| Role | Access |
|---|---|
| `User` | Own applications, own CV profile, own matching results, own dashboard |
| `Admin` | All user data (admin panel), user management |

> All data endpoints filter by `UserId` extracted from the JWT claim. The `Admin` role bypasses the user filter for moderation purposes only.

---

### FR-06-014: Run and Test

F5 → Swagger UI → test all 18 endpoints:
1. Signup → Login → copy JWT
2. Authorize in Swagger with Bearer token
3. Create 3 applications → list them → change one status
4. Retrieve dashboard summary → verify counts match
5. Create CV profile → call matching endpoint
6. Export applications as CSV

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **API Response Wrapper** | `ApiResponse<T>` — all endpoints return `{ success, data, message, errors }` |
| **Pagination** | `PagedResult<T>` — all list endpoints are paginated |
| **Current User Claim** | `IHttpContextAccessor` → `ClaimsPrincipalExtensions.GetCurrentUserId()` |
| **Ownership Enforcement** | Every service method receives `userId` and filters data accordingly |
| **Mock Provider Pattern** | `ILlmProvider` enables test-time mock injection without network calls |
| **Fail-Safe Matching** | On provider failure, return a structured error — never silently return empty data |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-06-01 | No application data ever crosses user ownership boundaries |
| NFR-06-02 | Provider API key is only read server-side — never in response body or logs |
| NFR-06-03 | LLM input size validated before sending to provider (max 8000 chars) |
| NFR-06-04 | All write endpoints use `[Authorize]` |
| NFR-06-05 | All controllers return `ApiResponse<T>` — never raw objects |
| NFR-06-06 | No expression-bodied members (`=>`) — block bodies only |
| NFR-06-07 | Swagger documents every endpoint with summary, request body, and response schema |

---

## 7. Assessment & Completion Criteria

**Capstone Task (full vertical slice):**
1. Sign up, log in, copy JWT
2. Create 5 applications in different statuses
3. Request dashboard summary — verify counts
4. Change one application from `Applied` to `Interview` — verify event logged
5. Create CV profile and call matching endpoint
6. Export all applications as CSV

**Pass Criteria:**
- [ ] All 18 endpoints implemented and reachable in Swagger
- [ ] Ownership enforced — changing user ID in URL returns 404, not another user's data
- [ ] Mock matching provider returns consistent response in tests
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-05 completed | Prerequisite | Trainee |
| SQL Server 2022 + migrations applied | Environment | IT |
| LLM provider API key (optional) | Config | Team Lead |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
