# Job Application Tracker — Database Schema Diagram

> **Current-source note:** EF Core entity configurations and migrations are authoritative for the deployed schema. The `UserId` ownership filter is applied in every service method — never in the Angular frontend. All audit values are stamped in the `DbContext`, not by SQL triggers (unless the advanced SQL trigger is also implemented).

> 6 tables · 5 foreign key relationships · SQL Server 2022
>
> Based on the Job Application Tracker ERD and BRDs 03–05.

---

## 1. Entity Relationship Diagram (Mermaid)

```mermaid
erDiagram

    UserProfiles {
        int Id PK "IDENTITY(1,1)"
        nvarchar Email UK "NVARCHAR(150) NOT NULL"
        nvarchar PasswordHash "NVARCHAR(255) NOT NULL"
        nvarchar RefreshToken "NVARCHAR(500) NULL"
        datetime2 RefreshTokenExpiryTime "NULL"
        bit IsActive "DEFAULT 1"
        datetime2 CreatedAt "DEFAULT GETUTCDATE()"
        datetime2 UpdatedAt "NULL"
    }

    Applications {
        int Id PK "IDENTITY(1,1)"
        int UserId FK "NOT NULL REFERENCES UserProfiles CASCADE"
        nvarchar CompanyName "NVARCHAR(200) NOT NULL"
        nvarchar PositionTitle "NVARCHAR(200) NOT NULL"
        nvarchar JobUrl "NVARCHAR(500) NULL"
        nvarchar Location "NVARCHAR(100) NULL"
        nvarchar EmploymentType "CHECK: FullTime PartTime Contract Internship Remote"
        decimal SalaryMin "DECIMAL(12,2) NULL"
        decimal SalaryMax "DECIMAL(12,2) NULL"
        nvarchar Status "CHECK: Saved Applied OnlineAssessment Interview Offer Rejected"
        date Deadline "NULL"
        date FollowUpDate "NULL"
        nvarchar Notes "NVARCHAR(MAX) NULL"
        bit IsArchived "DEFAULT 0"
        datetime2 CreatedAt "DEFAULT GETUTCDATE()"
        datetime2 UpdatedAt "NULL"
    }

    ApplicationEvents {
        int Id PK "IDENTITY(1,1)"
        int ApplicationId FK "NOT NULL REFERENCES Applications CASCADE"
        nvarchar EventType "CHECK: ApplicationSubmitted AssessmentCompleted InterviewScheduled FollowUpSent OfferReceived RejectionReceived NoteAdded"
        datetime2 EventDate "NOT NULL"
        nvarchar Notes "NVARCHAR(MAX) NULL"
        datetime2 CreatedAt "DEFAULT GETUTCDATE()"
    }

    CvProfiles {
        int Id PK "IDENTITY(1,1)"
        int UserId FK "NOT NULL UNIQUE REFERENCES UserProfiles CASCADE"
        nvarchar Name "NVARCHAR(100) NOT NULL"
        nvarchar Summary "NVARCHAR(MAX) NULL"
        nvarchar SkillsText "NVARCHAR(MAX) NOT NULL"
        nvarchar ExperienceText "NVARCHAR(MAX) NULL"
        nvarchar EducationText "NVARCHAR(MAX) NULL"
        datetime2 UpdatedAt "NULL"
    }

    MatchResults {
        int Id PK "IDENTITY(1,1)"
        int UserId FK "NOT NULL REFERENCES UserProfiles NO ACTION"
        int ApplicationId FK "NULL REFERENCES Applications SET NULL"
        nvarchar JobDescriptionText "NVARCHAR(MAX) NOT NULL"
        int MatchScore "CHECK: BETWEEN 0 AND 100"
        nvarchar MatchingSkills "NVARCHAR(MAX) NULL JSON"
        nvarchar MissingSkills "NVARCHAR(MAX) NULL JSON"
        nvarchar Suggestions "NVARCHAR(MAX) NULL JSON"
        datetime2 CreatedAt "DEFAULT GETUTCDATE()"
    }

    AuditLogs {
        int Id PK "IDENTITY(1,1)"
        nvarchar TableName "NVARCHAR(100) NOT NULL"
        int RecordId "NOT NULL"
        nvarchar Action "CHECK: INSERT UPDATE DELETE"
        nvarchar ChangedBy "NVARCHAR(100) NOT NULL"
        datetime2 ChangedAt "DEFAULT GETUTCDATE()"
        nvarchar OldValues "NVARCHAR(MAX) NULL JSON"
        nvarchar NewValues "NVARCHAR(MAX) NULL JSON"
    }

    %% Relationships
    UserProfiles ||--o{ Applications      : "owns"
    UserProfiles ||--o| CvProfiles         : "has profile"
    UserProfiles ||--o{ MatchResults       : "runs matches"
    Applications ||--o{ ApplicationEvents  : "has events"
    Applications ||--o{ MatchResults       : "linked to"
```

---

## 2. Relationship Flow Diagram (Mermaid)

```mermaid
flowchart TD
    UP["UserProfiles<br/>(Id PK)<br/>Login & Auth"]
    APP["Applications<br/>(Id PK)<br/>Kanban Items"]
    EVT["ApplicationEvents<br/>(ApplicationId FK)<br/>Timeline"]
    CV["CvProfiles<br/>(UserId FK UNIQUE)<br/>One per User"]
    MR["MatchResults<br/>(UserId FK, ApplicationId FK NULL)<br/>LLM Results"]
    AL["AuditLogs<br/>(No FK - append only)"]

    UP -- "1 : N (CASCADE)" --> APP
    UP -- "1 : 0..1 (CASCADE)" --> CV
    UP -- "1 : N" --> MR
    APP -- "1 : N (CASCADE)" --> EVT
    APP -- "1 : N (SET NULL)" --> MR
    APP -. "trigger writes to" .-> AL
```

---

## 3. Foreign Key Reference Table

| FK Column | Source Table | References | On Delete |
|---|---|---|---|
| `Applications.UserId` | Applications | UserProfiles.Id | CASCADE |
| `ApplicationEvents.ApplicationId` | ApplicationEvents | Applications.Id | CASCADE |
| `CvProfiles.UserId` | CvProfiles | UserProfiles.Id | CASCADE |
| `MatchResults.UserId` | MatchResults | UserProfiles.Id | NO ACTION |
| `MatchResults.ApplicationId` | MatchResults | Applications.Id | SET NULL |

> **AuditLogs** has **no foreign keys** — it is append-only and must survive even if source records are deleted.

---

## 4. Index Catalogue

| Index Name | Table | Column(s) | Type | Purpose |
|---|---|---|---|---|
| `IX_UserProfiles_Email` | UserProfiles | Email | Non-Clustered Unique | Login lookup |
| `IX_Applications_UserId` | Applications | UserId (WHERE IsArchived=0) | Filtered Non-Clustered | User's application list |
| `IX_Applications_Status` | Applications | UserId, Status | Composite Non-Clustered | Dashboard + Kanban filter |
| `IX_Applications_Deadline` | Applications | UserId, Deadline (WHERE IsArchived=0) | Filtered Non-Clustered | Upcoming deadline widget |
| `IX_Applications_FollowUp` | Applications | UserId, FollowUpDate | Composite Non-Clustered | Follow-up reminders |
| `IX_Applications_Created` | Applications | CreatedAt | Non-Clustered | Time-sorted list |
| `IX_AppEvents_ApplicationId` | ApplicationEvents | ApplicationId | Non-Clustered | Timeline lookup |
| `IX_AppEvents_EventDate` | ApplicationEvents | ApplicationId, EventDate | Composite Non-Clustered | Ordered timeline |
| `IX_CvProfiles_UserId` | CvProfiles | UserId | Non-Clustered Unique | One CV per user |
| `IX_MatchResults_UserId` | MatchResults | UserId | Non-Clustered | Match history per user |
| `IX_AuditLogs_Table` | AuditLogs | TableName, ChangedAt | Composite Non-Clustered | Audit trail filtering |

---

## 5. Unique Constraints

| Constraint | Table | Columns |
|---|---|---|
| `UQ_UserProfiles_Email` | UserProfiles | Email |
| `UQ_CvProfiles_UserId` | CvProfiles | UserId |

---

## 6. Check Constraints

| Table | Column | Allowed Values |
|---|---|---|
| Applications | EmploymentType | `'FullTime'`, `'PartTime'`, `'Contract'`, `'Internship'`, `'Remote'` |
| Applications | Status | `'Saved'`, `'Applied'`, `'OnlineAssessment'`, `'Interview'`, `'Offer'`, `'Rejected'` |
| ApplicationEvents | EventType | `'ApplicationSubmitted'`, `'AssessmentCompleted'`, `'InterviewScheduled'`, `'FollowUpSent'`, `'OfferReceived'`, `'RejectionReceived'`, `'NoteAdded'` |
| MatchResults | MatchScore | `BETWEEN 0 AND 100` |
| AuditLogs | Action | `'INSERT'`, `'UPDATE'`, `'DELETE'` |

---

## 7. Design Decisions

| Decision | Rationale |
|---|---|
| **Soft Delete** (`IsArchived BIT`) | Preserves referential integrity, enables recovery, hidden by EF Core global query filter |
| **Ownership via UserId** | Every user-scoped table carries `UserId`. Every service method filters by it. This is the primary security boundary |
| **One-to-One CV Profile** | `UNIQUE (UserId)` on `CvProfiles` ensures exactly one profile per user. Simplifies matching — no profile selection needed |
| **JSON Columns for Matching** | `MatchingSkills`, `MissingSkills`, `Suggestions` stored as JSON arrays in `NVARCHAR(MAX)`. Avoids schema changes for variable-length LLM output |
| **Append-Only MatchResults** | Never edited — each match creates a new row. Users can track score improvement over time |
| **Append-Only AuditLogs** | No FKs — survives source record deletion. JSON snapshots via `FOR JSON PATH` |
| **Cascade Delete for Events** | Timeline events have no meaning without their parent application. CASCADE delete simplifies cleanup |
| **DECIMAL for Salary** | Never FLOAT/REAL/MONEY — avoids precision errors in salary range display |
| **PK named `Id`** | Convention-based naming. All primary keys are simply `Id` for consistency with `[Key]` data annotation |
| **NVARCHAR everywhere** | Full Unicode support for international company names, locations, and job descriptions |
