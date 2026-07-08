---
id: Auth.GetSessions
title: Get Sessions
module: Identity
method: GET
route: /api/v2/auth/sessions
status: Stable
auth: Bearer
frontendReady: true
order: 60
---
# Get Sessions

## 1. Endpoint
`GET /api/v2/auth/sessions`

## 2. Purpose
Session-ҳои active-и current user-ро бе secrets бармегардонад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Bearer required.

## 6. Authorization
User can see only own sessions.

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
```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "deviceName": "Chrome on Windows",
      "platform": "web",
      "createdAtUtc": "2026-07-08T07:37:27+00:00",
      "createdAtDushanbe": "2026-07-08T12:37:27+05:00",
      "lastUsedAtUtc": "2026-07-08T07:37:27+00:00",
      "lastUsedAtDushanbe": "2026-07-08T12:37:27+05:00",
      "isCurrent": true
    }
  ]
}
```

## 15. Error Responses
`401` missing/invalid access token.

## 16. Stable Error Codes
- `auth.invalid_credentials`

## 17. Frontend Behavior
Display sessions in security settings. Use `isCurrent` to mark current device. Never expect refresh token data here.

## 18. Retry Policy
If access token expired, refresh once then retry.

## 19. Caching
Do not persistently cache; short in-memory UI cache is acceptable.

## 20. Idempotency
Safe read.

## 21. Security Notes
No token/hash/security internals are returned.

## 22. Example Flow
Open security settings -> fetch sessions -> user may revoke a device.

## 23. Related Endpoints
- [Revoke Session](/docs/api/identity/revoke-session)
- [Logout All](/docs/api/identity/logout-all)

## 24. Change History
2026-07-08 - Initial V2 documentation.
