# ADR-004 Permission Authorization

## Context

Generic administrator roles are too broad for financial operations. Content administrators, finance administrators, and support staff need different Commerce capabilities.

## Problem

Role-only endpoint checks make least privilege difficult, encourage role proliferation, and can accidentally allow content staff to approve payments. Critical mutations also require immutable, attributable audit evidence.

## Decision

Authorize administrative operations with typed permission constants and policies whose names equal permission values. Roles map to permissions during principal construction; endpoints depend on permissions, not role names. Student self-access remains ownership-based.

Finance administrators can manage tariffs, review receipts, and grant entitlements. Support administrators may view receipts but cannot review them. Content administrators receive no Commerce review permission. Super administrators receive all registered permissions.

Create append-only audit records for security-sensitive mutations and protected receipt-image access. Audit creation shares the business transaction where a database mutation occurs. Records include actor, action, resource, correlation ID, trusted client IP, user agent, sanitized before/after data, and UTC time. No update/delete API is provided, and secrets or raw financial images are never captured.

## Alternatives Considered

- Continue using broad roles at endpoints: rejected because it violates least privilege.
- Store permissions only in a mutable external ACL service: rejected as an unnecessary distributed dependency for the modular monolith.
- Log-only audit: rejected because operational logs are mutable, retention-dependent, and not transactionally tied to business state.

## Consequences

Permission claim issuance and role mapping become security-critical and require matrix tests. Permission changes affect newly issued principals according to token lifetime. Audit storage grows append-only and needs retention/export planning without mutation.

## Migration Plan

1. Define typed permissions and role mappings.
2. Register policies and issue permission claims.
3. Add authorization tests before replacing role checks.
4. Add audit persistence and transactional writes.
5. Remove obsolete broad Commerce role checks after endpoint coverage is complete.

## Rollback Plan

Keep audit data and permission constants. Individual endpoints may temporarily use a stricter combined role-and-permission policy during rollback, but must not revert to broader access. Disable a problematic permission mapping by denying access, not by granting a generic administrator bypass.

## Implementation Status

Implemented on 2026-07-13. Commerce endpoints use permission-named policies, JWTs carry mapped permission claims, and only legacy SuperAdmin tokens receive a role fallback. PostgreSQL enforces audit immutability with an update/delete trigger; audit payloads redact credential, token, authorization, image, object-key, card, account, and connection fields.
