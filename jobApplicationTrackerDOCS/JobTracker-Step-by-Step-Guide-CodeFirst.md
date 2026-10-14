# Job Application Tracker — Step-by-Step Build Guide (Code First)

> **Approach:** Code-First with Migrations | **.NET 9.0** | **SQL Server** | **MVC Pattern (Web API)**
>
> This guide is an outline. When you get stuck on a step, paste the current step (and any error) to an AI and ask for specific help.

> **Current-source note:** This guide preserves the original detailed build sequence. Where an older exercise conflicts with the checked-in code, the current C# entities, controllers, EF configurations, migrations, and tests are authoritative. Historical exercises for features not present in the source are marked as proposed extensions below.

---

## Phase 1: Environment Check (Before You Write Code)

1. Open **Visual Studio 2022** — confirm .NET 9 SDK is available (Help → About)
2. Open **SQL Server Management Studio (SSMS)** — connect to your local SQL Server instance (usually `localhost` or `.\SQLEXPRESS`)
3. Confirm you can right-click **Databases** and see "New Database…" option

---

## Phase 2: Create the .NET Solution (BRD-01)

### Step 2.1 — Create Solution in Visual Studio

1. Open **Visual Studio 2022**
2. **Create a new project** → search **"Blank Solution"** → Next
3. Solution name: `JobTracker` → choose a folder → Create
4. You now have an empty `JobTracker.sln`

### Step 2.2 — Add Projects to the Solution

Right-click the Solution → **Add** → **New Project…** for each:

| # | Project Template | Name | Framework |
|---|---|---|---|
| 1 | **Class Library** | `JobTracker.Utilities` | .NET 9.0 |
| 2 | **Class Library** | `JobTracker.Domain` | .NET 9.0 |
| 3 | **Class Library** | `JobTracker.DataAccess` | .NET 9.0 |
| 4 | **Class Library** | `JobTracker.Business` | .NET 9.0 |
| 5 | **ASP.NET Core Web API** | `JobTracker.Api` | .NET 9.0 |
| 6 | **xUnit Test Project** | `JobTracker.Tests` | .NET 9.0 |

> When adding the Web API project, uncheck "Use controllers" if checked — we want controller-based, not minimal APIs. Make sure "Enable OpenAPI support" is checked.

### Step 2.3 — Set Project References

Right-click each project → **Add** → **Project Reference…**:

| Project | References |
|---|---|
| `JobTracker.Domain` | `JobTracker.Utilities` |
| `JobTracker.DataAccess` | `JobTracker.Domain` |
| `JobTracker.Business` | `JobTracker.Domain`, `JobTracker.DataAccess` |
| `JobTracker.Api` | `JobTracker.Business`, `JobTracker.Domain`, `JobTracker.Utilities` |
| `JobTracker.Tests` | `JobTracker.Api`, `JobTracker.Business`, `JobTracker.Domain`, `JobTracker.Utilities` |

### Step 2.4 — Build the Solution

1. **Build** → **Build Solution** (Ctrl+Shift+B)
2. Should show **0 errors** in the Output window

---

## Phase 3: JobTracker.Utilities (BRD-01)

### Step 3.1 — Create Utility Classes

1. In `JobTracker.Utilities`, delete the default `Class1.cs`
2. Add 4 new classes (right-click project → Add → Class):
   - `ApplicationCalculator.cs` — static class with: `CalculateResponseRate`, `DaysUntilDeadline`, `IsOverdue`, `GetFollowUpPriority`
   - `InputValidator.cs` — static class with: `IsValidEmail`, `IsValidUrl`, `IsValidCompanyName`, `IsValidDate`, `SanitizeString`
   - `StringFormatter.cs` — static class with: `FormatSalaryRange`, `FormatApplicationStatus`, `FormatDeadline`, `TruncateNotes`, `FormatResponseRate`
   - `ConsoleHelper.cs` — static class for a menu-driven test CLI

> **What to code:** Each method body should implement the logic described in BRD-01. Use guard clauses, throw `ArgumentException` for invalid inputs, and add XML doc comments (`///`).

---

## Phase 4: JobTracker.Domain — Entities First (BRD-02)

### Step 4.1 — Create Folder Structure

In `JobTracker.Domain`, create folders:
- `Common/` — `Applications/` — `Events/` — `Profile/` — `Matching/` — `Audit/`

### Step 4.2 — Create Interfaces (in `Common/`)

- `IAuditable.cs` — `ISoftDeletable.cs` — `IUserOwned.cs`

### Step 4.3 — Create Base Classes (in `Common/`)

- `Entity.cs` — abstract class with `[Key] Id` property only (identity base)
- `EntityAudit.cs` — abstract class extending `Entity`, implementing `IAuditable` (adds `CreatedAt`, `UpdatedAt`, `IsDeleted`)

> **Important:** `Entity` has `Id` only. `EntityAudit` extends `Entity` and adds audit fields. All entities that need audit fields extend `EntityAudit`; lightweight entities (like `ApplicationEvent`) extend `Entity` directly.

### Step 4.4 — Create Domain Entities

> **Reconciliation:** Include all domain properties — the EF Core migration will add the correct columns.

- `Applications/Application.cs` — sealed, extends **EntityAudit**, implements **IUserOwned**
  - Properties: `UserId` (FK), `CompanyName`, `PositionTitle`, `JobUrl`, `Location`, `EmploymentType` (enum), `SalaryMin`, `SalaryMax`, `Status` (ApplicationStatus enum), `Deadline`, `FollowUpDate`, `Notes`, `IsArchived`
  - Domain methods: `ChangeStatus(ApplicationStatus)`, `Archive()`, `IsOverdue()`
- `Applications/ApplicationStatus.cs` — enum: `Saved`, `Applied`, `OnlineAssessment`, `Interview`, `Offer`, `Rejected`
- `Applications/EmploymentType.cs` — enum: `FullTime`, `PartTime`, `Contract`, `Internship`, `Remote`
- `Events/ApplicationEvent.cs` — sealed, extends **Entity**
  - Properties: `ApplicationId` (FK), `EventType` (EventType enum), `EventDate`, `Notes`, `CreatedAt`
- `Events/EventType.cs` — enum: `ApplicationSubmitted`, `AssessmentCompleted`, `InterviewScheduled`, `FollowUpSent`, `OfferReceived`, `RejectionReceived`, `NoteAdded`
- `Profile/CvProfile.cs` — sealed, extends **Entity** (no soft delete on CV)
  - Properties: `UserId` (FK, UNIQUE), `Name`, `Summary`, `SkillsText`, `ExperienceText`, `EducationText`, `UpdatedAt`
- `Matching/MatchResult.cs` — sealed, extends **Entity** (append-only)
  - Properties: `UserId` (FK), `ApplicationId` (FK, nullable), `JobDescriptionText`, `MatchScore` (INT 0–100), `MatchingSkills`, `MissingSkills`, `Suggestions` (all NVARCHAR MAX JSON)
- `Audit/AuditLog.cs` — sealed, extends **Entity** (no FK constraints)
  - Properties: `TableName`, `RecordId`, `Action`, `ChangedBy`, `ChangedAt`, `OldValues`, `NewValues`
- `Profile/UserProfile.cs` — sealed, extends **EntityAudit** (stores login credentials)
  - Properties: `Email`, `PasswordHash`, `RefreshToken`, `RefreshTokenExpiryTime`, `IsActive`

> **Coding Standard:** All methods and properties use block bodies — no expression-bodied members (`=>`).

### Step 4.5 — Create DTOs and Command Records

Create the DTOs used by the Business services:
- `CreateApplicationDto`, `UpdateApplicationDto`, `ApplicationListDto`, `ApplicationDetailDto`
- `ChangeStatusDto`, `CreateEventDto`, `EventDto`
- `UpdateCvProfileDto`, `CvProfileDto`
- `MatchRequestDto`, `MatchResultDto`
- `DashboardSummaryDto`, `DeadlineItemDto`
- `SignupDto`, `LoginDto`, `AuthResponseDto`

---

## Phase 5: JobTracker.DataAccess — Code First with Migrations (BRD-05)

### Step 5.1 — Install NuGet Packages

In `JobTracker.DataAccess` → Manage NuGet Packages → install:
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`
- `Dapper`

Also install `Microsoft.EntityFrameworkCore.Design` in `JobTracker.Api`.

### Step 5.2 — Create the DbContext

1. Create folder `Context/` in `JobTracker.DataAccess`
2. Add `JobTrackerDbContext.cs`:
   - Add a `DbSet<>` property for every entity (6 total: `UserProfiles`, `Applications`, `ApplicationEvents`, `CvProfiles`, `MatchResults`, `AuditLogs`)
   - Override `OnModelCreating` to configure the entity mappings via Fluent API

### Step 5.3 — Configure Fluent API (in `OnModelCreating`)

Configure each entity via `IEntityTypeConfiguration<T>` classes in `Configuration/`. Key configurations:

**Applications:**
- Convert `ApplicationStatus` enum to `NVARCHAR(30)` via `.HasConversion<string>()`
- Convert `EmploymentType` enum to `NVARCHAR(30)` via `.HasConversion<string>()`
- `IsArchived` → `BIT DEFAULT 0`
- `CreatedAt` → `DEFAULT GETUTCDATE()`
- CHECK constraint on `Status` values
- CHECK constraint on `EmploymentType` values
- Global query filter: `.HasQueryFilter(a => !a.IsArchived)`

**CvProfiles:**
- `HasIndex(c => c.UserId).IsUnique()` — one profile per user
- `SkillsText` → `NOT NULL`

**MatchResults:**
- `MatchScore` → CHECK constraint `BETWEEN 0 AND 100`
- `ApplicationId` → nullable FK with `OnDelete(DeleteBehavior.SetNull)`
- `UserId` → non-nullable FK with `OnDelete(DeleteBehavior.NoAction)`

**Defaults:**
- `IsArchived DEFAULT false`, `IsActive DEFAULT true`
- `CreatedAt DEFAULT GETUTCDATE()`

**Relationships (match ERD FK summary):**
- `Applications.UserId` → `UserProfiles.Id` (CASCADE)
- `ApplicationEvents.ApplicationId` → `Applications.Id` (CASCADE)
- `CvProfiles.UserId` → `UserProfiles.Id` (CASCADE)
- `MatchResults.UserId` → `UserProfiles.Id` (NO ACTION)
- `MatchResults.ApplicationId` → `Applications.Id` (SET NULL)

**Indexes (11 from ERD):**
- `IX_UserProfiles_Email` (unique)
- `IX_Applications_UserId` (filtered: `IsArchived = 0`)
- `IX_Applications_Status` (composite: `UserId, Status`)
- `IX_Applications_Deadline` (composite: `UserId, Deadline`, filtered)
- `IX_Applications_FollowUp` (composite: `UserId, FollowUpDate`)
- `IX_Applications_Created`
- `IX_AppEvents_ApplicationId`
- `IX_AppEvents_EventDate` (composite: `ApplicationId, EventDate`)
- `IX_CvProfiles_UserId` (unique)
- `IX_MatchResults_UserId`
- `IX_AuditLogs_Table` (composite: `TableName, ChangedAt`)

**AuditLogs:**
- No FK constraints configured

### Step 5.4 — Create the Initial Migration

1. Open **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console)
2. Set **Default project** dropdown to `JobTracker.DataAccess`
3. Make sure `JobTracker.Api` is set as the **Startup Project** (right-click → Set as Startup Project)
4. Run:

```
Add-Migration InitialCreate -StartupProject JobTracker.Api
```

5. Review the generated migration file in `Migrations/` — verify it matches the ERD:
   - 6 tables with correct column names, types, and constraints
   - All FKs with correct ON DELETE behavior
   - `CvProfiles.UserId` unique constraint
   - `MatchResults.MatchScore` CHECK constraint
   - All indexes created

> **Tip:** If the migration doesn't match the ERD, adjust your Fluent API configuration and run `Remove-Migration` then `Add-Migration` again.

### Step 5.5 — Apply the Migration (Create the Database)

1. Add your connection string to `appsettings.json` in `JobTracker.Api`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=JobTracker_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

2. Register the DbContext in `Program.cs`:

```csharp
builder.Services.AddDbContext<JobTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

3. Run the migration:

```
Update-Database -StartupProject JobTracker.Api
```

4. Open SSMS → refresh → verify `JobTracker_DB` was created with all 6 tables

> **Important:** After applying the migration, check the table structure in SSMS. Every column, type, and constraint should match the ERD exactly. All string columns must be `NVARCHAR`. All PKs must be named `Id`.

### Step 5.6 — Historical SQL-object exercises (not current dependencies)

EF Core migrations handle the current tables, columns, indexes, and constraints. The following list is retained as an advanced SQL exercise from BRD-04; these objects are not required for the current API but add depth:

1. **Views** (3):
   - `vw_ActiveApplications`
   - `vw_DashboardSummary`
   - `vw_UpcomingDeadlines`

2. **Functions** (2):
   - `dbo.fn_GetResponseRate` (scalar)
   - `dbo.fn_GetStatusLabel` (scalar)

3. **Stored Procedures** (4):
   - `usp_GetUserDashboard`
   - `usp_ChangeApplicationStatus`
   - `usp_ArchiveOldRejections`
   - `usp_GetApplicationTimeline`

4. **Triggers** (1):
   - `trg_Applications_Audit` (AFTER UPDATE on Applications → inserts into AuditLogs)

> **Tip:** Implement these only as a deliberate feature extension, together with repository/service/controller code, tests, and updated schema documentation.

### Step 5.7 — Seed Data (via Migration or SQL)

Option A — **EF Core `HasData`** in `OnModelCreating`:
- 2 user accounts with BCrypt-hashed passwords
- 10 applications per user across all 6 statuses
- 15 application events
- 2 CV profiles
- 5 match results
- Run `Add-Migration SeedData` then `Update-Database`

Option B — **SQL INSERT scripts** in SSMS:
- Same data as above
- After seeding: run `SELECT * FROM Applications` to verify data

### Step 5.8 — Create Repository Layer

Create folders: `Repositories/Interfaces/`, `Repositories/Implementations/`, `UnitOfWork/`

Add:
- `IRepository.cs` — generic CRUD interface
- `IApplicationRepository.cs`, `IEventRepository.cs`, `ICvProfileRepository.cs`, `IMatchResultRepository.cs`, `IUserRepository.cs`
- `BaseRepository.cs` + concrete implementations
- `IDashboardRepository.cs` + `DashboardRepository.cs` (Dapper-based)
- `IUnitOfWork.cs` + `UnitOfWork.cs`

---

## Phase 6: JobTracker.Api — REST API (BRD-06)

### Step 6.1 — Install NuGet Packages

- `Swashbuckle.AspNetCore`
- `FluentValidation.AspNetCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`
- `BCrypt.Net-Next`

### Step 6.2 — Configure `appsettings.json`

Add `ConnectionStrings`, `Jwt`, and `Matching` sections (see BRD-06 FR-06-002).

### Step 6.3 — Configure `Program.cs`

Register: EF Core, Repositories, Services, JWT Auth, FluentValidation, Swagger, Health Checks, Middleware, `IHttpContextAccessor`.

### Step 6.4 — Create Common Classes

- `Common/ApiResponse.cs` — generic response wrapper
- `Common/PagedResult.cs` — paginated response
- `Middleware/GlobalExceptionMiddleware.cs`
- `Extensions/ClaimsPrincipalExtensions.cs` — `GetCurrentUserId()`

### Step 6.5 — Create Service Layer (in `JobTracker.Business`)

In `Services/`: `IAuthService`/`AuthService`, `IApplicationService`/`ApplicationService`, `IDashboardService`/`DashboardService`, `ICvProfileService`/`CvProfileService`, `IMatchingService`/`MatchingService`

In `Matching/`: `ILlmProvider`, `MockLlmProvider`, `OpenAiLlmProvider`

> **Note:** `AuthService` queries the `UserProfiles` table for login, password validation, and token management. Every `ApplicationService` method receives `userId` from the controller and filters all queries by it.

### Step 6.6 — Current Controllers (5 total)

`AuthController`, `ApplicationsController`, `DashboardController`, `CvProfileController`, `MatchingController`. Health check is built-in ASP.NET Core middleware.

### Step 6.7 — Current Validators

`SignupValidator`, `LoginValidator`, `CreateApplicationValidator`, `UpdateApplicationValidator`, `ChangeStatusValidator`, `AddEventValidator`, `UpdateCvProfileValidator`, `MatchRequestValidator`.

### Step 6.8 — Run and Test

F5 → Swagger UI → Authorize with JWT → test all 18 endpoints in order (signup → login → create applications → dashboard → cv profile → match).

---

## Phase 7: Testing (BRD-07)

### Step 7.1 — Install Test Packages

`Moq`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`

### Step 7.2 — Write focused unit tests

Priority order:
1. `ApplicationStatusTests` — all status transition rules
2. `ApplicationCalculatorTests` — response rate, overdue detection
3. `ApplicationServiceTests` — ownership enforcement, CRUD
4. `MatchingServiceTests` — mock provider, oversized input
5. `DashboardServiceTests` — response rate calculation
6. `ApplicationsControllerTests` (integration) — cross-user access returns 404

### Step 7.3 — Run the tests (Ctrl+R, A)

---

## Quick Reference

| What | Where | Command |
|---|---|---|
| Add Migration | PMC | `Add-Migration MigrationName -StartupProject JobTracker.Api` |
| Apply Migration | PMC | `Update-Database -StartupProject JobTracker.Api` |
| Remove Last Migration | PMC | `Remove-Migration -StartupProject JobTracker.Api` |
| List Migrations | PMC | `Get-Migration -StartupProject JobTracker.Api` |
| Generate SQL Script | PMC | `Script-Migration -StartupProject JobTracker.Api` |
| Build | VS | Ctrl+Shift+B |
| Run | VS | F5 |
| Run Tests | VS | Ctrl+R, A |

---

## Key Differences from SmartHR

| Aspect | SmartHR | Job Application Tracker |
|---|---|---|
| **Primary User** | HR departments, companies | Individual job seekers / students |
| **Entity Focus** | Employees, Departments, Projects | Applications, Events, CV Profiles |
| **Auth Model** | Role-based (HR, Finance, Manager, Admin) | Ownership-based (every user sees only their own data) |
| **Database Size** | 12 tables | 6 tables |
| **AI Feature** | None | LLM CV-to-job matching |
| **Kanban** | No | Yes (application status workflow) |
| **Key Security Rule** | Role policies per endpoint | `UserId` ownership filter on every query |
| **TPH Pattern** | FullTimeEmployee / ContractEmployee | Not used |

---

> **When stuck:** Copy the phase/step you're on + error message → paste to AI → ask for help.
