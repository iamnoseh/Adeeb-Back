# Identity Module Instructions

These rules apply to `src/Modules/Identity/**` and Identity-specific tests/docs. They add to the root `AGENTS.md`; they do not replace it.

## Current Scope

Identity currently owns registration, email-or-phone login, JWT access tokens, refresh sessions, refresh rotation, token family reuse detection, logout, logout-all, session listing, session revocation, current user, change password, superadmin seeding, Identity PostgreSQL schema/tables, and Identity route documentation.

## Security Invariants

- Never store raw refresh tokens; persist only the hash produced by `RefreshTokenGenerator.Hash`.
- Never log passwords, access tokens, refresh tokens, token hashes, password hashes, authorization headers, or JWT signing keys.
- JWTs stay short-lived and must not carry dashboard/profile/business state beyond required identity/session claims.
- Login supports `identifier` for email or phone. Do not regress to email-only login.
- Device metadata is optional in request bodies. If omitted, derive it from client context/headers so Swagger and web clients work without manual device fields.
- Change password must keep the current session alive and revoke other active sessions.

## Refresh Rotation

Refresh flow is security-critical. Preserve:

- token hash lookup;
- transaction and row locking behavior;
- one successful rotation per refresh token;
- replaced-session lineage;
- reuse detection for already-rotated tokens;
- family revocation on reuse;
- generic auth failure returned to clients on invalid/reused tokens.

Two concurrent refresh attempts with the same token must not both succeed. Any refresh-flow change requires tests and documentation updates.

## Authorization And Ownership

Every protected Identity operation must derive `userId` and `sessionId` from claims, not from trusted client input. Any route accepting a session ID must verify that the session belongs to the authenticated user before mutation or disclosure.

Roles currently include `User` and `SuperAdmin`. Adding role behavior requires explicit authorization policy/design, tests, and docs.

## Persistence

Identity owns schema `identity` and tables `users` and `auth_sessions`. Keep migrations in `Adeeb.Modules.Identity/Infrastructure/Persistence/Migrations`. Do not modify applied migrations casually. Add indexes/constraints for real invariants such as normalized email, normalized phone, token hash lookup, session family lookup, and active-session queries.

Canonical timestamps are UTC. Add Dushanbe projections only in public responses where useful.

## Errors And Localization

Use `IdentityErrors`, `Validation`, and `ProblemDetailsMapper` conventions. Stable API error codes are lower-case dotted strings such as `auth.invalid_credentials`; resource keys are strings such as `Auth.InvalidCredentials`. Add messages for all supported cultures when introducing new frontend-visible errors.

## Documentation And Tests

Identity route changes must update `docs/api/identity/...` and any affected flow/frontend docs. Add or update architecture documentation coverage when a stable route is added.

Prefer unit tests for token generation/hash behavior, password policy, language parsing, validation, error mapping, and domain session decisions. Use PostgreSQL/Testcontainers integration tests for refresh rotation, concurrency, unique constraints, and cross-user session/IDOR behavior when Docker support is available.
