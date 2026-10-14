# Business Requirements Document
## BRD-02: Object-Oriented Programming (OOP) in C#

> **Current implementation note:** The detailed OOP lessons remain intact. In the current solution, entities belong to `JobTracker.Domain`, DTOs and validators to Business, and EF Core mappings to DataAccess. Current source takes precedence over historical exercise names and properties.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-02                                               |
| **Module**        | 2 of 7 — OOP in C#                                   |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-01 completed and signed off                      |
| **Est. Duration** | 5–7 days                                             |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This module builds the **Domain Layer** of the Job Application Tracker — the core business entities and contracts that every other module depends on.

**Module Contribution:** `JobTracker.Domain` class library

---

## 📋 APIs Addressed in This Module

This module builds the object model that directly backs the following API groups:

### Application APIs (partial implementation)
| # | Method | Endpoint | Domain Class |
|---|--------|----------|--------------|
| 4 | GET | `/api/applications` | `Application`, `ApplicationListDto` |
| 5 | GET | `/api/applications/{id}` | `Application`, `ApplicationDetailDto` |
| 6 | POST | `/api/applications` | `CreateApplicationCommand` |
| 7 | PUT | `/api/applications/{id}` | `UpdateApplicationCommand` |
| 8 | PUT | `/api/applications/{id}/status` | `Application.ChangeStatus()` |
| 9 | DELETE | `/api/applications/{id}` | `Application.Archive()` |

### Timeline APIs
| # | Method | Endpoint | Domain Class |
|---|--------|----------|--------------|
| 12 | POST | `/api/applications/{id}/events` | `ApplicationEvent` entity |
| 13 | GET | `/api/applications/{id}/events` | `ApplicationEvent`, `EventListDto` |

### Dashboard APIs
| # | Method | Endpoint | Domain Class |
|---|--------|----------|--------------|
| 14 | GET | `/api/dashboard/summary` | `DashboardSummary` |

### CV Profile & Matching APIs
| # | Method | Endpoint | Domain Class |
|---|--------|----------|--------------|
| 15 | GET | `/api/cv-profile` | `CvProfile` entity |
| 16 | PUT | `/api/cv-profile` | `UpdateCvProfileCommand` |
| 17 | POST | `/api/matching` | `MatchRequest`, `MatchResult` |

### Auth APIs
| # | Method | Endpoint | Domain Class |
|---|--------|----------|--------------|
| 1 | POST | `/api/auth/signup` | `UserProfile` entity |
| 2 | POST | `/api/auth/login` | `UserProfile` entity |
| 3 | POST | `/api/auth/refresh-token` | `UserProfile` entity |

---

## 1. Module Overview

Trainees design and implement the **Job Application Tracker Domain Model** — the rich, behavior-driven objects at the heart of the system. These classes encapsulate business rules (status transition validation, ownership enforcement), enforce invariants, and define the language of the domain.

### 1.1 Business Justification

A well-designed domain model prevents business logic from leaking into controllers, services, or the database layer. It makes the system easier to test, extend, and reason about. The application status workflow, deadline enforcement, and CV-matching boundary must live in the domain — not in Angular templates or SQL procedures.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Define classes with encapsulated properties and constructor validation
- [ ] Apply access modifiers (`public`, `private`, `protected`, `internal`)
- [ ] Build inheritance hierarchies (`ApplicationEvent : Entity`)
- [ ] Create and implement interfaces (`IAuditable`, `ISoftDeletable`, `IUserOwned`)
- [ ] Define `abstract` classes as domain contracts
- [ ] Use `virtual` / `override` for polymorphic behavior
- [ ] Apply all 5 SOLID principles with justification
- [ ] Use `enum` types for domain states (e.g., `ApplicationStatus`, `EmploymentType`, `EventType`)
- [ ] Use `record` types for immutable DTOs and commands
- [ ] Write XML documentation for all public members
- [ ] Understand why user ownership checks must live in the backend service layer

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- All domain entities: `Application`, `ApplicationEvent`, `CvProfile`, `MatchResult`, `UserProfile`
- All enums: `ApplicationStatus`, `EmploymentType`, `EventType`
- All interfaces: `IAuditable`, `ISoftDeletable`, `IUserOwned`
- Base classes: `Entity`, `EntityAudit`
- DTOs and command records for all CRUD operations

---

## 4. Functional Requirements

### FR-02-001: Base Class Hierarchy
**Description:** Trainee implements the base class hierarchy that all domain entities extend.

```
Entity (abstract)
└── EntityAudit (abstract, implements IAuditable + ISoftDeletable)
    ├── Application (sealed)
    ├── ApplicationEvent (sealed)
    ├── CvProfile (sealed)
    └── UserProfile (sealed, implements IUserOwned)
```

**Acceptance Criteria:**
- `Entity` has `[Key] Id` property only
- `EntityAudit` adds `CreatedAt`, `UpdatedAt`, `IsDeleted`
- `IUserOwned` enforces `UserId` on every user-scoped entity
- All base class members use block bodies (no `=>` expression-bodied members)

---

### FR-02-002: Application Entity
**Description:** Trainee implements the core `Application` entity with its status workflow.

**Properties:**
- `Id`, `UserId` (FK → UserProfile, ownership key)
- `CompanyName` (NVARCHAR 200)
- `PositionTitle` (NVARCHAR 200)
- `JobUrl` (NVARCHAR 500, nullable)
- `Location` (NVARCHAR 100, nullable)
- `EmploymentType` (enum: `FullTime`, `PartTime`, `Contract`, `Internship`, `Remote`)
- `SalaryMin`, `SalaryMax` (DECIMAL 12,2, nullable)
- `Status` (enum: `Saved`, `Applied`, `OnlineAssessment`, `Interview`, `Offer`, `Rejected`)
- `Deadline` (DATE, nullable)
- `FollowUpDate` (DATE, nullable)
- `Notes` (NVARCHAR MAX, nullable)
- `IsArchived` (BIT, soft-delete flag)
- `CreatedAt`, `UpdatedAt`
- `Events` (navigation — collection of ApplicationEvent)

**Domain Methods:**
- `ChangeStatus(ApplicationStatus newStatus)` — validates allowed transitions
- `Archive()` — sets `IsArchived = true`
- `IsOverdue()` — returns true if Deadline < today and Status is not Offer/Rejected

**Status Transition Rules:**
```
Saved        → Applied, Rejected
Applied      → OnlineAssessment, Interview, Offer, Rejected
OnlineAssessment → Interview, Rejected
Interview    → Offer, Rejected
Offer        → (terminal — no further transitions)
Rejected     → (terminal — no further transitions)
```

**Acceptance Criteria:**
- Invalid status transitions throw `InvalidOperationException`
- All properties use block bodies
- Navigation properties are initialized to empty collections

---

### FR-02-003: ApplicationEvent Entity
**Description:** Trainee implements the timeline event entity that records milestones per application.

**Properties:**
- `Id`, `ApplicationId` (FK → Application)
- `EventType` (enum: `ApplicationSubmitted`, `AssessmentCompleted`, `InterviewScheduled`, `FollowUpSent`, `OfferReceived`, `RejectionReceived`, `NoteAdded`)
- `EventDate` (DATETIME2)
- `Notes` (NVARCHAR MAX, nullable)
- `CreatedAt`

**Acceptance Criteria:**
- `EventDate` cannot be in the future (enforced in constructor)
- `ApplicationId` is always required

---

### FR-02-004: CvProfile Entity
**Description:** Trainee implements the text-based CV profile used for LLM matching.

**Properties:**
- `Id`, `UserId` (FK → UserProfile, unique — one profile per user)
- `Name` (NVARCHAR 100)
- `Summary` (NVARCHAR MAX)
- `SkillsText` (NVARCHAR MAX)
- `ExperienceText` (NVARCHAR MAX)
- `EducationText` (NVARCHAR MAX)
- `UpdatedAt`

**Acceptance Criteria:**
- `SkillsText` is required — cannot be null or empty (used for matching)
- One CvProfile per user enforced by unique constraint on `UserId`

---

### FR-02-005: MatchResult Entity
**Description:** Trainee implements the entity that stores LLM matching results.

**Properties:**
- `Id`, `UserId` (FK → UserProfile)
- `ApplicationId` (FK → Application, nullable — can be standalone)
- `JobDescriptionText` (NVARCHAR MAX)
- `MatchScore` (INT, 0–100)
- `MatchingSkills` (NVARCHAR MAX — JSON array)
- `MissingSkills` (NVARCHAR MAX — JSON array)
- `Suggestions` (NVARCHAR MAX — JSON array)
- `CreatedAt`

---

### FR-02-006: UserProfile Entity
**Description:** Trainee implements the user account entity that stores credentials and profile information.

**Properties:**
- `Id`
- `Email` (NVARCHAR 150, UNIQUE)
- `PasswordHash` (NVARCHAR 255)
- `RefreshToken` (NVARCHAR 500, nullable)
- `RefreshTokenExpiryTime` (DATETIME2, nullable)
- `IsActive` (BIT, DEFAULT 1)
- `CreatedAt`, `UpdatedAt`

**Acceptance Criteria:**
- Login is done via `Email` + `PasswordHash` (BCrypt)
- `RefreshToken` is rotated on each refresh

---

### FR-02-007: Enums and Interfaces

**Enums to create:**
```csharp
// ApplicationStatus.cs
public enum ApplicationStatus
{
    Saved, Applied, OnlineAssessment, Interview, Offer, Rejected
}

// EmploymentType.cs
public enum EmploymentType
{
    FullTime, PartTime, Contract, Internship, Remote
}

// EventType.cs
public enum EventType
{
    ApplicationSubmitted, AssessmentCompleted, InterviewScheduled,
    FollowUpSent, OfferReceived, RejectionReceived, NoteAdded
}
```

**Interfaces to create:**
```csharp
// IAuditable.cs — CreatedAt, UpdatedAt
// ISoftDeletable.cs — IsArchived
// IUserOwned.cs — UserId
```

---

### FR-02-008: DTOs and Command Records

Create the DTOs used by the Business services:
- `CreateApplicationDto`, `UpdateApplicationDto`, `ApplicationListDto`, `ApplicationDetailDto`
- `CreateEventDto`, `EventDto`
- `UpdateCvProfileDto`, `CvProfileDto`
- `MatchRequestDto`, `MatchResultDto`
- `DashboardSummaryDto`
- `SignupDto`, `LoginDto`

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **Rich Domain Model** | `Application.ChangeStatus()`, `Application.IsOverdue()`, `Application.Archive()` |
| **Guard Clauses** | Invalid status transitions throw `InvalidOperationException` at the domain level |
| **Value Objects** | `ApplicationStatus`, `EmploymentType`, `EventType` enums define the domain vocabulary |
| **Ownership Invariant** | `IUserOwned` ensures every user-scoped entity carries a `UserId` |
| **Soft Delete** | `IsArchived` flag — never hard delete user data |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-02-01 | All code compiles with zero errors and zero warnings |
| NFR-02-02 | All public members have XML doc comments |
| NFR-02-03 | No expression-bodied members (`=>`) — block bodies only |
| NFR-02-04 | `decimal` used for salary fields (never `double`) |
| NFR-02-05 | All navigation properties initialized to empty collections in constructors |
| NFR-02-06 | Status transition violations must throw `InvalidOperationException` from the domain |

---

## 7. Assessment & Completion Criteria

**Capstone Task:** Demo a status transition workflow:
1. Create an `Application` with status `Saved`
2. Call `ChangeStatus(Applied)` — succeeds
3. Call `ChangeStatus(Rejected)` — succeeds
4. Call `ChangeStatus(Offer)` — throws `InvalidOperationException` (Rejected is terminal)
5. Console prints formatted result using `StringFormatter`

**Pass Criteria:**
- [ ] All entities implemented with correct properties and domain methods
- [ ] All enums defined with all required values
- [ ] Status transition rules enforced
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-01 completed | Prerequisite | Trainee |
| .NET 9 SDK | Environment | IT |
| Domain entity sign-off | Approval | Team Lead |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
