# ADR-001 Commerce Concurrency

## Context

Payment receipt review is a critical mutation. Approval may also create a student entitlement and an audit record. Concurrent reviewers must not produce two decisions or duplicate entitlements.

## Problem

Application-only status checks are subject to races. Two requests can load the same pending receipt, both transition it, and both attempt downstream writes before either observes the other.

## Decision

Use PostgreSQL-backed optimistic concurrency for `PaymentReceipt`, preferring the Npgsql `xmin` row version mapping supported by the installed provider. Execute receipt transition, entitlement creation, audit creation, and persistence in one explicit transaction.

Entitlement-period mutations additionally acquire a transaction-scoped PostgreSQL advisory lock derived from `StudentId`. The lock is acquired before reading the latest active expiry. This serializes purchases for one student across API instances while allowing different students to proceed independently.

Domain transition methods return stable `Result` failures for expected final-state conflicts. Persistence maps concurrency and relevant unique-constraint failures to stable `409 Conflict` errors. A filtered unique index on non-null `StudentEntitlement.SourcePaymentReceiptId` is the final duplicate-entitlement guard.

Concurrency tests use separate scopes and DbContexts against real PostgreSQL, coordinate simultaneous starts, and assert responses plus final receipt, entitlement, and audit state.

## Alternatives Considered

- In-memory locks: rejected because they do not coordinate multiple API instances.
- Serializable isolation for every Commerce mutation: rejected as unnecessarily broad and likely to increase contention.
- Pessimistic row locks only: viable, but optimistic concurrency plus a uniqueness constraint gives explicit conflict behavior with less lock duration.
- In-memory per-student locks: rejected because they do not coordinate multiple API instances.
- Application-only idempotency: rejected because it cannot replace database invariants.

## Consequences

Concurrent losers receive a deterministic conflict. Review code must translate `DbUpdateConcurrencyException` and named PostgreSQL constraints. Migrations and integration tests are required before this behavior is considered complete.

## Migration Plan

1. Add the receipt concurrency mapping.
2. Add nullable `SourcePaymentReceiptId`.
3. Backfill only when a trustworthy relationship exists.
4. Add the filtered unique index.
5. Introduce transactional review behavior and PostgreSQL concurrency tests.

## Rollback Plan

Application behavior can be rolled back while retaining additive columns and indexes. Removing the unique index or concurrency mapping requires a deliberate migration and a prior duplicate-data check; it must not be done while concurrent review endpoints are active.
