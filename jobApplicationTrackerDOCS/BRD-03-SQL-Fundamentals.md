# Business Requirements Document
## BRD-03: SQL Fundamentals

> **Current implementation note:** This document provides the SQL training curriculum in the context of the Job Application Tracker database. EF Core Code-First migrations are the authoritative schema source. All SQL examples here are for learning and manual verification purposes.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-03                                               |
| **Module**        | 3 of 7 — SQL Fundamentals                            |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-02 completed and signed off                      |
| **Est. Duration** | 4–6 days                                             |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This module teaches SQL fundamentals using the **Job Application Tracker database schema** as the hands-on context.
> Every SQL exercise produces a query or script that could be run against the real application database.

**Module Contribution:** SQL skills to understand and verify the EF Core-generated schema in SSMS.

---

## 📋 Database Tables in Scope

The Job Application Tracker database has **6 tables**:

| # | Table | Purpose |
|---|---|---|
| 1 | `UserProfiles` | User accounts — login credentials and tokens |
| 2 | `Applications` | Core application records (Kanban items) |
| 3 | `ApplicationEvents` | Timeline events per application |
| 4 | `CvProfiles` | User CV text profiles for matching |
| 5 | `MatchResults` | Stored LLM matching results |
| 6 | `AuditLogs` | System-wide immutable change log |

---

## 1. Module Overview

Trainees learn SQL by querying, filtering, joining, and aggregating the Job Application Tracker data. Every concept is grounded in a real scenario — finding overdue applications, calculating response rates, or analysing application status distributions.

### 1.1 Business Justification

SQL literacy allows developers to verify EF Core migrations match the intended schema, debug data issues directly in SSMS, write performant report queries, and understand how EF Core generates its SQL output.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Write `SELECT`, `WHERE`, `ORDER BY`, and `LIMIT`/`TOP` queries
- [ ] Filter with `AND`, `OR`, `NOT`, `IN`, `BETWEEN`, `LIKE`, and `IS NULL`
- [ ] Use `JOIN` (INNER, LEFT, RIGHT, FULL) to query related tables
- [ ] Write aggregation queries: `COUNT`, `SUM`, `AVG`, `MIN`, `MAX` with `GROUP BY` and `HAVING`
- [ ] Use subqueries and `EXISTS`
- [ ] Write `INSERT`, `UPDATE`, and `DELETE` statements
- [ ] Create and modify tables with `CREATE TABLE`, `ALTER TABLE`, `DROP TABLE`
- [ ] Understand primary keys, foreign keys, `NOT NULL`, `DEFAULT`, `UNIQUE`, and `CHECK` constraints
- [ ] Use `CASE WHEN` for conditional output
- [ ] Use common string and date functions (`GETUTCDATE()`, `DATEDIFF`, `DATEPART`, `LEN`, `TRIM`, `UPPER`)

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- SQL literacy for schema verification in SSMS
- Manual seed data scripts (used before EF Core `HasData` is configured)
- Foundation for BRD-04 (Advanced SQL: views, stored procedures, indexes)

---

## 4. Functional Requirements

### FR-03-001: CREATE TABLE Statements
**Description:** Trainee manually writes the full DDL for all 6 tables to understand the schema before relying on EF Core migrations.

**Key constraints to implement:**
```sql
-- UserProfiles
CREATE TABLE UserProfiles (
    Id          INT            IDENTITY(1,1) PRIMARY KEY,
    Email       NVARCHAR(150)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    RefreshToken NVARCHAR(500) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL,
    IsActive    BIT            NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2      NULL
);

-- Applications
CREATE TABLE Applications (
    Id             INT            IDENTITY(1,1) PRIMARY KEY,
    UserId         INT            NOT NULL REFERENCES UserProfiles(Id) ON DELETE CASCADE,
    CompanyName    NVARCHAR(200)  NOT NULL,
    PositionTitle  NVARCHAR(200)  NOT NULL,
    JobUrl         NVARCHAR(500)  NULL,
    Location       NVARCHAR(100)  NULL,
    EmploymentType NVARCHAR(30)   NOT NULL
                   CHECK (EmploymentType IN ('FullTime','PartTime','Contract','Internship','Remote')),
    SalaryMin      DECIMAL(12,2)  NULL,
    SalaryMax      DECIMAL(12,2)  NULL,
    Status         NVARCHAR(30)   NOT NULL DEFAULT 'Saved'
                   CHECK (Status IN ('Saved','Applied','OnlineAssessment','Interview','Offer','Rejected')),
    Deadline       DATE           NULL,
    FollowUpDate   DATE           NULL,
    Notes          NVARCHAR(MAX)  NULL,
    IsArchived     BIT            NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt      DATETIME2      NULL
);

-- ApplicationEvents
CREATE TABLE ApplicationEvents (
    Id            INT           IDENTITY(1,1) PRIMARY KEY,
    ApplicationId INT           NOT NULL REFERENCES Applications(Id) ON DELETE CASCADE,
    EventType     NVARCHAR(50)  NOT NULL
                  CHECK (EventType IN (
                      'ApplicationSubmitted','AssessmentCompleted','InterviewScheduled',
                      'FollowUpSent','OfferReceived','RejectionReceived','NoteAdded')),
    EventDate     DATETIME2     NOT NULL,
    Notes         NVARCHAR(MAX) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

-- CvProfiles
CREATE TABLE CvProfiles (
    Id             INT           IDENTITY(1,1) PRIMARY KEY,
    UserId         INT           NOT NULL UNIQUE REFERENCES UserProfiles(Id) ON DELETE CASCADE,
    Name           NVARCHAR(100) NOT NULL,
    Summary        NVARCHAR(MAX) NULL,
    SkillsText     NVARCHAR(MAX) NOT NULL,
    ExperienceText NVARCHAR(MAX) NULL,
    EducationText  NVARCHAR(MAX) NULL,
    UpdatedAt      DATETIME2     NULL
);

-- MatchResults
CREATE TABLE MatchResults (
    Id                  INT           IDENTITY(1,1) PRIMARY KEY,
    UserId              INT           NOT NULL REFERENCES UserProfiles(Id) ON DELETE NO ACTION,
    ApplicationId       INT           NULL REFERENCES Applications(Id) ON DELETE SET NULL,
    JobDescriptionText  NVARCHAR(MAX) NOT NULL,
    MatchScore          INT           NOT NULL CHECK (MatchScore BETWEEN 0 AND 100),
    MatchingSkills      NVARCHAR(MAX) NULL,
    MissingSkills       NVARCHAR(MAX) NULL,
    Suggestions         NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);
```

**Acceptance Criteria:**
- All 6 tables created with correct column types and constraints
- All CHECK constraints apply the correct allowed values
- All FKs specify the correct ON DELETE behavior

---

### FR-03-002: DML Exercises — INSERT Seed Data
**Description:** Trainee writes `INSERT` scripts to populate realistic test data.

**Required seed data:**
- 3 user accounts (1 active job seeker, 1 student, 1 admin)
- 10 applications per user across all 6 statuses
- At least 15 application events spread across applications
- 2 CV profiles
- 5 match results

---

### FR-03-003: SELECT Queries — Core Application Reports

**Query 1: All active applications for a user, sorted by deadline**
```sql
SELECT CompanyName, PositionTitle, Status, Deadline, FollowUpDate
FROM Applications
WHERE UserId = 1 AND IsArchived = 0
ORDER BY Deadline ASC;
```

**Query 2: Overdue applications**
```sql
SELECT CompanyName, PositionTitle, Deadline,
       DATEDIFF(DAY, Deadline, GETDATE()) AS DaysOverdue
FROM Applications
WHERE UserId = 1
  AND IsArchived = 0
  AND Deadline < CAST(GETDATE() AS DATE)
  AND Status NOT IN ('Offer', 'Rejected');
```

**Query 3: Application count by status (Dashboard)**
```sql
SELECT Status, COUNT(*) AS ApplicationCount
FROM Applications
WHERE UserId = 1 AND IsArchived = 0
GROUP BY Status
ORDER BY ApplicationCount DESC;
```

**Query 4: Response rate calculation**
```sql
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN Status NOT IN ('Saved', 'Applied') THEN 1 ELSE 0 END) AS Responded,
    CAST(
        SUM(CASE WHEN Status NOT IN ('Saved', 'Applied') THEN 1.0 ELSE 0 END)
        / NULLIF(COUNT(*), 0) * 100
    AS DECIMAL(5,2)) AS ResponseRate
FROM Applications
WHERE UserId = 1 AND IsArchived = 0;
```

**Query 5: Applications with their latest timeline event**
```sql
SELECT a.CompanyName, a.PositionTitle, a.Status,
       e.EventType AS LatestEvent, e.EventDate AS LatestEventDate
FROM Applications a
LEFT JOIN ApplicationEvents e
    ON e.ApplicationId = a.Id
    AND e.EventDate = (
        SELECT MAX(e2.EventDate)
        FROM ApplicationEvents e2
        WHERE e2.ApplicationId = a.Id
    )
WHERE a.UserId = 1 AND a.IsArchived = 0;
```

**Query 6: Upcoming follow-ups in the next 7 days**
```sql
SELECT CompanyName, PositionTitle, FollowUpDate,
       DATEDIFF(DAY, GETDATE(), FollowUpDate) AS DaysUntilFollowUp
FROM Applications
WHERE UserId = 1
  AND IsArchived = 0
  AND FollowUpDate BETWEEN CAST(GETDATE() AS DATE)
                       AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE));
```

**Acceptance Criteria:**
- All 6 queries return correct results against seed data
- Query results validated by comparing to expected output

---

### FR-03-004: UPDATE and DELETE Exercises

```sql
-- Archive all rejected applications older than 30 days
UPDATE Applications
SET IsArchived = 1, UpdatedAt = GETUTCDATE()
WHERE Status = 'Rejected'
  AND CreatedAt < DATEADD(DAY, -30, GETUTCDATE())
  AND IsArchived = 0;

-- Change application status (trainee must replicate the domain status rule in SQL)
UPDATE Applications
SET Status = 'Interview', UpdatedAt = GETUTCDATE()
WHERE Id = 5 AND UserId = 1 AND Status = 'Applied';
```

---

### FR-03-005: JOINS and Cross-Table Queries

**Query: User's CV profile joined with their match history**
```sql
SELECT
    up.Email,
    cp.Name AS CvName,
    cp.SkillsText,
    mr.MatchScore,
    mr.CreatedAt AS MatchedAt
FROM UserProfiles up
LEFT JOIN CvProfiles cp ON cp.UserId = up.Id
LEFT JOIN MatchResults mr ON mr.UserId = up.Id
WHERE up.Id = 1
ORDER BY mr.CreatedAt DESC;
```

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **DDL before ORM** | Write tables manually to understand what EF Core generates |
| **Defensive NULLs** | `NULLIF` in calculations, `LEFT JOIN` for optional data |
| **Status Filtering** | Always filter `IsArchived = 0` in user-facing queries |
| **Ownership Filter** | Always filter `UserId = @currentUserId` — never return another user's data |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-03-01 | All queries run without errors against seed data |
| NFR-03-02 | All monetary columns use `DECIMAL`, never `FLOAT` |
| NFR-03-03 | All string columns use `NVARCHAR`, never `VARCHAR` |
| NFR-03-04 | All PKs are named `Id` (IDENTITY 1,1) |
| NFR-03-05 | All queries include a `UserId` filter — no cross-user data leakage |
| NFR-03-06 | Parameterized queries only — no inline string concatenation |

---

## 7. Assessment & Completion Criteria

**Capstone Task:** Write and run 3 queries:
1. The dashboard response rate query for a specific user
2. All overdue applications for that user sorted by days overdue
3. The full timeline for a single application, ordered by event date

**Pass Criteria:**
- [ ] All 6 tables created correctly with DDL
- [ ] Seed data inserted successfully
- [ ] All 6 core queries produce correct output
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-02 completed | Prerequisite | Trainee |
| SQL Server 2022 / SSMS | Environment | IT |
| Seed data approved | Approval | Team Lead |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
