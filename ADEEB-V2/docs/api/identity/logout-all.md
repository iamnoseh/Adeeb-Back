---
id: Auth.LogoutAll
title: Logout All Devices
module: Identity
method: POST
route: /api/v2/auth/logout-all
status: Stable
auth: Bearer
frontendReady: true
order: 50
---
# Logout All Devices

## 1. Endpoint
`POST /api/v2/auth/logout-all`

## 2. Purpose
Ҳама session-ҳои active-и user, аз ҷумла current session, revoke мешаванд.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Bearer required.

## 6. Authorization
Authenticated user only.

## 7. Rate Limit
No named route-specific rate limit.

## 8. Localization
Auth errors localized мешаванд.

## 9. Request Headers
`Authorization: Bearer <access-token>`

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
`401` missing/invalid access token.

## 16. Stable Error Codes
- `auth.invalid_credentials`

## 17. Frontend Behavior
Clear all local tokens. Other devices will fail refresh/auth and must return to login.

## 18. Retry Policy
Manual retry only if network failed.

## 19. Caching
Do not cache.

## 20. Idempotency
Practical idempotency.

## 21. Security Notes
Use for account security actions such as suspected compromise.

## 22. Example Flow
Security settings -> logout all -> clear local tokens -> login screen.

## 23. Related Endpoints
- [Sessions](/docs/api/identity/sessions)
- [Revoke Session](/docs/api/identity/revoke-session)

## 24. Change History
2026-07-08 - Initial V2 documentation.
