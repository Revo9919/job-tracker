# Business Requirements Document
## BRD-04: Advanced SQL

> **Current implementation note:** This document covers advanced SQL concepts applied to the Job Application Tracker. EF Core Code-First migrations are authoritative. Views, stored procedures, indexes, and triggers listed here are retained as learning exercises and optional extensions. Implement them only alongside corresponding service/repository code and tests.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-04                                               |
| **Module**        | 4 of 7 — Advanced SQL                                |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-03 completed and signed off                      |
| **Est. Duration** | 4–6 days                                             |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This module covers advanced SQL objects that can optimize and extend the Job Application Tracker. All objects here support specific API endpoints or report queries.

**Module Contribution:** Advanced SQL skills — views, stored procedures, triggers, indexes, window functions.

---

## 📋 Advanced SQL Objects

| # | Object | Type | Supports API |
|---|---|---|---|
| 1 | `vw_ActiveApplications` | View | GET `/api/applications` |
| 2 | `vw_DashboardSummary` | View | GET `/api/dashboard/summary` |
| 3 | `vw_UpcomingDeadlines` | View | GET `/api/dashboard/summary` |
| 4 | `usp_ArchiveOldRejections` | Stored Procedure | Maintenance job |
| 5 | `usp_GetUserDashboard` | Stored Procedure | GET `/api/dashboard/summary` |
| 6 | `usp_ChangeApplicationStatus` | Stored Procedure | PUT `/api/applications/{id}/status` |
| 7 | `usp_GetApplicationTimeline` | Stored Procedure | GET `/api/applications/{id}/events` |
| 8 | `fn_GetResponseRate` | Scalar UDF | Used by dashboard SP |
| 9 | `fn_GetStatusLabel` | Scalar UDF | Human-readable status labels |
| 10 | `trg_Applications_Audit` | AFTER UPDATE Trigger | Writes to AuditLogs |

---

## 1. Module Overview

Trainees extend their SQL skills with performance-focused and business-logic SQL objects. Every object maps directly to a real feature in the Job Application Tracker — the dashboard, status management, timeline, and audit trail.

### 1.1 Business Justification

Advanced SQL techniques — indexed views, parameterized stored procedures, and window functions — make the dashboard performant as a user's application count grows. The audit trigger ensures every status change is captured without requiring application code changes.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Create and query views (`CREATE VIEW`, `WITH SCHEMABINDING`)
- [ ] Write parameterized stored procedures with `OUTPUT` parameters and `TRY/CATCH`
- [ ] Write scalar and table-valued user-defined functions
- [ ] Use window functions: `ROW_NUMBER()`, `RANK()`, `LAG()`, `LEAD()`
- [ ] Write `AFTER` and `INSTEAD OF` triggers
- [ ] Design and create non-clustered, filtered, and composite indexes
- [ ] Write `MERGE` statements for upsert operations
- [ ] Use `FOR JSON PATH` and `OPENJSON` for JSON data
- [ ] Use CTEs (`WITH ...`) for readable complex queries
- [ ] Understand query plans with `SET STATISTICS IO ON` and `EXPLAIN`

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- 3 views for optimized read queries
- 4 stored procedures for business-critical operations
- 2 scalar UDFs used within stored procedures
- 1 audit trigger
- 11 performance indexes (see Section 4.5)

---

## 4. Functional Requirements

### FR-04-001: Views

#### `vw_ActiveApplications`
Pre-joined view of non-archived applications with user information. Used by the application list API.

```sql
CREATE VIEW vw_ActiveApplications
AS
SELECT
    a.Id,
    a.UserId,
    up.Email           AS UserEmail,
    a.CompanyName,
    a.PositionTitle,
    a.JobUrl,
    a.Location,
    a.EmploymentType,
    a.SalaryMin,
    a.SalaryMax,
    a.Status,
    a.Deadline,
    a.FollowUpDate,
    a.Notes,
    a.CreatedAt,
    a.UpdatedAt,
    CASE
        WHEN a.Deadline IS NOT NULL
             AND a.Deadline < CAST(GETDATE() AS DATE)
             AND a.Status NOT IN ('Offer', 'Rejected')
        THEN 1
        ELSE 0
    END                AS IsOverdue
FROM Applications a
JOIN UserProfiles up ON up.Id = a.UserId
WHERE a.IsArchived = 0;
```

#### `vw_DashboardSummary`
Aggregated status counts per user for the dashboard endpoint.

```sql
CREATE VIEW vw_DashboardSummary
AS
SELECT
    UserId,
    COUNT(*)                                              AS TotalApplications,
    SUM(CASE WHEN Status = 'Saved'             THEN 1 ELSE 0 END) AS SavedCount,
    SUM(CASE WHEN Status = 'Applied'           THEN 1 ELSE 0 END) AS AppliedCount,
    SUM(CASE WHEN Status = 'OnlineAssessment'  THEN 1 ELSE 0 END) AS AssessmentCount,
    SUM(CASE WHEN Status = 'Interview'         THEN 1 ELSE 0 END) AS InterviewCount,
    SUM(CASE WHEN Status = 'Offer'             THEN 1 ELSE 0 END) AS OfferCount,
    SUM(CASE WHEN Status = 'Rejected'          THEN 1 ELSE 0 END) AS RejectedCount,
    CAST(
        SUM(CASE WHEN Status NOT IN ('Saved') THEN 1.0 ELSE 0 END)
        / NULLIF(COUNT(*), 0) * 100
    AS DECIMAL(5,2))                                      AS ResponseRate
FROM Applications
WHERE IsArchived = 0
GROUP BY UserId;
```

#### `vw_UpcomingDeadlines`
Applications with deadlines in the next 14 days, used by the dashboard widget.

```sql
CREATE VIEW vw_UpcomingDeadlines
AS
SELECT
    a.Id,
    a.UserId,
    a.CompanyName,
    a.PositionTitle,
    a.Status,
    a.Deadline,
    a.FollowUpDate,
    DATEDIFF(DAY, GETDATE(), a.Deadline) AS DaysRemaining
FROM Applications a
WHERE a.IsArchived = 0
  AND a.Deadline IS NOT NULL
  AND a.Deadline BETWEEN CAST(GETDATE() AS DATE)
                     AND DATEADD(DAY, 14, CAST(GETDATE() AS DATE))
  AND a.Status NOT IN ('Offer', 'Rejected');
```

**Acceptance Criteria:**
- All 3 views created without errors
- `vw_DashboardSummary` returns one row per user
- `vw_UpcomingDeadlines` never returns offers or rejections

---

### FR-04-002: Scalar User-Defined Functions

#### `fn_GetResponseRate`
```sql
CREATE FUNCTION dbo.fn_GetResponseRate
(
    @UserId INT
)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @Total    INT;
    DECLARE @Responded INT;
    DECLARE @Rate     DECIMAL(5,2);

    SELECT @Total     = COUNT(*),
           @Responded = SUM(CASE WHEN Status NOT IN ('Saved') THEN 1 ELSE 0 END)
    FROM Applications
    WHERE UserId = @UserId AND IsArchived = 0;

    SET @Rate = CAST(@Responded AS DECIMAL(5,2)) / NULLIF(@Total, 0) * 100;
    RETURN ISNULL(@Rate, 0);
END;
```

#### `fn_GetStatusLabel`
Returns a human-readable label for each status value.
```sql
CREATE FUNCTION dbo.fn_GetStatusLabel
(
    @Status NVARCHAR(30)
)
RETURNS NVARCHAR(50)
AS
BEGIN
    RETURN CASE @Status
        WHEN 'Saved'             THEN 'Saved'
        WHEN 'Applied'           THEN 'Applied'
        WHEN 'OnlineAssessment'  THEN 'Online Assessment'
        WHEN 'Interview'         THEN 'Interview'
        WHEN 'Offer'             THEN 'Offer Received'
        WHEN 'Rejected'          THEN 'Rejected'
        ELSE 'Unknown'
    END;
END;
```

---

### FR-04-003: Stored Procedures

#### `usp_GetUserDashboard`
Returns full dashboard summary for a user including counts, response rate, and upcoming deadlines.

```sql
CREATE PROCEDURE usp_GetUserDashboard
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Status summary
    SELECT * FROM vw_DashboardSummary WHERE UserId = @UserId;

    -- Upcoming deadlines (next 14 days)
    SELECT * FROM vw_UpcomingDeadlines WHERE UserId = @UserId ORDER BY DaysRemaining ASC;

    -- Upcoming follow-ups (next 7 days)
    SELECT CompanyName, PositionTitle, FollowUpDate,
           DATEDIFF(DAY, GETDATE(), FollowUpDate) AS DaysUntilFollowUp
    FROM Applications
    WHERE UserId = @UserId
      AND IsArchived = 0
      AND FollowUpDate BETWEEN CAST(GETDATE() AS DATE)
                           AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE));
END;
```

#### `usp_ChangeApplicationStatus`
Atomically changes status, validates transitions, and inserts an audit event.

```sql
CREATE PROCEDURE usp_ChangeApplicationStatus
    @ApplicationId INT,
    @UserId        INT,
    @NewStatus     NVARCHAR(30),
    @Notes         NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @OwnerId       INT;

        SELECT @CurrentStatus = Status, @OwnerId = UserId
        FROM Applications
        WHERE Id = @ApplicationId AND IsArchived = 0;

        IF @OwnerId IS NULL
            THROW 50001, 'Application not found.', 1;
        IF @OwnerId <> @UserId
            THROW 50002, 'Access denied — this application belongs to another user.', 1;

        -- Validate allowed transitions (mirrors domain logic)
        IF NOT (
            (@CurrentStatus = 'Saved'            AND @NewStatus IN ('Applied','Rejected')) OR
            (@CurrentStatus = 'Applied'          AND @NewStatus IN ('OnlineAssessment','Interview','Offer','Rejected')) OR
            (@CurrentStatus = 'OnlineAssessment' AND @NewStatus IN ('Interview','Rejected')) OR
            (@CurrentStatus = 'Interview'        AND @NewStatus IN ('Offer','Rejected'))
        )
            THROW 50003, 'Invalid status transition.', 1;

        UPDATE Applications
        SET Status    = @NewStatus,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @ApplicationId;

        INSERT INTO ApplicationEvents (ApplicationId, EventType, EventDate, Notes, CreatedAt)
        VALUES (@ApplicationId,
                CASE @NewStatus
                    WHEN 'Applied'          THEN 'ApplicationSubmitted'
                    WHEN 'OnlineAssessment' THEN 'AssessmentCompleted'
                    WHEN 'Interview'        THEN 'InterviewScheduled'
                    WHEN 'Offer'            THEN 'OfferReceived'
                    WHEN 'Rejected'         THEN 'RejectionReceived'
                    ELSE 'NoteAdded'
                END,
                GETUTCDATE(),
                @Notes,
                GETUTCDATE());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
```

#### `usp_ArchiveOldRejections`
Maintenance procedure that archives rejected applications older than N days.

```sql
CREATE PROCEDURE usp_ArchiveOldRejections
    @DaysOld INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Applications
    SET IsArchived = 1, UpdatedAt = GETUTCDATE()
    WHERE Status = 'Rejected'
      AND IsArchived = 0
      AND CreatedAt < DATEADD(DAY, -@DaysOld, GETUTCDATE());

    SELECT @@ROWCOUNT AS ArchivedCount;
END;
```

#### `usp_GetApplicationTimeline`
Returns all timeline events for a specific application with ownership check.

```sql
CREATE PROCEDURE usp_GetApplicationTimeline
    @ApplicationId INT,
    @UserId        INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM Applications WHERE Id = @ApplicationId AND UserId = @UserId
    )
        THROW 50002, 'Application not found or access denied.', 1;

    SELECT EventType, dbo.fn_GetStatusLabel(EventType) AS EventLabel,
           EventDate, Notes, CreatedAt
    FROM ApplicationEvents
    WHERE ApplicationId = @ApplicationId
    ORDER BY EventDate ASC;
END;
```

---

### FR-04-004: Audit Trigger

#### `trg_Applications_Audit`
Writes a record to `AuditLogs` after every `UPDATE` on the `Applications` table.

```sql
CREATE TRIGGER trg_Applications_Audit
ON Applications
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLogs (TableName, RecordId, Action, ChangedBy, ChangedAt, OldValues, NewValues)
    SELECT
        'Applications',
        d.Id,
        'UPDATE',
        'system',
        GETUTCDATE(),
        (SELECT * FROM deleted d2 WHERE d2.Id = d.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
        (SELECT * FROM inserted i2 WHERE i2.Id = d.Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM deleted d;
END;
```

---

### FR-04-005: Performance Indexes

| Index Name | Table | Column(s) | Type | Purpose |
|---|---|---|---|---|
| `IX_Applications_UserId` | Applications | `UserId` WHERE `IsArchived=0` | Filtered Non-Clustered | User's application list |
| `IX_Applications_Status` | Applications | `UserId, Status` | Composite Non-Clustered | Status filter on dashboard |
| `IX_Applications_Deadline` | Applications | `UserId, Deadline` WHERE `IsArchived=0` | Filtered Non-Clustered | Upcoming deadline queries |
| `IX_Applications_FollowUp` | Applications | `UserId, FollowUpDate` | Composite Non-Clustered | Follow-up date queries |
| `IX_Applications_Created` | Applications | `CreatedAt` | Non-Clustered | Time-sorted application list |
| `IX_AppEvents_ApplicationId` | ApplicationEvents | `ApplicationId` | Non-Clustered | Timeline lookup per application |
| `IX_AppEvents_EventDate` | ApplicationEvents | `ApplicationId, EventDate` | Composite Non-Clustered | Ordered timeline |
| `IX_UserProfiles_Email` | UserProfiles | `Email` | Non-Clustered Unique | Login lookup |
| `IX_CvProfiles_UserId` | CvProfiles | `UserId` | Non-Clustered Unique | Single CV per user |
| `IX_MatchResults_UserId` | MatchResults | `UserId` | Non-Clustered | Match history per user |
| `IX_AuditLogs_Table` | AuditLogs | `TableName, ChangedAt` | Composite Non-Clustered | Audit trail filtering |

---

### FR-04-006: Window Function Exercises

```sql
-- Rank applications per user by how recently they were updated
SELECT
    UserId, CompanyName, PositionTitle, UpdatedAt,
    ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY UpdatedAt DESC) AS RecentRank
FROM Applications
WHERE IsArchived = 0;

-- Show how many days passed between consecutive status changes for an application
SELECT
    ApplicationId,
    EventType,
    EventDate,
    LAG(EventDate) OVER (PARTITION BY ApplicationId ORDER BY EventDate) AS PreviousEventDate,
    DATEDIFF(DAY,
        LAG(EventDate) OVER (PARTITION BY ApplicationId ORDER BY EventDate),
        EventDate
    ) AS DaysBetweenSteps
FROM ApplicationEvents
ORDER BY ApplicationId, EventDate;
```

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **Ownership Enforcement in SQL** | Every SP checks `UserId` before mutating data |
| **TRY/CATCH Transactions** | `usp_ChangeApplicationStatus` is atomic — rolls back on any failure |
| **Append-Only Audit** | `AuditLogs` trigger never modifies existing rows |
| **Mirror Domain Rules in SQL** | Status transition validation duplicated in SP to protect against direct DB access |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-04-01 | All SPs use `SET NOCOUNT ON` |
| NFR-04-02 | All SPs use `TRY/CATCH/TRANSACTION` for write operations |
| NFR-04-03 | All SPs enforce `@UserId` ownership — no cross-user access |
| NFR-04-04 | `AuditLogs` is append-only — no UPDATE or DELETE ever written against it |
| NFR-04-05 | `MatchScore` always between 0 and 100 (CHECK constraint) |
| NFR-04-06 | All string columns use `NVARCHAR` |

---

## 7. Assessment & Completion Criteria

**Capstone Task:**
1. Call `usp_GetUserDashboard` for a seeded user and verify the response rate
2. Call `usp_ChangeApplicationStatus` for an invalid transition and verify it throws
3. Verify the audit trigger wrote to `AuditLogs` after an update

**Pass Criteria:**
- [ ] All 3 views return correct data
- [ ] All 4 stored procedures execute successfully
- [ ] Audit trigger writes correct JSON to `AuditLogs`
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-03 completed | Prerequisite | Trainee |
| SQL Server 2022 / SSMS | Environment | IT |
| Seed data from BRD-03 | Data | Trainee |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
