# Business Requirements Document
## BRD-05: ADO.NET & Entity Framework Core

> **Current implementation note:** This document provides the data-access layer curriculum for the Job Application Tracker. The Code-First approach with EF Core migrations is the production path. ADO.NET / Dapper exercises are included for learning purposes and for high-performance report queries.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-05                                               |
| **Module**        | 5 of 7 — ADO.NET & Entity Framework Core             |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-04 completed and signed off                      |
| **Est. Duration** | 5–7 days                                             |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This module builds the **Data Access Layer** — the bridge between domain entities and SQL Server. It implements the EF Core DbContext, Fluent API configuration, migrations, repository pattern, and Unit of Work.

**Module Contribution:** `JobTracker.DataAccess` class library — the full persistence layer.

---

## 📋 APIs Supported by this Module

Every API endpoint ultimately calls through this layer:

| # | API | Data Access Component |
|---|---|---|
| 4–11 | Application CRUD | `IApplicationRepository` / `ApplicationRepository` |
| 12–13 | Timeline events | `IEventRepository` / `EventRepository` |
| 14 | Dashboard summary | `IDashboardRepository` / `DashboardRepository` (Dapper) |
| 15–16 | CV Profile | `ICvProfileRepository` / `CvProfileRepository` |
| 17 | Match results | `IMatchResultRepository` / `MatchResultRepository` |
| 1–3 | Auth | `IUserRepository` / `UserRepository` |

---

## 1. Module Overview

Trainees implement the complete data-access layer using EF Core Code-First. They configure the `DbContext`, write Fluent API mappings that mirror the BRD-03 DDL, create and apply migrations, and then implement the repository pattern on top of the `DbContext`.

### 1.1 Business Justification

A clean data-access layer prevents SQL from leaking into controllers and services. The repository pattern enables unit testing with mock data — critical for testing ownership enforcement, status transition rules, and dashboard calculations without a live database.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Configure a `DbContext` with a `DbSet<T>` for every entity
- [ ] Use `OnModelCreating` and Fluent API for entity mapping
- [ ] Apply all common Fluent API methods: `HasKey`, `HasIndex`, `HasOne`, `HasMany`, `WithMany`, `Property`, `HasConversion`, `HasDefaultValue`, `HasCheckConstraint`, `HasQueryFilter`
- [ ] Create and apply EF Core migrations using Package Manager Console
- [ ] Implement the generic `IRepository<T>` interface
- [ ] Build concrete repositories with LINQ queries and EF Core includes
- [ ] Use Dapper for high-performance direct SQL queries (dashboard)
- [ ] Configure the Unit of Work pattern
- [ ] Understand and configure EF Core global query filters (ownership + soft delete)
- [ ] Seed data using `HasData` in `OnModelCreating`

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- `JobTrackerDbContext` with 6 `DbSet<T>` properties
- Full Fluent API configuration mirroring BRD-03 DDL
- Initial migration and seed data migration
- Repository interfaces and implementations
- Unit of Work
- Dapper-based dashboard repository

---

## 4. Functional Requirements

### FR-05-001: Install NuGet Packages

In `JobTracker.DataAccess` → Manage NuGet Packages → install:
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`
- `Dapper`

Also install `Microsoft.EntityFrameworkCore.Design` in `JobTracker.Api`.

---

### FR-05-002: Create the DbContext

Create `Context/JobTrackerDbContext.cs`:

```csharp
public class JobTrackerDbContext : DbContext
{
    public JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
        : base(options) { }

    public DbSet<UserProfile>      UserProfiles      { get; set; }
    public DbSet<Application>      Applications      { get; set; }
    public DbSet<ApplicationEvent> ApplicationEvents { get; set; }
    public DbSet<CvProfile>        CvProfiles        { get; set; }
    public DbSet<MatchResult>      MatchResults      { get; set; }
    public DbSet<AuditLog>         AuditLogs         { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply entity configurations
    }
}
```

---

### FR-05-003: Fluent API Configuration

Configure each entity via separate `IEntityTypeConfiguration<T>` classes in `Configuration/`:

#### `UserProfileConfiguration`
```csharp
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(150);
        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("IX_UserProfiles_Email");
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(u => u.RefreshToken).HasMaxLength(500);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}
```

#### `ApplicationConfiguration`
Key configurations:
- `Status` → `NVARCHAR(30)` with `HasConversion<string>()` for the enum
- `EmploymentType` → `NVARCHAR(30)` with `HasConversion<string>()`
- `CHECK (Status IN ('Saved','Applied','OnlineAssessment','Interview','Offer','Rejected'))`
- `CHECK (EmploymentType IN ('FullTime','PartTime','Contract','Internship','Remote'))`
- `SalaryMin`, `SalaryMax` → `DECIMAL(12,2)` nullable
- `IsArchived` → `BIT DEFAULT 0`
- `CreatedAt` → `DEFAULT GETUTCDATE()`
- Indexes: `IX_Applications_UserId` (filtered: `IsArchived=0`), `IX_Applications_Status`, `IX_Applications_Deadline`, `IX_Applications_FollowUp`
- Relationship: `HasOne<UserProfile>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade)`

**Global Query Filters (CRITICAL — ownership + soft delete):**
```csharp
// Applied in OnModelCreating:
modelBuilder.Entity<Application>()
    .HasQueryFilter(a => !a.IsArchived);
```

> ⚠️ **Ownership filtering is NOT done via global query filter** — it is enforced in service methods so that the `@UserId` is always explicitly required in every query. Global filter only applies `IsArchived = 0`.

#### `ApplicationEventConfiguration`
- `EventType` → `NVARCHAR(50)` with CHECK constraint
- `EventDate` → `DATETIME2 NOT NULL`
- Cascade delete when parent Application is deleted
- Indexes: `IX_AppEvents_ApplicationId`, `IX_AppEvents_EventDate` (composite: `ApplicationId, EventDate`)

#### `CvProfileConfiguration`
- `UserId` unique index (one CV per user): `HasIndex(c => c.UserId).IsUnique()`
- `SkillsText` → `NVARCHAR(MAX) NOT NULL`
- Cascade delete when UserProfile is deleted

#### `MatchResultConfiguration`
- `MatchScore` → `INT` with `HasCheckConstraint("CK_MatchResults_Score", "MatchScore BETWEEN 0 AND 100")`
- `ApplicationId` nullable FK → `Applications.Id` ON DELETE SET NULL
- `UserId` non-nullable FK → `UserProfiles.Id` ON DELETE NO ACTION
- `MatchingSkills`, `MissingSkills`, `Suggestions` → `NVARCHAR(MAX)` (stored as JSON)

#### `AuditLogConfiguration`
- No FK constraints (append-only — must survive source record deletion)
- `Action` → CHECK constraint: `IN ('INSERT', 'UPDATE', 'DELETE')`
- Composite index: `IX_AuditLogs_Table` on `(TableName, ChangedAt)`

---

### FR-05-004: Create the Initial Migration

1. Open **Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console)
2. Set **Default project** to `JobTracker.DataAccess`
3. Set `JobTracker.Api` as **Startup Project**
4. Run:

```
Add-Migration InitialCreate -StartupProject JobTracker.Api
```

5. Review the generated migration — verify:
   - 6 tables with correct column names, types, and constraints
   - All CHECK constraints for `Status`, `EmploymentType`, `EventType`, `MatchScore`
   - All FK relationships with correct `ON DELETE` behavior
   - `CvProfiles.UserId` has a unique constraint
   - All indexes are created

> **Tip:** If the migration doesn't match BRD-03 DDL, adjust Fluent API configuration and run `Remove-Migration` then `Add-Migration` again.

---

### FR-05-005: Apply the Migration

1. Add connection string to `appsettings.json` in `JobTracker.Api`:

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

4. Open SSMS → verify `JobTracker_DB` was created with all 6 tables.

---

### FR-05-006: Seed Data (via Migration)

Use EF Core `HasData` in `OnModelCreating` to seed:
- 2 user accounts (1 active job seeker, 1 student) with BCrypt-hashed passwords
- 10 applications per user across all 6 statuses
- 15 application events
- 2 CV profiles
- 5 match results

Run:
```
Add-Migration SeedData -StartupProject JobTracker.Api
Update-Database -StartupProject JobTracker.Api
```

---

### FR-05-007: Repository Layer

Create folders: `Repositories/Interfaces/`, `Repositories/Implementations/`, `UnitOfWork/`

**Generic Interface:**
```csharp
// Repositories/Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

**Domain-Specific Interfaces:**
```csharp
// IApplicationRepository — adds user-scoped queries
public interface IApplicationRepository : IRepository<Application>
{
    Task<IEnumerable<Application>> GetByUserIdAsync(int userId, ApplicationStatus? status);
    Task<Application?> GetByIdAndUserIdAsync(int id, int userId);
    Task<IEnumerable<Application>> SearchAsync(int userId, string searchTerm);
    Task<bool> ExistsForUserAsync(int id, int userId);
}

// IUserRepository — used by Auth service
public interface IUserRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByEmailAsync(string email);
    Task<UserProfile?> GetByRefreshTokenAsync(string token);
}

// ICvProfileRepository — one profile per user
public interface ICvProfileRepository : IRepository<CvProfile>
{
    Task<CvProfile?> GetByUserIdAsync(int userId);
}

// IMatchResultRepository
// IEventRepository
// IDashboardRepository (Dapper-based — no generic base)
```

**Dashboard Repository (Dapper):**
```csharp
// Repositories/Implementations/DashboardRepository.cs
public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        // Execute usp_GetUserDashboard via Dapper
        var result = await connection.QueryFirstOrDefaultAsync<DashboardSummaryDto>(
            "usp_GetUserDashboard",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
        return result ?? new DashboardSummaryDto();
    }
}
```

---

### FR-05-008: Unit of Work

```csharp
// UnitOfWork/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IApplicationRepository Applications { get; }
    IEventRepository       Events       { get; }
    ICvProfileRepository   CvProfiles   { get; }
    IMatchResultRepository MatchResults { get; }
    IUserRepository        Users        { get; }
    Task<int> SaveChangesAsync();
}
```

---

## 5. EF Core Migration Quick Reference

| What | Where | Command |
|---|---|---|
| Add Migration | PMC | `Add-Migration MigrationName -StartupProject JobTracker.Api` |
| Apply Migration | PMC | `Update-Database -StartupProject JobTracker.Api` |
| Remove Last Migration | PMC | `Remove-Migration -StartupProject JobTracker.Api` |
| List Migrations | PMC | `Get-Migration -StartupProject JobTracker.Api` |
| Generate SQL Script | PMC | `Script-Migration -StartupProject JobTracker.Api` |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-05-01 | Every application repository method must include `UserId` filter — no cross-user data access |
| NFR-05-02 | `IsArchived = 0` enforced via global query filter on `Application` |
| NFR-05-03 | All monetary columns use `DECIMAL(12,2)` |
| NFR-05-04 | All string columns use `NVARCHAR` |
| NFR-05-05 | `AuditLogs` has no FK constraints in EF Core config |
| NFR-05-06 | All async methods use `Async` suffix and `await` |
| NFR-05-07 | No expression-bodied members (`=>`) — block bodies only |

---

## 7. Assessment & Completion Criteria

**Capstone Task:**
1. Create an application via `IApplicationRepository`
2. Change its status via `usp_ChangeApplicationStatus`
3. Retrieve the dashboard summary via `DashboardRepository` (Dapper)
4. Verify all 3 operations produce correct results and the audit log has a new row

**Pass Criteria:**
- [ ] Migration applied successfully — 6 tables created in SSMS
- [ ] All repositories implemented with user-ownership enforcement
- [ ] Dapper dashboard repository returns correct aggregated data
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-04 completed | Prerequisite | Trainee |
| SQL Server 2022 | Environment | IT |
| NuGet access | Network | IT |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
