---
id: Auth.RefreshToken
title: Refresh Token
module: Identity
method: POST
route: /api/v2/auth/refresh
status: Stable
auth: Anonymous
frontendReady: true
order: 30
---
# Refresh Token

## 1. Endpoint
`POST /api/v2/auth/refresh`

## 2. Purpose
Expired/near-expired access token-ро бо refresh token нав мекунад ва refresh token-ро rotate мекунад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Anonymous; refresh token itself is the credential.

## 6. Authorization
Not applicable.

## 7. Rate Limit
Default: 20 requests / minute.

## 8. Localization
Errors localized мешаванд.

## 9. Request Headers
- `Content-Type: application/json`
- Optional localization/device headers.

## 10. Path Parameters
Not applicable.

## 11. Query Parameters
Not applicable.

## 12. Request Body
```json
{
  "refreshToken": "<refresh-token>"
}
```

## 13. Field Rules
- `refreshToken`: required, opaque string returned by register/login/refresh.

## 14. Success Response
`200 OK`; returns new access token and new refresh token. Old refresh token is revoked.

## 15. Error Responses
- `401` invalid, expired, revoked, or reused refresh token.
- `429` too many refresh attempts.

## 16. Stable Error Codes
- `auth.invalid_refresh_token`
- `rate_limit.too_many_requests`

## 17. Frontend Behavior
On `200`, atomically replace stored refresh token and retry the original request once. On `401`, clear auth state and navigate to login.

## 18. Retry Policy
Use single-flight refresh lock. Never send several parallel refresh calls for concurrent 401 responses.

## 19. Caching
Do not cache.

## 20. Idempotency
Not idempotent. A successful refresh invalidates the submitted token.

## 21. Security Notes
Refresh token reuse revokes the affected token family. Never log refresh tokens.

## 22. Example Flow
Access token expired -> one refresh request -> save new tokens -> retry original request once.

## 23. Related Endpoints
- [Login](/docs/api/identity/login)
- [Logout](/docs/api/identity/logout)
- [Refresh Token Flow](/docs/flows/refresh-token-flow)

## 24. Change History
2026-07-08 - Initial V2 documentation.
