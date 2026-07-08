# Auth Sessions

Refresh tokens are opaque, high-entropy, URL-safe values. Only SHA-256 hashes are stored.

Each login/register creates a device-aware `AuthSession` with a `FamilyId`. Refresh rotates by revoking the current session row and creating a replacement in the same family inside a database transaction using row locking.

If a previously rotated token is used again, the active sessions in that token family are revoked and a structured `auth.refresh.reuse_detected` security event is emitted.

Clients may send explicit device data in the JSON body. If omitted, the API derives a best-effort device from request metadata. Optional headers are supported for web/mobile clients:

- `X-Adeeb-Device-Id`
- `X-Adeeb-Device-Name`
- `X-Adeeb-Platform`
- `X-Adeeb-App-Version`

When these headers are absent, the API uses `User-Agent` and IP to create a deterministic fallback device id for Swagger/browser use.
