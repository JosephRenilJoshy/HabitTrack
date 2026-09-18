# HabitTrack

A microservices-based habit tracking application built with ASP.NET Core, designed as a
hands-on learning project covering .NET Core internals (middleware, filters, dependency
injection), EF Core in depth, JWT-based authentication across services, and real
service-to-service communication patterns.

This project intentionally goes beyond CRUD — the goal was to build, by hand, the kind of
infrastructure most jobs hand you pre-configured (middleware pipelines, custom filters,
authentication/authorization plumbing) and to get real depth with EF Core (relationships,
disconnected entities, N+1 diagnosis and fixes, transactions).

---

## Architecture

Three independent microservices, each with its own database, communicating over HTTP:

```
┌─────────────────┐        ┌──────────────────┐        ┌───────────────────────┐
│   User Service   │        │  Habit Service    │        │ Notification Service  │
│                  │        │                   │        │                       │
│ - Register/Login │        │ - Habit CRUD      │        │ - Background at-risk  │
│ - JWT issuance   │◄──────►│ - Check-ins       │◄──────►│   check (scheduled)   │
│ - User profile   │  JWT   │ - Streak calc     │  HTTP  │ - Milestone receiver  │
│                  │ trust  │ - Repository      │  calls │ - Notification store  │
│  UserServiceDb   │        │   pattern         │        │                       │
└─────────────────┘        │  HabitServiceDb   │        │ NotificationServiceDb │
                            └──────────────────┘        └───────────────────────┘
```

- **User Service** issues JWTs on login. **Habit Service** and **Notification Service**
  independently validate those tokens (same signing key/issuer/audience) — no service calls
  back to User Service to check a token. This is the core benefit of JWT in a microservices
  setup: stateless, verifiable trust without a network round-trip per request.
- **Each service owns its own SQL Server database** — no shared schema, no cross-database
  foreign keys. Referential integrity across services (e.g. a habit's `UserId`) is enforced
  by trust in a validated JWT claim, not by a database constraint.
- **Service-to-service calls** use `HttpClient` via `IHttpClientFactory`, protected by a
  shared internal API key (`X-Internal-Api-Key`) — a distinct authorization mechanism from
  user-facing JWT auth, used specifically for machine-to-machine calls that have no user
  identity behind them.

---

## Services

### User Service
- Register, login, JWT issuance (HMAC-SHA256, `System.IdentityModel.Tokens.Jwt`)
- Full CRUD on user profiles, with a custom **ownership authorization policy**
  (`SameUserOnly`) — a user can only update/delete their own account
- Password hashing via BCrypt
- Direct EF Core `DbContext` access (no repository layer) — deliberate choice to maximize
  hands-on EF Core exposure

### Habit Service
- Habit CRUD, check-ins (including **backdated/retroactive check-ins**)
- **Streak calculation** for two frequency types:
  - Daily habits — consecutive-day streaks, using `DateOnly.DayNumber` for safe date-gap math
  - Weekly-target habits — rolling ISO-week bucketing, evaluating whether each week met a
    configurable target count
- **Repository pattern** (`IHabitRepository`) — contrasts with User Service's direct-DbContext
  approach, demonstrating both patterns deliberately
- Milestone detection (7/30/100-day streaks) with a service-to-service notification call
- In-memory caching (Resource Filter) on the streak endpoint, with cache invalidation on
  new check-ins

### Notification Service
- **Scheduled background job** (`BackgroundService`) checking for at-risk habits
  (no check-in yet today, past a configurable time threshold)
- **Milestone notification receiver** — an internal endpoint Habit Service calls when a
  streak milestone is crossed
- User-facing endpoint to fetch their own notifications (JWT-protected)

---

## What this project deliberately demonstrates

### Middleware (custom, hand-built — not framework defaults)
- **Exception handling middleware** — global safety net, maps exception types to HTTP
  status codes, logs unhandled exceptions
- **Correlation ID middleware** — generates or propagates an `X-Correlation-Id` header,
  attached to every log line for a request via `ILogger.BeginScope`
- **Request logging middleware** — logs method, path, status code, and duration for every
  request
- Structured logging via **Serilog**, writing to console and rolling log files, with EF Core
  query logging enabled during development

### Filters (all 5 ASP.NET Core filter types, each with a distinct purpose)
| Filter type | Where | Purpose |
|---|---|---|
| Action Filter | User Service | Custom validation error response shape |
| Exception Filter | User Service | Narrow handling of a specific `NotFoundException`, distinct from the global middleware handler |
| Result Filter | User Service | Wraps every successful response in a consistent envelope |
| Authorization Filter | Habit Service | API-key gate for internal, service-to-service endpoints |
| Resource Filter | Habit Service | Short-circuits the pipeline entirely on a cache hit for the streak endpoint |

### Authentication & Authorization
- JWT issuance and independent validation across three services
- Custom policy-based authorization (`IAuthorizationRequirement` + `AuthorizationHandler<T>`)
  for resource ownership — not just "is authenticated," but "is this *their* resource"
- A separate, distinct mechanism (shared API key) for service-to-service calls that have no
  user identity at all

### EF Core depth
- Fluent API configuration (keys, required fields, unique indexes, enum-to-string conversion)
- One-to-many relationships with explicit cascade delete behavior
- Composite unique indexes (one check-in per habit per date)
- Eager loading (`.Include()`) vs. projection (`.Select()`) vs. `.AsNoTracking()` — each used
  deliberately where appropriate
- Disconnected entity updates (`EntityState.Modified`) in the repository pattern
- **A deliberately introduced and then fixed N+1 query** — proven via SQL query logging
  (before: 1+N queries; after: 1 query using a single projected join/count)
- Transactions and Unit-of-Work-style `SaveChanges()` control at the repository boundary

### Dependency Injection
- Correct lifetime choices throughout (Scoped for anything depending on a per-request
  `DbContext`; Singleton for stateless handlers and the in-memory cache)
- The **Singleton background service + per-iteration `IServiceProvider.CreateScope()`**
  pattern — required because a `BackgroundService` is a Singleton but its dependencies
  (repository, `DbContext`) are Scoped

---

## Tech stack

- ASP.NET Core 8 (Web API, Controllers)
- Entity Framework Core 10 + SQL Server
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt.Net for password hashing
- Serilog (console + rolling file sinks, structured logging)
- Scalar for interactive API documentation (OpenAPI-based)

---

## Running locally

Each service is a separate project within the `HabitTrack` solution, each with its own
SQL Server database (`UserServiceDb`, `HabitServiceDb`, `NotificationServiceDb`) and its
own port.

1. Ensure SQL Server (Developer Edition) is running locally
2. Run `Update-Database` for each service's project (via Package Manager Console, with
   the correct project selected as both Default Project and Startup Project)
3. Configure the solution's multiple startup projects to run all three services together
4. Each service exposes an interactive API reference via Scalar at `/scalar/v1`

**Note:** the JWT signing key, issuer, and audience in each service's `appsettings.json`
must match exactly across all three services for cross-service token validation to work.
The internal API key (`InternalApiKey`) must similarly match between Habit Service and
Notification Service.

---

## Known simplifications

- **In-memory caching** (Habit Service's streak cache) is per-instance — a multi-instance
  deployment would need a distributed cache (e.g. Redis) instead. The cache sits behind an
  interface specifically so this swap wouldn't require changing any calling code.
- **Milestone notifications use a direct, synchronous HTTP call** from Habit Service to
  Notification Service, not a message queue. A genuine pub/sub upgrade (e.g. RabbitMQ or
  Azure Service Bus) would decouple the two services further — this is a natural next step.
- **The at-risk time threshold is a fixed UTC hour**, not per-user timezone-aware — a
  production version would account for each user's own timezone.
- **Shared constants (JWT settings shape, internal API header name) are duplicated** across
  service projects rather than extracted into a shared library, since each service is
  independently deployable. This is a deliberate trade-off for project simplicity; a header
  name mismatch between two services was in fact hit and fixed during development, which is
  exactly the failure mode this duplication risks at larger scale.

---

## Still to build

- Docker + Docker Compose for all three services and SQL Server
- API Gateway (YARP/Ocelot) in front of all three services
- Azure deployment for at least one service
- Frontend (React)
