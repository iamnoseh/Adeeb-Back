---
id: Auth.Me
title: Current User
module: Identity
method: GET
route: /api/v2/auth/me
status: Stable
auth: Bearer
frontendReady: true
order: 80
---
# Current User

## 1. Endpoint
`GET /api/v2/auth/me`

## 2. Purpose
Identity-level current user data бармегардонад.

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
Authenticated user's stored preferred language can affect localized errors.

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
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "email": "noseh@example.com",
  "phoneNumber": "+992900000000",
  "firstName": "Noseh",
  "lastName": "Tagaymurodzoda",
  "preferredLanguage": "tg-TJ",
  "role": "User"
}
```

## 15. Error Responses
`401` missing/invalid access token.

## 16. Stable Error Codes
- `auth.invalid_credentials`

## 17. Frontend Behavior
Use to bootstrap authenticated state after app start. Do not treat it as dashboard/profile aggregate.

## 18. Retry Policy
If access token expired, refresh once then retry.

## 19. Caching
In-memory cache acceptable during app session.

## 20. Idempotency
Safe read.

## 21. Security Notes
No secrets returned.

## 22. Example Flow
App start -> load tokens -> call `/me` -> set current user.

## 23. Related Endpoints
- [Login](/docs/api/identity/login)
- [Change Password](/docs/api/identity/change-password)

## 24. Change History
2026-07-08 - Initial V2 documentation.
