# ADR-004 Error Contract

API errors use ProblemDetails with stable machine-readable `code` values. Localized text may change by culture; clients must branch on `code`, not `title`.
