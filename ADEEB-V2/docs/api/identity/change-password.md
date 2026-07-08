---
id: Auth.ChangePassword
title: Change Password
module: Identity
method: POST
route: /api/v2/auth/change-password
status: Stable
auth: Bearer
frontendReady: true
order: 90
---
# Change Password

## 1. Endpoint
`POST /api/v2/auth/change-password`

## 2. Purpose
Current password-ро тасдиқ мекунад, password-и навро hash мекунад ва session-ҳои дигарро revoke мекунад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Bearer required.

## 6. Authorization
Authenticated user only.

## 7. Rate Limit
Default: 3 requests / 10 minutes.

## 8. Localization
Validation/auth errors localized мешаванд.

## 9. Request Headers
`Authorization: Bearer <access-token>`

## 10. Path Parameters
Not applicable.

## 11. Query Parameters
Not applicable.

## 12. Request Body
```json
{
  "currentPassword": "OldPassword123",
  "newPassword": "NewPassword123"
}
```

## 13. Field Rules
- `currentPassword`: required.
- `newPassword`: min 8, uppercase, lowercase, digit required, symbol optional.

## 14. Success Response
`204 No Content`. Current session survives; other sessions are revoked.

## 15. Error Responses
- `401` bad current password or invalid token.
- `422` validation failure.
- `429` rate limit.

## 16. Stable Error Codes
- `auth.invalid_credentials`
- `validation.failed`
- `auth.password.required`
- `auth.password.policy`
- `rate_limit.too_many_requests`

## 17. Frontend Behavior
On success, keep current user logged in. Show success message and optionally notify that other devices were logged out.

## 18. Retry Policy
Do not auto retry bad password/validation. If token expired, refresh once before submitting.

## 19. Caching
Not applicable.

## 20. Idempotency
Not idempotent.

## 21. Security Notes
Never log passwords. Clear password form fields after response.

## 22. Example Flow
Settings -> enter current/new password -> success -> stay logged in.

## 23. Related Endpoints
- [Current User](/docs/api/identity/me)
- [Logout All](/docs/api/identity/logout-all)

## 24. Change History
2026-07-08 - Initial V2 documentation.
