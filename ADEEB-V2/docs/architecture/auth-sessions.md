# Auth Sessions

Refresh tokens are opaque, high-entropy, URL-safe values. Only SHA-256 hashes are stored.

Each login/register creates a device-aware `AuthSession` with a `FamilyId`. Refresh rotates by revoking the current session row and creating a replacement in the same family inside a database transaction using row locking.

If a previously rotated token is used again, the active sessions in that token family are revoked and a structured `auth.refresh.reuse_detected` security event is emitted.
