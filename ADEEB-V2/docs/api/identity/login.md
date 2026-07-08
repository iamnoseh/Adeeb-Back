---
id: Auth.Login
title: Login
module: Identity
method: POST
route: /api/v2/auth/login
status: Stable
auth: Anonymous
frontendReady: true
order: 20
---
# Login

## 1. Endpoint
`POST /api/v2/auth/login`

## 2. Purpose
Бо email ё phone login мекунад, password-ро месанҷад, session-и нав месозад ва access/refresh token медиҳад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Anonymous.

## 6. Authorization
Not applicable.

## 7. Rate Limit
Default: 5 requests / minute by endpoint, IP and anonymous user partition.

## 8. Localization
Public auth errors localized мешаванд; stable `code` тағйир намеёбад.

## 9. Request Headers
- `Content-Type: application/json`
- `Accept-Language: tg-TJ | ru-RU | en-US`
- Optional: `X-Adeeb-Language`
- Optional device headers if body `device` omitted.

## 10. Path Parameters
Not applicable.

## 11. Query Parameters
Not applicable.

## 12. Request Body
```json
{
  "identifier": "noseh@example.com",
  "password": "StrongPassword123"
}
```
Phone login:
```json
{
  "identifier": "+992900000000",
  "password": "StrongPassword123"
}
```
Legacy `email` property is still accepted when `identifier` is absent.

## 13. Field Rules
- `identifier`: required; email or normalized phone.
- `password`: required.
- `device`: optional; explicit device fields follow register rules.

## 14. Success Response
`200 OK`; same auth response shape as register. `user.role` may be `User` or `SuperAdmin`.

## 15. Error Responses
- `401` invalid credentials or non-active account.
- `422` invalid request.
- `429` too many login attempts.

## 16. Stable Error Codes
- `auth.invalid_credentials`
- `auth.account_blocked`
- `validation.failed`
- `auth.identifier.required`
- `auth.identifier.invalid`
- `auth.password.required`
- `rate_limit.too_many_requests`

## 17. Frontend Behavior
On `200`, replace current auth state with returned user/tokens. On `401 auth.invalid_credentials`, show localized title and do not reveal whether account exists. On `429`, disable submit temporarily.

## 18. Retry Policy
No automatic retry. User must submit again.

## 19. Caching
Do not cache.

## 20. Idempotency
Not idempotent: each successful login creates a new auth session.

## 21. Security Notes
Do not log password or tokens. Use `Authorization: Bearer <access-token>` after success.

## 22. Example Flow
Login -> copy `accessToken` -> Swagger Authorize -> call `/api/v2/auth/sessions`.

## 23. Related Endpoints
- [Refresh Token](/docs/api/identity/refresh-token)
- [Sessions](/docs/api/identity/sessions)
- [Logout](/docs/api/identity/logout)

## 24. Change History
2026-07-08 - Initial V2 documentation.
