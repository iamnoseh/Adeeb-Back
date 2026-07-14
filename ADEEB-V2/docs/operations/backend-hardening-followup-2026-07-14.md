# Backend hardening follow-up - 2026-07-14

## Scope

This pass continued `hardening/backend-production-readiness` without changing Docker,
deployment, CI/CD, or GitHub Actions files. The requested independent audit document,
`docs/architecture/independent-production-readiness-audit-2026-07-13.md`, was not
present in the branch. The implementation was therefore verified against the current
code, migrations, tests, ADR-001, ADR-002, and the supplied follow-up requirements.

## Problems fixed

- Entitlement-changing operations now take a transaction-scoped PostgreSQL advisory
  lock per student before reading the latest entitlement. Receipt approval and manual
  grants cannot lose purchased duration across application instances.
- Legacy public receipt files have an explicit restartable migration with dry-run,
  normalized root-boundary checks, private-storage verification, structured outcomes,
  duplicate handling, and source deletion only after all database updates succeed.
  The legacy public URL prefix is blocked independently of migration progress.
- Commerce amounts have one `decimal(18,2)` business contract across application and
  domain validation. PostgreSQL constraints reject unsupported scale instead of
  relying on implicit column rounding.
- Receipt image processing bounds the real stream size, identifies the format before
  decode, enforces dimension/pixel/frame limits, skips metadata, and re-encodes to
  WebP.
- Authentication integration scenarios now derive permissions from persisted roles
  through login and refresh, and verify FinanceAdmin, ContentAdmin, and ordinary-user
  authorization behavior.
- Tariff, receipt, image-access, and entitlement orchestration now lives in focused
  use-case services used by endpoints. An architecture test prevents those services
  from delegating back to `CommerceService`.

## Database changes

- `20260714025913_EnforceCommerceAmountPrecision` changes tariff and receipt snapshot
  amount columns to PostgreSQL `numeric` and adds positive, maximum-value, and
  `scale(...) <= 2` check constraints. Unconstrained `numeric` is intentional: a
  `numeric(18,2)` column can round before a check constraint observes the input.
- Per-student serialization uses `pg_advisory_xact_lock` and requires an active
  relational transaction. No persistent lock table is introduced.
- No file movement is performed by an EF migration.

## Files changed

- Commerce domain/application: money rules, focused tariff/receipt/entitlement use
  cases, legacy migration models/service, and compatibility service locking.
- Commerce infrastructure: advisory lock, amount constraints/migration, hardened
  image processor, legacy migration hosted service, and legacy-route blocker.
- API/configuration: middleware registration and disabled-by-default legacy migration
  options.
- Tests: Commerce unit/security/migration tests, architecture rules, deterministic
  PostgreSQL concurrency scenarios, and persisted-role authorization scenarios.
- Documentation: Commerce ADRs, tariff API contract, legacy migration runbook, and
  this report.

## Verification

Executed without Docker:

- Release build: passed, 0 warnings, 0 errors.
- Release warning-as-error build: passed, 0 warnings, 0 errors.
- `dotnet format Adeeb.slnx --verify-no-changes`: passed.
- Identity unit tests: 25 passed, 0 failed.
- AcademicCatalog unit tests: 3 passed, 0 failed.
- QuestionBank unit tests: 60 passed, 0 failed.
- Students unit tests: 10 passed, 0 failed.
- Commerce unit tests: 56 passed, 0 failed.
- Architecture tests: 18 passed, 0 failed.
- Configuration validation tests: 14 passed, 0 failed.
- Total executed tests: 186 passed, 0 failed.
- `git diff --check`: passed.

The new real-PostgreSQL concurrency, database-constraint, and persisted-role HTTP
integration scenarios were not executed. No Commerce test connection was configured
through `ConnectionStrings__Commerce` or
`ADEEB_COMMERCE_MIGRATIONS_CONNECTION`; Testcontainers was intentionally not started
because this phase explicitly excludes Docker commands. These scenarios compile but
must not be reported as passed until run against PostgreSQL.

## Remaining risks

- PostgreSQL-specific advisory-lock and constraint behavior still needs execution in
  an environment with a configured test database.
- `CommerceService` remains as a compatibility implementation for existing internal
  callers and tests. Production endpoints use focused services, but duplicated legacy
  orchestration can drift until compatibility callers are retired.
- A 64-bit advisory-lock hash collision is theoretically possible, although
  operationally negligible.
- Legacy migration operators must review all `missing source` and `failed` results;
  blocking the public route prevents exposure but cannot reconstruct absent files.

