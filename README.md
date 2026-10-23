# HabitTrack

A habit-tracking backend built as **.NET 10 microservices**, with an **Azure deployment and CI/CD pipeline** on top. Three services, each with its own database, trust each other through JWTs and an internal API key. A timer-triggered Azure Function detects streak milestones.

## Highlights

- Three microservices (User, Habit, Notification), each owning its own SQL database
- Cross-service JWT trust plus an internal API key (`X-Internal-Api-Key`) for service-to-service calls
- Custom middleware, all five ASP.NET filter types, EF Core depth (N+1 fix, migrations), DI lifetimes
- xUnit test suite that gates every deployment
- Azure: App Service with a staging slot, Azure SQL, Key Vault + Managed Identity, Azure Functions
- GitHub Actions CI/CD with passwordless OIDC login, smoke test, slot swap and automatic rollback

## Architecture

```
            +-------------+        JWT        +--------------+
  Client -> | UserService | <---------------- | HabitService |
            +------+------+                   +------+-------+
                   |                                 |
              UserDb (SQL)                      HabitDb (SQL)
                                                     |
                                     X-Internal-Api-Key (HTTP)
                                                     v
                                          +----------------------+
                                          | NotificationService  |
                                          +----------+-----------+
                                                     |
                                            NotificationDb (SQL)

  MilestoneFunction (timer, every 5 min) --reads--> HabitDb + UserDb
```

## Services

| Service | Responsibility |
|---|---|
| **UserService** | Registration, login, JWT issuing, user profiles |
| **HabitService** | Habits, check-ins, streak calculation, at-risk and milestone logic |
| **NotificationService** | Stores and serves notifications; called by HabitService with the internal API key |
| **MilestoneFunction** | Azure Function (timer trigger) that finds habits crossing a streak milestone |

## What this project deliberately demonstrates

- **Middleware:** custom middleware in the request pipeline
- **Filters:** authorization, resource, action, exception and result filters
  
  | Filter | Used for |
  |---|---|
  | Authorization | Access checks before the action runs |
  | Resource | Short-circuiting / caching around the action |
  | Action | Cross-cutting logic before and after actions |
  | Exception | Consistent error responses |
  | Result | Shaping the response |

- **Auth:** JWT bearer authentication shared across services, plus an API-key scheme for internal calls
- **EF Core:** per-service DbContext, migrations, an N+1 query fixed with eager loading and projection
- **Dependency injection:** correct lifetimes (singleton / scoped / transient)
- **Testing:** xUnit tests for streak and milestone logic, run by CI on every push

## Azure deployment and CI/CD

### What is deployed

| Component | Azure service | Notes |
|---|---|---|
| UserService | App Service (Linux, .NET 10, S1) | `production` and `staging` slots |
| UserDb, HabitDb | Azure SQL | Free serverless offer and Basic DTU tier |
| Secrets | Key Vault (RBAC mode) | Read by Managed Identity, no secrets in code |
| MilestoneFunction | Azure Functions (Consumption) | Isolated worker, timer trigger |
| Pipeline | GitHub Actions | OIDC login, no stored Azure passwords |

HabitService and NotificationService were not deployed. The resources have since been torn down, so there are no live endpoints.

### Architecture on Azure

```
GitHub push --> GitHub Actions --OIDC--> Azure
                                          |
        +---------------------------------+----------------------+
        v                                                        v
  App Service (UserService)                         Function App (MilestoneFunction)
   [staging] <--swap--> [production]                          |
        |                                                     |
        +--- Managed Identity ---> Key Vault (JwtKey, SQL)    +--> Azure SQL (Habit + User DBs)
        |
        +--> Azure SQL (UserDb)
```

### Configuration and secrets

- `appsettings.json` is git-ignored, so CI and Azure never receive it. Configuration comes from App Settings instead.
- Hierarchical keys use double underscores: `Jwt__Key` maps to `Jwt:Key`.
- Sensitive values are Key Vault references: `@Microsoft.KeyVault(SecretUri=...)`.
- Each slot has its own system-assigned Managed Identity, granted **Key Vault Secrets User**.
- Slot-specific values (such as `SLOT_LABEL`) are marked as deployment slot settings so they stay with the slot during a swap.

### MilestoneFunction

Runs on `0 */5 * * * *`. For each habit it loads check-ins, computes the current streak, and asks `MilestoneChecker.GetCrossedMilestone(lastCheckedStreak, currentStreak)`. Only when a milestone is crossed does it look up the user's email. It then persists `LastCheckedStreak`, which makes the function **idempotent**: re-running it never re-announces the same milestone.

### CI/CD pipeline

```
push / PR to main
      |
 build-and-test  (restore, build, xUnit tests)      <- failing tests stop everything
      |
 changes         (path filter: what changed?)
      |
      +--> deploy-userservice
      |       deploy to staging --> smoke test /health (must report this commit SHA)
      |       --> swap staging <-> production --> verify production --> rollback swap on failure
      |
      +--> deploy-milestonefunction
```

- **Tests gate deploys:** deployment jobs `need` the test job and only run on a push to `main`.
- **Passwordless:** GitHub authenticates to Azure with OIDC federation (federated credential on the `main` branch, Contributor on one resource group). No client secret is stored.
- **Build once:** the published output is uploaded as an artifact and deployed, not rebuilt.
- **Path filters** (`dorny/paths-filter`):

  | Changed path | Deploys |
  |---|---|
  | `UserService/**` | UserService |
  | `MilestoneFunction/**`, `HabitService/**`, `UserService/**` | MilestoneFunction (it references both) |
  | `.github/workflows/**` | Everything |

- **Concurrency:** one deploy per service at a time.
- **Version stamping:** the commit SHA is baked into the assembly (`SourceRevisionId`) and exposed by `/health`, so the pipeline can prove which build is running.

### Zero-downtime release flow

1. Deploy the new build to the **staging** slot.
2. Smoke test staging `/health`, requiring the exact commit SHA.
3. **Swap** staging and production. The new version is already warm, so users see no cold start.
4. Verify production `/health` returns the same SHA.
5. If verification fails, swap back. The previous version is still in the other slot.

### Problems solved along the way

| Problem | Cause | Fix |
|---|---|---|
| App served a PHP container | `CURRENT_STACK` metadata was "Unknown" | Set Stack to .NET 10 in Portal general settings |
| Crash loop, exit code 150 | .NET 10 app on a .NET 8 runtime | `--linux-fx-version "DOTNETCORE:10.0"` |
| Function deployed, zero functions listed | Hidden `.azurefunctions` folder dropped from artifact | `include-hidden-files: true` on upload |
| Staging smoke test crashed (NullReference) | Git-ignored `appsettings.json` never reached CI | Supply `Jwt__*` App Settings and Key Vault `JwtKey` on both slots |
| Key Vault "not allowed" for its own creator | RBAC mode needs explicit roles | Secrets Officer for the admin, Secrets User for identities |
| SQL error 40615 | Client IP not in firewall | Add client IPv4 firewall rule |
| Free SQL DB paused | Monthly free vCore-second allowance used | Left paused; avoided upgrading to stay free |
| Connection string saved under wrong name | CLI quoting | Delete and re-add via Portal |

## Tech stack

.NET 10, ASP.NET Core, Entity Framework Core, SQL Server / Azure SQL, xUnit, Azure App Service, Azure Functions, Azure Key Vault, Managed Identity, GitHub Actions, Scalar (API docs).

## Running locally

1. Create an `appsettings.json` in each service (they are git-ignored). Use the same `Jwt` settings and `InternalApiKey` in all services, plus a connection string to a local SQL Server for each.
2. Apply migrations in each service: `Update-Database` (Package Manager Console) or `dotnet ef database update`.
3. Start the services together (multiple startup projects in Visual Studio).
4. Browse API docs at `/scalar/v1` on each service.
5. Run the tests: `dotnet test HabitTrack.slnx`

## Known simplifications

- In-memory cache rather than a distributed cache
- Synchronous HTTP between services instead of a message queue
- At-risk hour is a fixed UTC value, not per-user time zone
- Some shared constants are duplicated across services
- The Function reads the databases directly rather than calling service APIs
- `/health` does not check database connectivity
- Only UserService and the Function were deployed to Azure

## Future work

- **Docker containerization:** a Dockerfile per service and a Docker Compose setup (with SQL Server) so the whole system starts with one command
- **Frontend:** a React client for the habit tracker