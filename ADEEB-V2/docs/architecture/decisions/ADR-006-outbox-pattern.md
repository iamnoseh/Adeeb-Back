# ADR-006 Outbox Pattern

## Context

ADEEB is a modular monolith today. Some future workflows may need reliable asynchronous side effects across module boundaries or external systems without turning a database commit and message publication into an unsafe dual write.

## Problem

Publishing a message before commit can expose state that later rolls back. Publishing after commit can lose the message if the process stops between commit and publication.

## Decision

When a use case first requires reliable asynchronous delivery, write an outbox message in the owning module's PostgreSQL transaction alongside the business mutation. A background worker claims committed messages, publishes them through an application-level contract, and records success with retry metadata.

Messages carry a stable ID, type, version, occurred time, correlation ID, sanitized payload, processing state, attempt count, and next-attempt time. Consumers are idempotent and persist handled-message IDs where side effects are not naturally idempotent. Module contracts must not expose another module's Infrastructure types.

This ADR does not add an event bus or outbox tables during Phase 0. Synchronous in-process calls remain appropriate where atomic cross-module behavior is not required and coupling is explicit.

## Alternatives Considered

- Immediate publish after `SaveChanges`: rejected for reliability-critical side effects because it is a dual write.
- Distributed transactions: rejected due provider complexity and poor fit for HTTP/object-store dependencies.
- Introduce a broker immediately: rejected because no current requirement justifies the operational cost.
- Poll domain tables directly: rejected because it couples publishers to business schema and makes delivery state ambiguous.

## Consequences

Reliable asynchronous behavior gains eventual consistency, retries, duplicate delivery, retention, and monitoring concerns. Message schemas require versioning and sensitive-data review. The worker can be scaled independently while PostgreSQL remains the source of truth.

## Migration Plan

1. Identify a concrete reliability requirement and its owning module.
2. Add module-owned outbox persistence and a non-destructive migration.
3. Add transactional writes and idempotent consumer tests.
4. Add worker retry, locking, metrics, retention, and failure operations.
5. Introduce a broker adapter only when deployment requirements demand it.

## Rollback Plan

Pause publishers and drain or quarantine pending messages before disabling the worker. Retain outbox rows for reconciliation. A synchronous fallback is allowed only when its failure semantics are explicit and it does not recreate an unprotected dual write.
