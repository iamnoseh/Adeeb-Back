# ADR-002 Payment Snapshot

## Context

Tariff names, prices, currencies, and durations may change after a student submits a payment receipt. Financial history must preserve what the student selected and what an administrator reviewed at submission time.

## Problem

Rendering historical receipts from the current tariff row silently rewrites history. It can also change entitlement duration after payment without changing the receipt itself.

## Decision

Persist an immutable tariff snapshot on each payment receipt: tariff name, price, ISO-style currency code, and duration days. Receipt creation copies normalized values from the active tariff. Review and entitlement calculation use the receipt snapshot, never the current tariff.

Price columns use explicit `numeric(18,2)` precision. Currency is trimmed, uppercased, and restricted initially to `TJS`, `USD`, and `RUB`. Historical Commerce entities use restrictive/no-action foreign-key deletion behavior and are archived rather than hard-deleted.

## Alternatives Considered

- Always join the current tariff: rejected because it mutates historical meaning.
- Serialize the full tariff as JSON only: rejected because core financial fields need typed constraints, indexes, and stable contracts.
- Version every tariff row and retain all versions: useful later, but more complex than the required receipt-level evidence.

## Consequences

Receipt rows duplicate a small amount of tariff data in exchange for historical integrity. List and details contracts expose snapshot values. Snapshot properties have no public mutation path after receipt creation.

## Migration Plan

1. Add nullable snapshot columns.
2. Backfill from each receipt's referenced tariff.
3. Fail migration or deployment validation if a trustworthy source tariff is missing; do not invent financial data silently.
4. Make snapshot columns required and add constraints.
5. Update creation, review, responses, and tests.

## Rollback Plan

Keep populated snapshot columns if application code is rolled back. Dropping them is destructive and requires an explicit archival/export decision. Existing receipt history must never be reconstructed from mutable tariff values after snapshot data exists.
