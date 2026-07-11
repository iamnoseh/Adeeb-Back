# Commerce Phase 2

Commerce Phase 2 introduces a readiness foundation for premium access without implementing a payment provider, subscription renewal, invoices, refunds, promo codes, market purchases, wallet balances, or automatic premium activation.

The Commerce module owns the `commerce` PostgreSQL schema. It stores student entitlement grants in `commerce.student_entitlements`. The module references Students through explicit application contracts and stores only `StudentId`; it does not create database foreign keys to Students or Identity tables.

Current public behavior is read-only: authenticated students can retrieve their current entitlement summary. If no active premium entitlement exists, the response is `Free`. Premium is reported only when an active entitlement row exists for the current `StudentId`.

Entitlement writes remain intentionally private for now. Future payment-provider callbacks, admin grants, trials, and promo flows must add idempotent write APIs or background handlers with explicit idempotency keys, auditability, and provider verification.
