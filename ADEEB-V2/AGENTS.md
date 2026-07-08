# ADEEB V2 Engineering Constitution

This file is permanent repository-level guidance for Codex and other AI coding agents working in ADEEB V2. Read it before every non-trivial change. The goal is not to generate code quickly; the goal is to preserve a long-lived production educational platform with clear architecture, secure authentication, stable client contracts, reliable documentation, and disciplined engineering.

## 1. Mission And Current Reality

ADEEB V2 is the backend foundation for a multilingual educational platform used by Flutter mobile clients and future web/admin clients. Planned product areas include assessments, gamification, missions, XP, AdeebCoin, leagues, duels, market, premium/payments, notifications, AI-assisted learning, and analytics.

Do not claim a planned area exists until it is implemented in this repository. As of this file:

- Implemented: Identity/Auth backend foundation, documentation portal, health endpoints, PostgreSQL persistence, localization foundation, ProblemDetails error contract, Swagger/OpenAPI in Development, architecture/unit/integration-test scaffolding.
- Future/planned: non-Identity product modules such as learning, economy, payments, market, duels, notifications, analytics, and AI-assisted learning.

## 1A. Legacy Is Reference, Not Source Of Truth

Legacy IQRA/ADEEB implementations may be used only as behavioral and historical reference. Do not treat legacy entities, DTOs, routes, database schemas, localization models, delete behavior, pagination/filtering, or service boundaries as mandatory V2 design.

For every legacy behavior:

1. identify what currently exists;
2. identify why it exists;
3. identify whether ADEEB V2 still needs it;
4. identify changed V2 product requirements;
5. propose a V2-native design;
6. preserve only behavior that remains valid.

Never copy a legacy model merely because it exists. If a future task asks to implement a feature based on legacy code, first separate:

- Legacy facts: what IQRA/legacy currently does.
- ADEEB V2 product requirements: what the new system needs.
- Recommended V2 design: what should actually be implemented.

When the legacy comparison materially affects model shape, route contract, persistence, localization, authorization, or delete/update semantics, stop after analysis and ask for design approval before writing implementation code unless the user has already explicitly approved the V2 design.

## 1B. Multilingual Test Content Design Rule

Future assessment/test content in ADEEB V2 must support at minimum `tg-TJ` and `ru-RU` for frontend-visible test content where relevant, such as test title, description, instructions, and question-facing labels owned by the test definition. English may exist for UI/system behavior, but English test-content support must be an explicit product/design decision.

Do not assume legacy one-language fields are acceptable. Before implementing tests/assessments, analyze at least these storage options:

- Separate columns such as `TitleTg`, `TitleRu`, `DescriptionTg`, `DescriptionRu`.
- Translation table such as `TestTranslation(TestId, LanguageCode, Title, Description, Instructions)`.
- PostgreSQL `jsonb` localized content keyed by culture.

Evaluate query performance, filtering, sorting, uniqueness, maintainability, EF Core complexity, PostgreSQL indexing, frontend contracts, future language expansion, migration complexity, and content-completeness validation. Do not blindly choose translation tables, separate columns, or JSONB.

Language semantics must be explicit before implementation:

- Can a draft test exist with only Tajik or only Russian content?
- Must both `tg-TJ` and `ru-RU` exist before publish?
- Is one test bilingual, or are there separate language variants?
- How does filtering by language work?
- How does the frontend request a preferred language?
- What happens when a requested translation is missing?

For frontend contracts, evaluate resolved-translation responses for student/public list/detail endpoints and all-translation responses for admin create/update/detail endpoints. Choose deliberately per endpoint.

For any legacy-to-V2 assessment/test design, include a gap-analysis table covering at least test localization, pagination, filtering, update semantics, delete semantics, question relationship, status/publishing, authorization, frontend contracts, and database model.

## 2. Current Repository Map

- `Adeeb.slnx`: .NET solution.
- `Directory.Build.props`: central build settings, nullable enabled, latest language/analyzers.
- `Directory.Packages.props`: central package versions.
- `global.json`: .NET SDK pinned to `10.0.102` with latest feature roll-forward.
- `src/BuildingBlocks/Adeeb.SharedKernel`: framework-light shared primitives such as `Result`, `Error`, `Entity`.
- `src/BuildingBlocks/Adeeb.Application.Abstractions`: cross-cutting abstractions such as localization and time.
- `src/BuildingBlocks/Adeeb.Infrastructure`: cross-cutting implementations such as static message localization and Dushanbe/UTC time provider.
- `src/Modules/Identity/Adeeb.Modules.Identity`: Identity module with domain, application, endpoint, authentication, password, persistence, migrations, and seeding.
- `src/Hosts/Adeeb.Api`: ASP.NET Core API host, middleware composition, Swagger/OpenAPI, docs portal, health checks, rate limiting, module endpoint mapping.
- `src/Hosts/Adeeb.Worker`: minimal future worker host.
- `tests/Adeeb.ArchitectureTests`: NetArchTest dependency rules and documentation coverage rules.
- `tests/Modules/Adeeb.Identity.Tests`: focused Identity unit tests.
- `tests/Adeeb.IntegrationTests`: Testcontainers PostgreSQL scenario placeholders; skipped when Docker is unavailable.
- `docs/api`: route documentation loaded into `/docs`.
- `docs/flows`: integration flow documentation loaded into `/docs`.
- `docs/frontend`: frontend integration documentation loaded into `/docs`.
- `docs/architecture` and `docs/adr`: repository architecture and decisions; currently repository docs, not loaded by the portal.
- `deploy/docker`: local deployment assets.

## 3. Mandatory Discovery Workflow

For every non-trivial task:

1. Read this root `AGENTS.md`.
2. Locate and read any nested `AGENTS.md` or `AGENTS.override.md` that applies to the files being changed.
3. Inspect task-relevant code before implementing: routes, contracts, handlers/services, validators, domain entities, persistence, migrations, tests, docs, and configuration.
4. Trace dependencies: who calls this, what it calls, which module owns the data, and which clients consume the contract.
5. Identify invariants before writing code: uniqueness, ownership, authorization, transactions, concurrency, idempotency, localization, and compatibility.
6. Implement the smallest cohesive correct change.
7. Update docs when frontend-visible behavior, routes, contracts, errors, retry behavior, or flows change.
8. Run the narrowest relevant tests first, then broader tests when needed.
9. Review the final diff for unrelated edits, missed docs/tests, architecture violations, duplicated logic, and secrets.

For complex work involving schema changes, security changes, multiple modules, cross-cutting behavior, or large refactors, write a concise execution plan before modifications.

## 4. Architecture

ADEEB V2 uses a modular monolith direction with explicit boundaries and pragmatic vertical slices. A modular monolith does not mean every project can access every internal type. Modules own their behavior and data. Host projects compose modules; they do not own business rules.

Prefer:

- small, explicit, cohesive, testable changes;
- feature/use-case organization when adding application behavior;
- rich domain logic where invariants exist;
- simple code for simple CRUD;
- semantic CQRS when useful, without adding a CQRS framework by default.

Do not introduce architecture cargo cult. Avoid default use of `IRepository<TEntity>`, `GenericRepository<TEntity>`, wrappers over EF Core `DbContext` named `UnitOfWork`, `BaseService<TEntity>`, `BaseController<TEntity>`, `BaseHandler<TEntity>`, broad `Manager` classes, AutoMapper everywhere, MediatR everywhere, reflection magic, Kafka/RabbitMQ/microservices/event sourcing for hypothetical needs, or large framework layers without a real boundary.

## 5. Dependency Rules

Allowed direction:

- API host -> modules and building blocks.
- Worker host -> building blocks and explicit application/module contracts when needed.
- Module application -> module domain, shared kernel, application abstractions.
- Module infrastructure/persistence -> module application/domain and required external packages.
- SharedKernel -> no concrete modules and no infrastructure.

Forbidden direction:

- Domain -> EF Core, ASP.NET Core, Npgsql, JWT, HTTP, SignalR, Hangfire, or host projects.
- SharedKernel -> concrete modules, EF Core, ASP.NET Core, Npgsql, or infrastructure.
- Module A -> Module B infrastructure.
- Modules -> API host.
- Host project business rules that bypass module behavior.

Architecture tests currently enforce part of this. Add tests for new important boundaries when practical.

## 6. Module Ownership

Each module owns its schema, tables, entities, migrations, invariants, and write behavior. A module must not mutate another module's tables directly or depend on another module's internal EF entities. Cross-module behavior should use explicit contracts/events/outbox direction when needed. Read composition may be allowed for client efficiency, but write ownership remains with the source module.

Current database ownership:

- Identity owns PostgreSQL schema `identity`.
- Identity tables are `identity.users` and `identity.auth_sessions`.
- Identity migrations live under `src/Modules/Identity/Adeeb.Modules.Identity/Infrastructure/Persistence/Migrations`.

Do not create one giant `ApplicationDbContext` unless the architecture is deliberately changed and documented.

## 7. API Conventions

Current business API base path is `/api/v2`. Identity routes are under `/api/v2/auth`.

Every API endpoint must have:

- stable route and HTTP method;
- request/response contracts;
- authentication and authorization definition;
- validation;
- cancellation token propagation;
- declared success and error statuses;
- ProblemDetails-compatible error behavior;
- OpenAPI metadata sufficient for Swagger/OpenAPI;
- route documentation when frontend-visible.

Endpoints must stay thin. Avoid large business logic in endpoint lambdas, complex direct `DbContext` orchestration in endpoints, and repeated manual exception mapping.

Development endpoints and docs:

- `/swagger` is enabled only in Development.
- `/openapi/v2.json` serves the machine-readable contract in Development through Swagger.
- `/docs` serves internal human documentation when documentation options enable it.
- `/health/live` is liveness and should not depend on external services.
- `/health/ready` may validate critical dependencies such as PostgreSQL.

Do not break the separation:

- `/api/v2/*` = business API.
- `/docs` = human/internal documentation.
- `/openapi/*` = machine contract.

## 8. Mandatory Route Documentation Rule

A new or changed frontend-visible API route is NOT DONE until:

1. endpoint metadata is correct;
2. OpenAPI contract is correct;
3. `docs/api/...` Markdown exists or is updated;
4. `Frontend Behavior` is documented;
5. `Stable Error Codes` are documented;
6. documentation coverage tests pass.

Route docs use the established front matter and required sections. Current required sections are enforced in `tests/Adeeb.ArchitectureTests/DocumentationCoverageTests.cs`:

- Endpoint
- Purpose
- Status
- Module
- Authentication
- Authorization
- Rate Limit
- Localization
- Request Headers
- Path Parameters
- Query Parameters
- Request Body
- Field Rules
- Success Response
- Error Responses
- Stable Error Codes
- Frontend Behavior
- Retry Policy
- Caching
- Idempotency
- Security Notes
- Example Flow
- Related Endpoints
- Change History

If a section does not apply, write `Not applicable.` Do not silently omit it.

Before documenting a route, inspect the actual endpoint, contract, validation, service/domain behavior, persistence, transactions, error mapping, rate limit, localization, and tests. Never invent public behavior in docs.

OpenAPI is the source of truth for method/path/schema. Markdown is the source of truth for business explanation, frontend behavior, retry rules, security notes, and integration flow.

## 9. Error Contract And Result Pattern

Preserve the ProblemDetails-compatible contract:

- `type`: URI for error family.
- `title`: localized human title/message.
- `status`: HTTP status.
- `code`: stable machine-readable error code.
- `traceId`: troubleshooting correlation.
- `errors`: structured validation errors when applicable.

Frontend logic must branch on stable `code`, not localized `title`.

Expected business failures such as validation, not found, conflict, unauthorized, and forbidden use the lightweight `Result` / `Result<T>` / `Error` pattern. Exceptions are for unexpected failures, not normal business flow. Do not build a complex functional framework around this.

Do not return stack traces in production.

## 10. Localization Rules

Supported cultures are `tg-TJ`, `ru-RU`, and `en-US`. Default culture is `tg-TJ`.

Current culture resolution:

1. `X-Adeeb-Language` header during request localization.
2. `Accept-Language` header.
3. default `tg-TJ`.
4. For authenticated requests, middleware loads the user's persisted `PreferredLanguage` and sets current culture after authentication.

Stable error codes are language-independent. Resource keys such as `Auth.InvalidCredentials` are not API error codes. API codes such as `auth.invalid_credentials` remain stable.

Backend localization is for API errors, validation messages, and server-generated system text. Client localization is for buttons, labels, navigation, and static Flutter/web UI. Future database translations are for business content such as mission titles, market descriptions, and notification templates. Do not send static Flutter UI labels from the backend and do not store every system error in the database.

## 11. Security And Authentication Rules

Current Identity behavior includes registration, email-or-phone login, short-lived JWT access tokens, hashed refresh tokens, refresh rotation, token families, reuse detection, multi-device sessions, logout, logout-all, session revocation, current user, change password, superadmin seeding in Development, and Swagger bearer auth.

Never log or store:

- plaintext passwords;
- raw access tokens;
- raw refresh tokens;
- password hashes in logs;
- refresh token hashes in logs;
- authorization headers;
- JWT signing keys.

Refresh tokens are generated with cryptographic randomness, returned to the client once, and persisted only as SHA-256 hashes. Access-token claims currently include `sub`, `sid`, `email`, role claim, `jti`, and `iat`. Do not add profile/dashboard/business state to JWTs.

Authentication is not authorization. For every protected route ask:

- Who is authenticated?
- What resource do they own?
- What role/policy is required?
- Can another user's ID be supplied?
- Is there an IDOR risk?

Use the smallest authorization scope. Avoid broad `Admin` checks where a specific policy is appropriate. Do not expose operational docs or OpenAPI publicly in production accidentally.

## 12. Password And Session Rules

Use the centralized configured password policy. Current default policy is minimum 8 characters, uppercase required, lowercase required, digit required, symbol not required. Do not duplicate password validation logic.

Session/device behavior:

- `device` in register/login is optional.
- If omitted, Identity derives device metadata from IP, User-Agent, and optional headers: `X-Adeeb-Device-Id`, `X-Adeeb-Device-Name`, `X-Adeeb-Platform`, `X-Adeeb-App-Version`.
- Swagger usage must work without manually filling device fields.

Change password keeps the current session alive and revokes other sessions.

## 13. Time Rules

Persist timestamps and JWT times in UTC. Current infrastructure exposes Dushanbe projection through `IDateTimeProvider.ToDushanbeTime`, using UTC+05:00. Public responses may include both UTC and Dushanbe timestamps where useful, as current auth token/session responses do. Do not store local Dushanbe time as the canonical database value.

## 14. Rate Limiting

Security-sensitive routes must use named rate-limit policies configured in the host, not random hardcoded checks inside handlers. Current policies:

- `auth-login`
- `auth-register`
- `auth-refresh`
- `auth-change-password`

Future sensitive routes such as forgot password, OTP, payment submit, reward claim, and market purchase require rate-limit review and documentation reflecting the actual policy.

## 15. Database And EF Core Rules

Use PostgreSQL according to module ownership. Schema changes use EF Core migrations in the owning module.

Before schema changes:

1. inspect current model;
2. inspect existing migrations;
3. determine data impact;
4. identify constraints and indexes;
5. consider rollback/deployment order.

Do not modify old applied migrations. Do not run destructive migrations automatically.

For reads prefer projections, bounded result sets, appropriate indexes, and `AsNoTracking` when no write tracking is needed. Avoid loading full graphs for DTOs, unneeded `Include` chains, queries in loops, unbounded `ToList`, and premature materialization. Do not add `AsNoTracking` to writes blindly.

Unbounded collection endpoints are forbidden unless the data is inherently bounded. Use offset pagination for ordinary admin lists and keyset/cursor pagination for large feeds/history/hot paths when justified.

## 16. Transactions, Concurrency, And Idempotency

Use explicit transactions when one business operation spans multiple consistency-sensitive writes. Do not start transactions around every trivial single-row operation.

Refresh rotation is security-critical. Two concurrent refresh attempts with the same token must not both succeed. Any change to refresh flow requires transaction/concurrency reasoning, reuse-detection review, tests, and documentation update.

Critical retryable writes need idempotency review. Mobile networks retry. Future examples include payments, market purchases, reward claims, referral rewards, mission rewards, AdeebCoin credit, XP awards, and premium activation. Use idempotency keys, unique constraints, or processed-event keys according to context.

For future XP or AdeebCoin ledgers: preserve append-only history, idempotency keys, and auditability. Do not implement casual `Balance -= amount` behavior without an established ledger/consistency model.

## 17. Outbox And Background Work

ADR-006 documents future outbox direction. Use outbox-style reliable side effects where cross-module delivery matters, without inventing a huge event-bus framework. Avoid direct coupling such as Identity calling a future Gamification service directly for cross-module effects.

Background jobs should call application contracts, not controllers. They must be idempotent when retries are possible, log correlation/business IDs, handle cancellation, and avoid harmful overlapping execution.

## 18. Performance And Caching

Do not claim optimization without evidence. For performance-sensitive changes, inspect query count, query shape, payload size, serialization, allocations where relevant, indexes, client call pattern, and cache behavior.

ADEEB has a Flutter mobile client. Avoid forcing one mobile screen to call many sequential endpoints when a stable aggregate read model is more appropriate. Read composition is allowed for mobile latency and payload efficiency, but write ownership remains in source modules.

Cache only when justified. Good candidates include reference data, mobile read models, standings snapshots, and public configuration. Do not cache auth validation, wallet debit, payment approval, or reward claim as a substitute for correct persistence. Cache keys and invalidation rules must be explicit.

## 19. Observability And Configuration

Use structured logs, trace IDs, OpenTelemetry, and metrics where useful. Security-sensitive events should use stable structured names such as `auth.login.failed`, `auth.refresh.succeeded`, `auth.refresh.reuse_detected`, and `auth.session.revoked`.

Use strongly typed options and startup validation for critical configuration. Fail fast for missing/unsafe JWT signing config, issuer, audience, database config, and security settings. Production secrets must not be committed. Do not silently fall back to insecure production defaults.

## 20. Code Quality Rules

Required:

- nullable reference types respected;
- clear names;
- small cohesive methods;
- cancellation propagation;
- async I/O;
- no sync-over-async;
- no `.Result` or `.Wait()`;
- no mutable static global state for request/business state;
- no service locator pattern;
- no blind refactoring;
- no unrelated renames or style churn.

Comments should explain why, invariants, security reasoning, or non-obvious tradeoffs. Do not comment obvious syntax.

Avoid vague names such as `Helper`, `Manager`, `Utils`, `CommonService`, `ProcessData`, and `HandleStuff` unless the responsibility is genuinely explicit. Do not create god services that combine authentication, database writes, notifications, wallet, analytics, and file storage.

## 21. Testing Rules

Tests are part of implementation. Use the right test type:

- Unit tests for isolated logic such as token hashing decisions, language parsing, password policy, domain invariants, and error mapping.
- Integration tests with real PostgreSQL/Testcontainers for persistence/security behavior, unique constraints, transactions, concurrency, row locking, and PostgreSQL-specific behavior.
- Architecture tests for dependency boundaries and route documentation coverage.
- Contract tests where frontend-visible request/response shape, stable error codes, localization, and OpenAPI behavior matter.

Do not trust EF InMemory for unique constraints, transactions, concurrency, PostgreSQL behavior, or row locking.

Sensitive changes require regression tests: IDOR prevention, refresh reuse, concurrent refresh, cross-user session revoke, blocked account login, duplicate rewards, negative balances, and similar invariants.

## 22. Backward Compatibility

Before changing an existing route inspect clients, docs, tests, and current contracts. Do not casually rename JSON fields, change nullability, remove error codes, change status codes, or change routes. If a breaking change is necessary, make it explicit, version it where appropriate, document it, and update consumers/tests.

## 23. File Upload Rules

For any future upload validate size, allowed type, actual content where appropriate, safe generated filename, path containment, storage boundary, and authorization. Never trust user filenames. Never expose server physical paths.

## 24. Current Verification Commands

From the repository root:

```powershell
dotnet restore .\Adeeb.slnx
dotnet build .\Adeeb.slnx
dotnet test .\Adeeb.slnx
dotnet test .\tests\Adeeb.ArchitectureTests\Adeeb.ArchitectureTests.csproj
dotnet test .\tests\Modules\Adeeb.Identity.Tests\Adeeb.Identity.Tests.csproj
dotnet test .\tests\Adeeb.IntegrationTests\Adeeb.IntegrationTests.csproj
```

Integration tests are currently skipped because they require Docker/Testcontainers PostgreSQL. When Docker is available, remove/adjust skips only with real integration implementation.

## 25. Known High-Risk Areas

- Refresh-token rotation, row locking, family reuse detection, and concurrent refresh.
- Session ownership and IDOR prevention.
- Password changes revoking all other sessions while preserving the current session.
- Stable error codes and localized messages.
- Route documentation coverage for every frontend-visible endpoint.
- UTC persistence with Dushanbe presentation.
- Future financial/reward flows requiring idempotency, ledger discipline, and auditability.
- Future payment approval and outbox side effects.

## 26. Definition Of Done

A task is complete only when all applicable items are satisfied:

- Code compiles and follows module boundaries.
- Behavior is implemented with edge cases considered.
- Security, ownership, concurrency, and idempotency are reviewed.
- Persistence constraints, indexes, migrations, and transactions are considered.
- API contract and stable errors are explicit.
- Localization supports `tg-TJ`, `ru-RU`, and `en-US` where frontend-visible.
- Route docs, flow docs, and OpenAPI metadata are updated when required.
- Relevant tests pass or skipped tests are clearly explained.
- Final diff is reviewed for unrelated changes and secrets.

## 27. Realism Rule

Do not pretend certainty. If repository behavior is ambiguous, tests are missing, docs contradict code, or a boundary is already weak, identify it, preserve existing behavior unless the task requires change, report the discrepancy, and avoid inventing confidence.
