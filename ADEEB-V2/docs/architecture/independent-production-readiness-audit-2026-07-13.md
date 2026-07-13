# ADEEB Backend Independent Production Readiness Audit

Date: 2026-07-13  
Repository: `iamnoseh/Adeeb-Back`  
Target branch: `hardening/backend-production-readiness`  
Compared with: `main`

## 1. Executive Verdict

The Phase 1–9 implementation is **partially confirmed**, not fully confirmed.

The branch contains substantial and generally coherent hardening work: PostgreSQL optimistic concurrency for a single receipt review, immutable tariff snapshots, private receipt storage for new uploads, permission policies, append-only audit records, scoped idempotency, cursor pagination, query indexes, telemetry, readiness checks, and PostgreSQL integration-test scenarios.

It is not ready for an unqualified production approval. Two High findings remain:

1. Concurrent approval of two different receipts for the same student can calculate the same entitlement start time and create overlapping rather than cumulative paid periods.
2. The private-storage migration rewrites historical database paths but does not move the physical files out of the public `wwwroot` upload directory. Historical images can remain publicly reachable while the new protected endpoint points to a private object that does not exist.

Runtime verification was blocked in this independent environment because the GitHub connector exposes repository files and metadata but not an authenticated full checkout usable by `dotnet`, Docker, or `git`. No CI run or pull request exists for the target branch. Consequently, the prior agent's claimed local build, test, publish, format, and EF results are evidence reported by that agent, not independently reproduced evidence.

Final recommendation: **NO-GO for production until the High findings are fixed and the complete PostgreSQL/Testcontainers CI workflow passes on the exact reviewed commit.**

## 2. Audit Scope and Limitations

Performed:

- GitHub repository and branch verification.
- `main...hardening/backend-production-readiness` comparison through the GitHub API.
- Static review of security-critical production code, migrations, tests, configuration, and CI workflow.
- Review of test assertions, test database provider, final-state checks, and whether concurrency tests force a real race.
- Review of changed tracked files for obvious secrets and generated/local artifacts.
- Check for pull requests and observed CI statuses on the target branch.

Blocked from independent runtime verification:

- `git status`, local uncommitted files, and a native `git log` graph from the author's Windows checkout.
- `dotnet restore`, build, warnings-as-errors build, format, test, and publish.
- Docker/Testcontainers execution and three repeated integration-suite runs.
- `dotnet ef migrations has-pending-model-changes` for the five contexts.
- k6 execution.
- NuGet vulnerability audit completion.
- PostgreSQL `EXPLAIN (ANALYZE, BUFFERS)`.
- Branch protection and required-check enforcement.

These limitations are reported as `BLOCKED FROM RUNTIME VERIFICATION`; they are not counted as passing.

## 3. Git and Change Review

GitHub comparison result:

- Status: `diverged`.
- Branch is 13 commits ahead of its merge base and 2 commits behind current `main`.
- Merge base: `b22f48d7c65c7bc9216cc60abee00aa754a05f99`.
- Current `main`: `857dc1e913fde9c4f07a422c7f1827fb26b7f8f3`.
- Reviewed branch tip: `62ae12c7bf5f197fdd64cb06cb105504456ebe2c` (`docs: finalize backend production hardening report`).
- `ADEEB-V2/docs/operations/local-development.md` is modified (`+4`, `-0`).
- The change list contains source, migrations, tests, documentation, package references, and `.github/workflows/ci.yml`; no binary build output or obvious generated runtime artifact was added.
- The two commits by which `main` is ahead add and then remove a temporary private source-export workflow. The hardening branch should be updated from `main` before final merge even though those commits have no lasting tree effect.
- There is no pull request for the hardening branch and no combined status checks were observed.

The available connector returned the branch tip and aggregate comparison but not the full parent graph. Therefore, exact commit-to-phase mapping could not be independently reconstructed. Phase mapping below is based on actual changed code and migration timestamps, not commit messages or documentation claims.

## 4. Verification Matrix

| Claim | Status | Evidence | Issues |
|---|---|---|---|
| Atomic payment receipt review | PARTIALLY CONFIRMED | `PaymentReceipt.Version`; `IsRowVersion()`/PostgreSQL `xmin`; explicit transactions; concurrency and unique-violation mapping; unique source-receipt index | Different receipts for the same student can create overlapping paid periods; race test does not force both reads before either save |
| Immutable tariff snapshots | PARTIALLY CONFIRMED | Snapshot columns, submission population, snapshot response projection, snapshot duration used for entitlement | No explicit max precision/scale validation before `numeric(18,2)` persistence |
| Private receipt storage | PARTIALLY CONFIRMED | Local/S3 implementations, authorized streaming, decode/re-encode, metadata stripping, cleanup, health probe, orphan worker | Legacy files are not physically migrated; pixel limits are checked only after full decode; S3 requires static credentials |
| Permission-based authorization | PARTIALLY CONFIRMED | Typed permissions, policies, role mapping, endpoint policies | HTTP tests forge permission claims and do not verify login/refresh claim issuance |
| Append-only audit log | CONFIRMED (STATIC) | Same DbContext/SaveChanges, PostgreSQL update/delete trigger, actor/IP/UA/correlation fields, redaction | Runtime trigger behavior not independently executed; failed image-open attempt is logged before storage open succeeds |
| Receipt idempotency | PARTIALLY CONFIRMED | Student-scoped unique index, file-content hash fingerprint, mismatch `409`, loser-object cleanup | PostgreSQL concurrent same-key path is not directly tested; legacy overload can use a constant `legacy` fingerprint |
| Cursor pagination | CONFIRMED (STATIC) | Base64 cursor, validation, `(CreatedAtUtc, Id)`, `limit + 1`, `HasMore`, `NextCursor`, equal-timestamp PostgreSQL scenario | Endpoint-level `422` assertions were not found; compatibility list methods still impose a 100-item cap |
| Database indexes and cache | PARTIALLY CONFIRMED | Query-aligned indexes, partial pending index, projections, post-save cache invalidation | No independent `EXPLAIN ANALYZE`; several indexes may overlap; paid-period stacking race remains |
| Refactoring/module boundaries | NOT CONFIRMED | Thin endpoints and architecture rules exist | `CommerceService` remains a roughly 950-line God Service; three “UseCases” classes are wrappers around it |
| Observability and HTTP hardening | PARTIALLY CONFIRMED | Forwarded headers first, correlation/security headers, CORS, rate limits, live/ready split, real metrics | Npgsql OpenTelemetry instrumentation is absent; no observed exporter run; workflow/config remains operationally unverified |
| Integration database reset | CONFIRMED (STATIC) | PostgreSQL Testcontainer; all five schemas truncated with `RESTART IDENTITY CASCADE`; unique container per fixture | Runtime repetition/flakiness check blocked |
| EF factories and snapshots | PARTIALLY CONFIRMED | Five factories exist and use environment variables with passwordless localhost fallback; snapshots added/updated | Pending-model commands were not independently executed |
| CI workflow | PARTIALLY CONFIRMED | Correct working directory, Docker gate, format, warn-as-error build, tests/coverage, audit, publish, artifact path | No observed run; no explicit workflow permissions or timeout; branch push does not trigger CI and no PR exists |
| Secret scan | PARTIALLY CONFIRMED | Changed configuration has empty secrets; factories have no password; test credentials are deterministic/local | Full tracked-tree scan could not be repeated without a checkout |

## 5. Critical Findings

No Critical finding was proven by the available static evidence.

This does not mean critical runtime risk is ruled out: the security-sensitive PostgreSQL suite, migrations, and full repository secret scan were not independently executed.

## 6. High Findings

### H-01 — Concurrent payments for one student can lose paid duration

`EnsurePaymentEntitlementAsync` reads the latest active expiry and then creates a new entitlement. The operation is protected only against duplicate entitlement creation for the same receipt. There is no per-student database lock, serializable transaction, exclusion constraint, or other mechanism that serializes entitlement-period allocation across two different receipts.

Evidence:

- `CommerceService.cs:738-768` reads `Max(ExpiresAtUtc)`, calculates `startsAt`, then stages an entitlement.
- `CommerceDbContext.cs:143-146` makes `SourcePaymentReceiptId` unique, which prevents duplicates for one receipt but does not serialize different receipts belonging to the same student.

Failure scenario:

1. Receipt A and receipt B belong to the same student.
2. Both approvals read the same current expiry.
3. Both set the same `StartsAtUtc` and add their own duration.
4. Both commits are allowed because their `SourcePaymentReceiptId` values differ.
5. The periods overlap, so the student receives less total premium time than was paid for.

Severity: **High — financial integrity**.

Required fix: serialize allocation per student in PostgreSQL (for example, lock a stable student/entitlement aggregate row, use an advisory transaction lock keyed by student, or redesign around a ledger and deterministic projection) and add a forced-race PostgreSQL test approving two receipts for the same student.

### H-02 — Historical receipt migration does not privatize physical files

`SecurePrivatePaymentReceiptStorage` renames the database column and rewrites `/uploads/commerce/receipts/...` into a new object key. It does not copy or move the existing file from `wwwroot` into `data/private` or S3, and `UseStaticFiles()` remains enabled.

Evidence:

- `20260713142107_SecurePrivatePaymentReceiptStorage.cs:13-25` performs only a column rename and SQL string rewrite.
- `Program.cs:83` still enables static-file serving.
- `LocalPrivateFileStorage.cs:12-18` uses a different content-root path (`data/private` by default).

Impact:

- Historical files can remain publicly reachable under their old URL.
- The protected streaming endpoint can return not found because the rewritten private object was never created.
- Database state implies successful migration while storage state is inconsistent.

Severity: **High — confidentiality and data availability**.

Required fix: create a resumable, verified data migration/backfill job that copies each legacy file to the configured private provider, verifies hash/readability, updates the row only after successful copy, then removes the public original. Include rollback/retry and migration metrics.

## 7. Medium Findings

### M-01 — Currency is validated, monetary precision is not

`Validation.ValidateTariff` and `CommerceTariff.Update` verify only that price is positive. They do not reject more than two fractional digits or values outside `numeric(18,2)` range. PostgreSQL rounding/overflow can therefore disagree with the immediate API response or fail at persistence.

Evidence: `Validation.cs:18-25`, `CommerceTariff.cs:42-50`, `CommerceDbContext.cs:60` and `:81`.

### M-02 — Concurrency tests can pass without exercising an optimistic-concurrency collision

The PostgreSQL test starts two tasks, but it has no barrier after both contexts read `Pending`. The accepted failure set includes `ReceiptAlreadyReviewed`, so a serialized execution passes without proving `xmin` collision handling.

Evidence: `CommerceConcurrencyIntegrationScenarios.cs:26-69` and `:183-209`.

The final database assertions are valuable, but a deterministic forced-race test is still required.

### M-03 — Authorization integration test bypasses production claim issuance

The test signs its own JWT and supplies arbitrary permission claims. It proves policy enforcement at endpoints but not that login or refresh creates the correct claims for FinanceAdmin, SupportAdmin, ContentAdmin, Admin, and SuperAdmin.

Evidence: `CommerceAuthorizationIntegrationScenarios.cs:79-106`.

### M-04 — Image pixel limits are applied after full decode

The processor validates declared/actual byte size and then calls `Image.Load(bytes)` before checking dimensions and total pixels. A malicious compressed image can force large allocations during decode before the limit is enforced.

Evidence: `ReceiptImageProcessor.cs:46-56`.

Use ImageSharp identification/decoder limits before allocating the complete decoded image.

### M-05 — Npgsql telemetry instrumentation is missing

Tracing config includes ASP.NET Core, HttpClient, and `Adeeb.Commerce`, but no Npgsql instrumentation. Database spans therefore are not confirmed.

Evidence: `OpenTelemetryExtensions.cs:34-49`.

### M-06 — Claimed refactoring did not split the God Service

The endpoints are thinner, but `TariffUseCases`, `PaymentReceiptUseCases`, and `EntitlementUseCases` delegate to the same large `CommerceService`. The branch adds hundreds of lines to that service. Architecture tests prohibit endpoint dependencies but do not enforce independent handlers or bounded application services.

Evidence: `PaymentReceiptUseCases.cs:10-49`, `TariffUseCases.cs:6-25`, `EntitlementUseCases.cs:7-24`, and `DependencyRulesTests.cs:227-253`.

### M-07 — S3 provider requires long-lived static credentials

Registration rejects S3 configuration without AccessKey and SecretKey and constructs `BasicAWSCredentials`. This prevents the preferred production use of workload/instance roles and increases secret-management burden.

Evidence: `DependencyInjection.cs:42-60`.

### M-08 — No observed CI run for the exact branch

The workflow triggers on `main` pushes or pull requests targeting `main`. The hardening branch has no PR and no observed status checks. Static YAML quality is not runtime evidence.

## 8. Low Findings

### L-01 — Workflow least privilege and timeout are not explicit

`.github/workflows/ci.yml` has concurrency control but no top-level `permissions: contents: read` and no job `timeout-minutes`.

### L-02 — Compatibility list methods silently cap results at 100

`GetCurrentPaymentReceiptsAsync` and `GetPaymentReceiptsAsync` call the cursor methods with `Limit = 100` and discard pagination metadata. Current endpoints use the paginated use cases, so this is not an endpoint defect today, but the methods are hazardous for future callers.

Evidence: `CommerceService.cs:362-378` and `:414-429`.

### L-03 — Storage health probe does not verify read-after-write

The readiness probe writes and deletes an object but never opens and validates the content. It proves basic writability/deletion, not complete read-path health.

Evidence: `PrivateFileStorageHealthCheck.cs:13-22`.

### L-04 — Branch is behind `main`

The branch is two commits behind current `main`. Although those commits add and remove a temporary workflow, the final reviewed branch should be brought up to date before merge and reverified.

## 9. Phase-by-Phase Verification

### Phase 1

Status: **PARTIALLY CONFIRMED**.

Single-receipt approve/approve and approve/reject protection is well designed statically: PostgreSQL `xmin`, explicit transaction, final-state domain guard, unique entitlement-per-receipt index, and conflict mapping are present. Forced race verification and cross-receipt student-period serialization are missing.

### Phase 2

Status: **PARTIALLY CONFIRMED**.

Snapshot persistence, backfill, required columns, response projection, and duration use are present. Monetary precision/range validation is incomplete.

### Phase 3

Status: **PARTIALLY CONFIRMED**.

New uploads use validated private storage and protected streaming. Historical storage migration is incomplete and pre-decode resource limits need hardening.

### Phase 4

Status: **PARTIALLY CONFIRMED**.

Typed permissions and endpoint policies are present with sensible role separation. End-to-end login/refresh permission issuance is not covered by the HTTP tests inspected.

### Phase 5

Status: **PARTIALLY CONFIRMED**.

Append-only audit and scoped idempotency are materially implemented. Concurrent idempotency and runtime trigger behavior were not independently executed.

### Phase 6

Status: **CONFIRMED STATICALLY / BLOCKED AT RUNTIME**.

Cursor structure, validation, deterministic ordering, `limit + 1`, page metadata, equal timestamps, and query projections are present. Runtime endpoint status assertions and query plans remain unverified.

### Phase 7

Status: **PARTIALLY CONFIRMED**.

Indexes and cache behavior align with the primary queries, but no `EXPLAIN ANALYZE` exists as independent evidence and the application-service refactor is mostly indirection.

### Phase 8

Status: **PARTIALLY CONFIRMED**.

HTTP hardening, health separation, custom telemetry, and layered rate limits exist. Npgsql spans and production exporter behavior are not established.

### Phase 9

Status: **PARTIALLY CONFIRMED**.

Database reset, five factories, snapshots, and CI YAML exist. EF/runtime/CI execution was not independently reproduced.

## 10. Build and Test Results

| Check | Independent result |
|---|---|
| `dotnet restore Adeeb.slnx` | BLOCKED FROM RUNTIME VERIFICATION |
| Release build | BLOCKED FROM RUNTIME VERIFICATION |
| Warnings-as-errors build | BLOCKED FROM RUNTIME VERIFICATION |
| `dotnet format --verify-no-changes` | BLOCKED FROM RUNTIME VERIFICATION |
| Full solution tests | BLOCKED FROM RUNTIME VERIFICATION |
| API publish | BLOCKED FROM RUNTIME VERIFICATION |
| Docker version/info | BLOCKED FROM RUNTIME VERIFICATION |
| Testcontainers integration suite ×3 | BLOCKED FROM RUNTIME VERIFICATION |
| k6 smoke | BLOCKED FROM RUNTIME VERIFICATION |
| Dependency vulnerability audit | BLOCKED FROM RUNTIME VERIFICATION |

The previous report claims successful restore/build/format/publish, 150 module/unit/architecture tests, 14 non-container integration tests, and five pending-model checks. It explicitly states that 17 Testcontainers tests, k6, and the completed dependency audit were blocked. Those statements were not promoted to independent `CONFIRMED` status.

## 11. Test Quality Review

### Concurrency tests

- Call real `CommerceService` production code: yes.
- Use PostgreSQL through Testcontainers: yes.
- Verify final receipt, entitlement, and audit state: yes.
- Deterministically force a race: no.
- Could pass with broken optimistic concurrency: partially yes, because `ReceiptAlreadyReviewed` is accepted after serialized execution.

### Idempotency tests

- Sequential duplicate retry verifies only one upload with a fake processor/storage and EF InMemory.
- PostgreSQL test verifies student-scoped duplicate keys and cursor pagination.
- A simultaneous same-key PostgreSQL upload test with final object-count verification was not found.

### Permission tests

- Exercise real endpoints and verify 401/403/200: yes.
- Verify final database state for finance approval: yes.
- Use production login/refresh claim generation: no; JWT permissions are forged by the test.

### File validation tests

- Exercise real ImageSharp processing: yes.
- Verify fake/corrupt rejection, dimensions, WebP output, and metadata removal: yes.
- DB cleanup uses EF InMemory plus an interceptor and fake storage: useful but not a provider-level integration test.
- S3 behavior, read authorization, readiness, and orphan cleanup are not covered by the inspected tests.

### Pagination tests

- Use PostgreSQL: yes.
- Same fixed timestamp across rows: yes.
- Verify no duplicates across pages: yes.
- Do not verify ordering explicitly, invalid endpoint responses, `HasMore`, or every cursor edge case.

### Architecture tests

- Inspect real assemblies and useful dependency boundaries: yes.
- Endpoint persistence prohibition is meaningful.
- Tests do not detect wrapper-only use cases or the large central service, so the refactoring claim can pass without actual decomposition.

No skipped attributes were found in the critical tests inspected. The full-suite skip/conditional inventory remains runtime/checkout blocked.

## 12. Migration and EF Model Review

Confirmed statically:

- PostgreSQL `xmin` mapping is represented by the `uint Version` row-version model and snapshot column name `xmin`.
- Unique entitlement source-receipt index exists.
- Snapshot migration performs nullable-add, backfill, validation block, and non-null conversion.
- Audit migration creates an append-only update/delete trigger.
- Idempotency migration changes global uniqueness to `(student_id, idempotency_key)` and backfills fingerprints.
- Query indexes match the principal student/status/created cursor paths.
- All five design-time factories exist and use environment-variable overrides with passwordless localhost defaults.

Not confirmed:

- Actual migration application against a fresh and upgraded PostgreSQL database.
- Pending-model checks for all five contexts.
- Physical legacy-file migration, which is absent from the EF migration and must be handled operationally.
- Query-plan benefit and index selectivity.

## 13. Security and Secret Scan

Changed-file review classification:

| Match/type | Classification |
|---|---|
| Empty JSON connection strings and signing key | Safe configuration placeholders |
| Design-time `Username=postgres` without password | Local placeholder; not a credential secret |
| `adeeb-tests-password` in Testcontainers | Deterministic local-only test credential |
| `integration-tests-signing-key-32-bytes` | Deterministic test-only signing key |
| S3 AccessKey/SecretKey property names | Configuration schema, not secret values |
| Private keys / AWS `AKIA...` / credentialed URLs | No match observed in reviewed changed files |

No real unsafe secret was found in the reviewed branch changes. A complete independent tracked-file scan is still blocked without the full checkout, so the overall secret-scan claim remains partial.

## 14. CI Workflow Review

Confirmed statically:

- Root workflow path is correct.
- Default working directory is `ADEEB-V2`.
- Restore precedes format/build/test.
- Docker is a hard gate.
- Build uses `-warnaserror`.
- Tests are not conditionally skipped and collect XPlat coverage.
- Coverage artifact path includes the `ADEEB-V2` prefix required by the repository-root artifact action.
- Dependency audit and API publish paths are plausible and correspond to changed project paths.
- Concurrency cancellation is configured.

Gaps:

- No workflow run was observed.
- No PR exists, and push CI only targets `main`.
- No explicit least-privilege `permissions` block.
- No job timeout.
- Branch protection and required checks are unknown.
- Docker/Testcontainers success on GitHub runner is expected but unobserved.

CI status: **PARTIALLY CONFIRMED / NOT EXECUTED**.

## 15. Production Readiness Scores

| Area | Score |
|---|---:|
| Architecture | 6/10 |
| Domain correctness | 7/10 |
| Concurrency safety | 6/10 |
| Financial integrity | 5/10 |
| Security | 6/10 |
| Authorization | 7/10 |
| File storage safety | 5/10 |
| Database design | 7/10 |
| Performance | 6/10 |
| API design | 7/10 |
| Observability | 6/10 |
| Test quality | 6/10 |
| CI/CD readiness | 5/10 |
| Production readiness | 5/10 |

No area received 10/10 because critical runtime tests, CI, branch protection, load testing, query plans, and PostgreSQL security-sensitive execution were not independently confirmed.

## 16. Confirmed Strengths

- Snapshot data is actually persisted and used, not merely documented.
- Single-receipt final-state transition has layered domain, EF, and database protection.
- Audit records share the Commerce DbContext and transaction with financial changes.
- New receipt uploads are decoded, re-encoded to WebP, and stripped of major metadata profiles.
- New local receipt objects are outside `wwwroot`, with traversal protection.
- S3 implementation is functional rather than an empty stub.
- Permission separation avoids granting legacy Admin roles finance access.
- Cursor queries use deterministic compound ordering and projection.
- Integration reset covers all five schemas and uses PostgreSQL/Testcontainers.
- CI YAML does not pretend Docker integration tests passed when Docker is absent.

## 17. Remaining Risks

- Paid entitlement periods can overlap under cross-receipt concurrency.
- Historical receipt confidentiality/availability is not migrated.
- Full runtime correctness and migration consistency are unverified on the reviewed commit.
- File decompression can consume resources before dimension rejection.
- Login/refresh permission behavior is not proven end to end.
- Database tracing is incomplete.
- God-service complexity increases regression and transaction-boundary risk.
- Production proxy, CORS, AllowedHosts, OTLP, S3, and secret injection remain deployment responsibilities.

## 18. Required Actions Before Production

1. Serialize paid-period allocation per student and add a deterministic two-receipt PostgreSQL race test.
2. Implement and verify a resumable legacy-file migration from public storage to private storage.
3. Add explicit monetary precision/range validation and tests.
4. Add pre-decode image identification/limits and adversarial decompression tests.
5. Add real login and refresh HTTP tests for every administrative role and permission boundary.
6. Add simultaneous same-key PostgreSQL idempotency tests that verify database rows and storage object count.
7. Add Npgsql OpenTelemetry instrumentation and verify exported database spans.
8. Replace mandatory static S3 credentials with the AWS default credential chain/workload identity support.
9. Decompose `CommerceService` into real focused application handlers/services with explicit transaction ownership.
10. Add explicit CI `permissions: contents: read` and a reasonable timeout.
11. Update the branch from `main`, open a PR, and run the complete CI workflow on the exact final commit.
12. Run the integration suite at least three times, k6, dependency audit, five EF pending-model checks, and representative `EXPLAIN (ANALYZE, BUFFERS)` queries.
13. Verify branch protection and required checks before merge.

## 19. Final Recommendation

**NO-GO for production in the current independently verified state.**

Phase 1–9 is materially implemented but only partially confirmed. There are no proven Critical defects, but two High defects affect financial duration correctness and historical receipt confidentiality/availability. After those are fixed, the branch still requires an observed green PostgreSQL/Testcontainers CI run and the blocked verification steps before production approval.

