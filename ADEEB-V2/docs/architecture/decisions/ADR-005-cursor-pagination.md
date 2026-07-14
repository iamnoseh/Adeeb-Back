# ADR-005 Cursor Pagination

## Context

Receipt and tariff lists must remain predictable as tables grow and new rows are inserted during navigation.

## Problem

Offset pagination becomes increasingly expensive and can duplicate or skip rows when concurrent inserts change offsets. Unvalidated filters create ambiguous contracts and unstable query plans.

## Decision

Use opaque base64-encoded cursors and keyset pagination. Payment receipt cursors contain `CreatedAtUtc` and `Id`; queries order by both descending, apply the matching tuple boundary, and fetch `limit + 1`. Default limit is 30 and accepted values are 1 through 100.

Parse enums, dates, ranges, limits, and cursor payloads before querying. Invalid values return `422` with stable codes. List queries use `AsNoTracking` and project directly into lightweight list DTOs before materialization. Detail DTOs remain separate.

Idempotency keys are scoped by actor and operation. A SHA-256 request fingerprint distinguishes same-key retries from payload mismatches; same payload returns the prior resource and different payload returns `409`.

## Alternatives Considered

- Offset/limit pagination: rejected for mutable high-volume lists.
- Timestamp-only cursor: rejected because timestamps are not unique.
- Expose raw database IDs as cursors: rejected because the ordering boundary requires both fields and the contract should remain opaque.
- Silently ignore malformed filters: rejected because it hides client defects.

## Consequences

Clients navigate forward with `NextCursor` rather than page numbers. Sort order becomes part of the contract. Cursor decoding and boundary edge cases require focused tests, especially equal timestamps and records inserted between requests.

## Migration Plan

1. Add shared typed page contracts and a Commerce cursor codec.
2. Add validated filter models and stable errors.
3. Introduce projected keyset queries while preserving existing detail endpoints.
4. Update API documentation and contract tests.
5. Deprecate any offset parameters only after clients can migrate.

## Rollback Plan

Keep cursor-capable contracts additive during rollout. If a query regression occurs, disable cursor endpoints behind routing/version configuration and retain the previous bounded list temporarily. Do not reinterpret issued cursor payloads with different ordering semantics.
