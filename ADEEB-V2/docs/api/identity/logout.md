---
id: Auth.Logout
title: Logout Current Session
module: Identity
method: POST
route: /api/v2/auth/logout
status: Stable
auth: Bearer
frontendReady: true
order: 40
---
# Logout Current Session

## 1. Endpoint
`POST /api/v2/auth/logout`

## 2. Purpose
Current JWT session (`sid`) revoke мекунад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Bearer access token required.

## 6. Authorization
Authenticated user only.

## 7. Rate Limit
No named route-specific rate limit.

## 8. Localization
Auth errors localized мешаванд.

## 9. Request Headers
- `Authorization: Bearer <access-token>`
- Optional: `Accept-Language`, `X-Adeeb-Language`

## 10. Path Parameters
Not applicable.

## 11. Query Parameters
Not applicable.

## 12. Request Body
Not applicable.

## 13. Field Rules
Not applicable.

## 14. Success Response
`204 No Content`.

## 15. Error Responses
- `401` missing/invalid access token.

## 16. Stable Error Codes
- `auth.invalid_credentials`

## 17. Frontend Behavior
On `204`, clear local access/refresh tokens and navigate to login/onboarding.

## 18. Retry Policy
Can retry once if network failed before response. Do not refresh token just to logout.

## 19. Caching
Do not cache.

## 20. Idempotency
Practical idempotency: already revoked session has no useful client-side difference.

## 21. Security Notes
Logout revokes server session; client must also delete local tokens.

## 22. Example Flow
User taps logout -> call endpoint -> clear secure storage -> navigate out.

## 23. Related Endpoints
- [Logout All](/docs/api/identity/logout-all)
- [Sessions](/docs/api/identity/sessions)

## 24. Change History
2026-07-08 - Initial V2 documentation.
