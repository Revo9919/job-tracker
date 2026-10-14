# Business Requirements Document
## BRD-07: Full Integration — Testing & Deployment

> **Current implementation note:** This module covers end-to-end integration, automated testing, and deployment preparation for the Job Application Tracker. The current test project contains focused unit tests; the 50+ test target and integration-test examples remain future expansion work.

| Field             | Details                                              |
|-------------------|------------------------------------------------------|
| **Document ID**   | BRD-07                                               |
| **Module**        | 7 of 7 — Full Integration & Testing                  |
| **Version**       | 1.0                                                  |
| **Status**        | Ready for Implementation                             |
| **Date**          | 2026-10-14                                           |
| **Audience**      | On-the-job developers (interns / team transitions)   |
| **Prerequisites** | BRD-06 completed (full API layer)                    |
| **Est. Duration** | 4–6 days                                             |

---

## 🎯 Project: Job Application Tracker — Internship & Career Management System

> This final module brings everything together — automated tests, integration verification, CI/CD pipeline setup, and demo preparation.

**Module Contribution:** `JobTracker.Tests` — xUnit test project + deployment-ready build

---

## 📋 Definition of Done

The project is ready to present when:

- [ ] A user can sign up, log in, log out, and access only their own data
- [ ] A user can create, edit, archive, search, filter, and inspect applications
- [ ] The Kanban board displays all statuses and supports status changes on desktop and mobile
- [ ] Deadlines and follow-up dates are clearly surfaced
- [ ] Dashboard counts and response rate match the underlying application records
- [ ] A user can create a CV profile and compare it with a pasted job description
- [ ] Matching returns a score, matching skills, missing skills, and suggestions
- [ ] The matching UI works with both a real provider configuration and the mock provider
- [ ] LLM failures, invalid responses, oversized input, and timeouts show clear user-facing errors
- [ ] Backend authorization prevents cross-user access
- [ ] Backend tests cover status rules, ownership, dashboard calculations, and matching fallback
- [ ] Frontend tests cover forms, filters, status changes, dashboard states, and matching results
- [ ] Database migrations work from a clean local database
- [ ] The Angular production build and .NET build/test commands pass
- [ ] The application is usable with keyboard navigation and at mobile widths
- [ ] The README explains setup, environment variables, API behavior, and the demo flow

---

## 1. Module Overview

Trainees validate the complete Job Application Tracker by writing automated tests, running integration scenarios, and preparing the application for a live demo. This module also introduces CI/CD concepts and deployment readiness.

### 1.1 Business Justification

Untested applications fail in demos and in production. Automated tests catch regressions when the team iterates on features. The mock LLM provider is essential for CI/CD — it allows the matching feature to be tested without spending API credits or requiring a live provider connection.

---

## 2. Learning Objectives

By the end of this module, the trainee will be able to:

- [ ] Write xUnit unit tests with `[Fact]` and `[Theory]`
- [ ] Use Moq to mock service dependencies
- [ ] Use FluentAssertions for readable assertions
- [ ] Use `Microsoft.EntityFrameworkCore.InMemory` for repository tests
- [ ] Write integration tests with `WebApplicationFactory<T>`
- [ ] Use `Microsoft.AspNetCore.Mvc.Testing` for end-to-end HTTP testing
- [ ] Test JWT-protected endpoints with a test-scoped authentication token
- [ ] Understand test coverage and identify untested code paths
- [ ] Configure a basic CI build pipeline (GitHub Actions or Azure DevOps)
- [ ] Prepare a production build and verify migration scripts

---

## 3. Module Contribution to JobTracker

**This module delivers:**
- Complete unit test suite covering domain rules, service logic, and matching behavior
- Integration tests for all 18 API endpoints
- CI/CD pipeline configuration (`.github/workflows/build.yml` or equivalent)
- Deployment checklist and README

---

## 4. Functional Requirements

### FR-07-001: Install Test Packages

In `JobTracker.Tests`:
- `Moq`
- `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.EntityFrameworkCore.InMemory`
- `xunit`
- `xunit.runner.visualstudio`

---

### FR-07-002: Unit Tests — Domain Layer (Application Entity)

```csharp
// ApplicationStatusTests.cs

[Fact]
public void ChangeStatus_FromSaved_ToApplied_Succeeds()
{
    // Arrange
    var application = new Application { Status = ApplicationStatus.Saved };

    // Act
    application.ChangeStatus(ApplicationStatus.Applied);

    // Assert
    application.Status.Should().Be(ApplicationStatus.Applied);
}

[Fact]
public void ChangeStatus_FromRejected_ToOffer_ThrowsInvalidOperationException()
{
    // Arrange
    var application = new Application { Status = ApplicationStatus.Rejected };

    // Act
    Action act = () => application.ChangeStatus(ApplicationStatus.Offer);

    // Assert
    act.Should().Throw<InvalidOperationException>()
       .WithMessage("*Invalid status transition*");
}

[Theory]
[InlineData(ApplicationStatus.Offer)]
[InlineData(ApplicationStatus.Rejected)]
public void ChangeStatus_FromTerminalStatus_AlwaysThrows(ApplicationStatus terminal)
{
    var application = new Application { Status = terminal };
    Action act = () => application.ChangeStatus(ApplicationStatus.Applied);
    act.Should().Throw<InvalidOperationException>();
}

[Fact]
public void IsOverdue_DeadlineInPast_ActiveStatus_ReturnsTrue()
{
    var application = new Application
    {
        Status   = ApplicationStatus.Applied,
        Deadline = DateTime.Today.AddDays(-3)
    };
    application.IsOverdue().Should().BeTrue();
}

[Fact]
public void IsOverdue_StatusOffer_ReturnsFalse_Regardless()
{
    var application = new Application
    {
        Status   = ApplicationStatus.Offer,
        Deadline = DateTime.Today.AddDays(-10)
    };
    application.IsOverdue().Should().BeFalse();
}
```

---

### FR-07-003: Unit Tests — Utility Classes

```csharp
// ApplicationCalculatorTests.cs

[Theory]
[InlineData(10, 4, 40.0)]
[InlineData(0, 0, 0.0)]
[InlineData(20, 20, 100.0)]
public void CalculateResponseRate_ReturnsCorrectPercentage(
    int total, int responded, decimal expectedRate)
{
    var result = ApplicationCalculator.CalculateResponseRate(total, responded);
    result.Should().Be(expectedRate);
}

[Fact]
public void CalculateResponseRate_NegativeTotal_ThrowsArgumentException()
{
    Action act = () => ApplicationCalculator.CalculateResponseRate(-1, 0);
    act.Should().Throw<ArgumentException>();
}

[Fact]
public void IsOverdue_PastDate_ReturnsTrue()
{
    ApplicationCalculator.IsOverdue(DateTime.Today.AddDays(-1)).Should().BeTrue();
}

[Fact]
public void IsOverdue_FutureDate_ReturnsFalse()
{
    ApplicationCalculator.IsOverdue(DateTime.Today.AddDays(5)).Should().BeFalse();
}
```

---

### FR-07-004: Unit Tests — Service Layer (ApplicationService)

```csharp
// ApplicationServiceTests.cs

public class ApplicationServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly IApplicationService _service;

    public ApplicationServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _service = new ApplicationService(_uowMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotOwnedByUser_ThrowsNotFoundException()
    {
        // Arrange
        _uowMock.Setup(u => u.Applications.GetByIdAndUserIdAsync(1, 99))
                .ReturnsAsync((Application?)null);

        // Act
        Func<Task> act = async () => await _service.GetByIdAsync(1, userId: 99);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidTransition_ReturnsUpdatedDto()
    {
        // Arrange
        var app = new Application { Id = 1, UserId = 1, Status = ApplicationStatus.Applied };
        _uowMock.Setup(u => u.Applications.GetByIdAndUserIdAsync(1, 1)).ReturnsAsync(app);

        // Act
        var result = await _service.ChangeStatusAsync(1, 1, ApplicationStatus.Interview, null);

        // Assert
        result.NewStatus.Should().Be("Interview");
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
```

---

### FR-07-005: Unit Tests — Matching Service (Mock Provider)

```csharp
// MatchingServiceTests.cs

public class MatchingServiceTests
{
    [Fact]
    public async Task MatchAsync_MockProvider_ReturnsStructuredResult()
    {
        // Arrange
        var mockProvider = new MockLlmProvider();
        var service = new MatchingService(mockProvider, /* repos */);

        // Act
        var result = await service.MatchAsync(
            userId: 1,
            jobDescription: "Looking for a TypeScript developer...",
            cvProfileId: 1);

        // Assert
        result.MatchScore.Should().BeInRange(0, 100);
        result.MatchingSkills.Should().NotBeNull();
        result.MissingSkills.Should().NotBeNull();
        result.Suggestions.Should().NotBeNull();
    }

    [Fact]
    public async Task MatchAsync_OversizedInput_ThrowsValidationException()
    {
        var service = new MatchingService(new MockLlmProvider(), /* repos */);
        var hugeInput = new string('x', 9000);

        Func<Task> act = async () => await service.MatchAsync(1, hugeInput, 1);

        await act.Should().ThrowAsync<ValidationException>()
                 .WithMessage("*exceeds*");
    }
}
```

---

### FR-07-006: Unit Tests — Dashboard Service

```csharp
// DashboardServiceTests.cs

[Fact]
public async Task GetSummaryAsync_ReturnsCorrectResponseRate()
{
    // Arrange — 10 total, 8 responded (not Saved)
    var mockRepo = new Mock<IDashboardRepository>();
    mockRepo.Setup(r => r.GetSummaryAsync(1))
            .ReturnsAsync(new DashboardSummaryDto
            {
                TotalApplications = 10,
                SavedCount = 2,
                AppliedCount = 3,
                InterviewCount = 3,
                OfferCount = 1,
                RejectedCount = 1
            });
    var service = new DashboardService(mockRepo.Object);

    // Act
    var result = await service.GetSummaryAsync(1);

    // Assert
    result.ResponseRate.Should().Be(80.0m);  // 8/10 × 100
}
```

---

### FR-07-007: Integration Tests — HTTP Layer

```csharp
// ApplicationsControllerTests.cs

public class ApplicationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApplicationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real DB with InMemory
                // Replace real LLM provider with mock
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetApplications_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/applications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApplication_ValidBody_Returns201()
    {
        // Arrange
        await AuthenticateAsync(_client);
        var body = new CreateApplicationDto
        {
            CompanyName   = "Acme Corp",
            PositionTitle = "Junior Developer",
            Status        = ApplicationStatus.Saved
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ApplicationDetailDto>>();
        result!.Data!.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetApplicationById_OtherUsersApplication_Returns404()
    {
        // Arrange — create app as user1, try to get it as user2
        await AuthenticateAsUser2Async(_client);

        // Act
        var response = await _client.GetAsync("/api/applications/1");

        // Assert — 404, not 403, to avoid leaking existence information
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

---

### FR-07-008: 20-Day Build Schedule

| Days | Intern 1 (Applications & Kanban) | Intern 2 (Dashboard, Profile & Matching) | Shared Outcome |
|---|---|---|---|
| 1–2 | Define `Application` domain, statuses, DTOs, service interfaces | Define dashboard metrics, `CvProfile`, and matching contracts | Agree on architecture, auth, API errors, and DB relationships |
| 3–4 | Implement application persistence, CRUD endpoints, seed data | Implement auth/profile foundations and dashboard query models | Run migrations, verify contracts via Swagger |
| 5–6 | Build Angular application models, service, route, and list shell | Build dashboard route, profile model/service, and login integration | Confirm protected routes and real API loading |
| 7–8 | Build Kanban columns, application cards, and details view | Build dashboard cards, status breakdown, and upcoming deadlines | Demonstrate logged-in user viewing real application data |
| 9–10 | Add create/edit form, validation, search, and filters | Add CV profile form and save/load behavior | Demonstrate adding an application and seeing updated metrics |
| 11–12 | Add status changes and application timeline/events | Add mock matching provider and matching endpoint | Verify status transitions and matching response shape |
| 13–14 | Add deadline highlighting, follow-up actions, and mobile status menu | Build job description form and matching result view | Demonstrate complete application-to-matching workflow |
| 15–16 | Write/fix application, filter, and Kanban tests | Write/fix dashboard, profile, and matching tests | Run backend and frontend test suites |
| 17 | Test dashboard and matching features using Intern 1's data | Test Kanban and application features using Intern 2's metrics | Cross-review and resolve integration defects |
| 18 | Responsive and accessibility pass on applications/Kanban | Responsive and accessibility pass on dashboard/matching | Check keyboard nav, contrast, loading, empty, and error states |
| 19 | Fix high-priority bugs and prepare application demo data | Fix bugs, configure provider/mock environments | Production build, migrations, smoke tests |
| 20 | Final review, README, and presentation flow | Final review, README, and presentation flow | Deploy, rehearse, and demonstrate complete user journey |

---

### FR-07-009: CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/build.yml
name: Build and Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node 22
        uses: actions/setup-node@v4
        with:
          node-version: '22'
      - name: Install dependencies
        run: npm ci
      - name: Build
        run: npm run build
      - name: Test
        run: npm test -- --watch=false --browsers=ChromeHeadless
```

---

### FR-07-010: Risk Register

| Risk | Control |
|---|---|
| LLM provider setup takes too long | Build the mock provider first; treat the real provider as a drop-in adapter |
| Kanban drag-and-drop consumes time | Implement status menus first; add drag-and-drop only after the workflow works |
| Dashboard calculations disagree with the UI | Keep calculations in one backend service and test known datasets |
| Job descriptions contain very large text | Apply request-size limits (8000 chars) and show a validation message |
| Users can access another user's applications | Apply ownership filters in every service method and write integration tests that verify this |
| Scope expands into a job board | Keep the product focused on tracking personally added applications |

---

## 5. Architecture & Design Patterns

| Pattern | Application |
|---|---|
| **Test Pyramid** | Many unit tests, fewer integration tests, smoke tests for deployment |
| **Mock Provider** | `MockLlmProvider` used in all tests — no external API credits consumed |
| **In-Memory DB** | `InMemoryDbContext` for repository and integration tests |
| **Given-When-Then** | All test methods follow arrange/act/assert structure |
| **Ownership Tests** | Dedicated tests verify cross-user data access returns 404, not 403 |

---

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-07-01 | All tests must pass in CI without a live database or LLM provider |
| NFR-07-02 | Mock provider must be deterministic — same input always returns same output |
| NFR-07-03 | Ownership violation tests must explicitly verify 404 (not 403) is returned |
| NFR-07-04 | Production build (both `dotnet build` and `ng build`) must produce zero errors |
| NFR-07-05 | README must include setup steps, required environment variables, and demo flow |
| NFR-07-06 | Migration script must work from a clean (empty) SQL Server database |

---

## 7. Assessment & Completion Criteria

**Capstone Task (Full Demo Flow):**
1. Start from a fresh database — run migrations
2. Sign up as a new user
3. Create 5 applications in at least 4 different statuses
4. Verify dashboard counts match the applications
5. Change one application status — verify timeline event was recorded
6. Create a CV profile and run a match — verify result structure
7. Run test suite — all tests pass

**Pass Criteria:**
- [ ] All domain status transition tests pass
- [ ] All ownership integration tests pass (cross-user access returns 404)
- [ ] Matching tests pass with mock provider
- [ ] Dashboard calculation tests pass
- [ ] CI pipeline passes on a clean run
- [ ] Code reviewed and approved by senior developer

---

## 8. Dependencies

| Dependency | Type | Owner |
|---|---|---|
| BRD-06 completed | Prerequisite | Trainee |
| GitHub repository with Actions enabled | Infrastructure | Team Lead |
| All 18 endpoints functional | Prerequisite | Trainee |

---

*Document Owner: Training Lead | Next Review: After first cohort completion*
