# Job Tracker

Job Tracker is split by vertical slice: each person owns complete modules across
the backend and frontend. No work is divided into a backend-only stream and a
frontend-only stream.

## Work split

| Owner | Modules | Backend ownership | Frontend ownership |
| --- | --- | --- | --- |
| Person A | Auth & Security, CV Profile, Matching (LLM), Audit & Infrastructure | JWT setup, `ApiResponse<T>`, global exception middleware, `UserProfiles`, `CvProfiles`, `MatchResults`, and `AuditLogs` | Login/signup, auth guard and interceptor, CV page, and match page |
| Person B | Application Management & Kanban, Dashboard & Metrics | Application and event entities, status transition rules, and CSV export | Kanban board, application forms, and dashboard page |

Person A owns the shared foundation. Person B may use a temporary hardcoded
`UserId` in `GetCurrentUserId()` until Person A's signup and login flow is
merged; replace that value immediately after authentication is available.

## Shared contracts

- Database ownership:
  - Person A: `UserProfiles`, `CvProfiles`, `MatchResults`, `AuditLogs`
  - Person B: `Applications`, `ApplicationEvents`
- Response envelope:

  ```json
  {
    "success": true,
    "data": {},
    "message": null,
    "errors": []
  }
  ```

- Endpoint routes must match the approved module blueprint exactly.
- Only one person creates an EF Core migration at a time. Pull `main` first,
  create the migration, and push it immediately.
- Shared files such as `Program.cs`, `AppDbContext.cs`, and
  `app.routes.ts` require coordination before editing.

## Repository structure

```text
backend/
frontend/
```

The application projects will be scaffolded inside these folders after the
initial repository push.

## Branch workflow

Protect `main` and require pull requests before merging. Use one feature branch
per vertical slice:

- Person A: `feature/auth`, `feature/cv-profile`, `feature/matching`
- Person B: `feature/applications`, `feature/kanban-ui`, `feature/dashboard`

Before starting new work:

```powershell
git checkout main
git pull
git checkout -b feature/<slice-name>
```

Commit small, focused changes and open a pull request for review.
