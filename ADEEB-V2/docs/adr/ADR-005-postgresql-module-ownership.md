# ADR-005 PostgreSQL Module Ownership

The Identity module owns the `identity` PostgreSQL schema and maps `identity.users` and `identity.auth_sessions` explicitly with EF Core configurations.
