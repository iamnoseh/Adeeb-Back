---
id: Auth.RevokeSession
title: Revoke Session
module: Identity
method: DELETE
route: /api/v2/auth/sessions/{sessionId}
status: Stable
auth: Bearer
frontendReady: true
order: 70
---
# Revoke Session

## 1. Endpoint
`DELETE /api/v2/auth/sessions/{sessionId}`

## 2. Purpose
User метавонад танҳо session-и худи худро revoke кунад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Bearer required.

## 6. Authorization
Own session only; IDOR prevented by user ownership check.

## 7. Rate Limit
No named route-specific rate limit.

## 8. Localization
Errors localized мешаванд.

## 9. Request Headers
`Authorization: Bearer <access-token>`

## 10. Path Parameters
- `sessionId`: GUID.

## 11. Query Parameters
Not applicable.

## 12. Request Body
Not applicable.

## 13. Field Rules
`sessionId` must be a GUID.

## 14. Success Response
`204 No Content`.

## 15. Error Responses
- `401` unauthenticated.
- `403` another user's session.
- `404` session not found.

## 16. Stable Error Codes
- `auth.invalid_credentials`
- `common.forbidden`
- `common.not_found`

## 17. Frontend Behavior
On `204`, remove revoked session from UI list. If current session revoked, clear tokens and navigate to login.

## 18. Retry Policy
Manual retry only. If token expired, refresh once then retry.

## 19. Caching
Invalidate sessions list.

## 20. Idempotency
Revoking an already revoked session may behave as not found depending on current list state.

## 21. Security Notes
Never allow client to revoke arbitrary IDs outside own account.

## 22. Example Flow
Fetch sessions -> user selects device -> delete by session id -> refresh list.

## 23. Related Endpoints
- [Sessions](/docs/api/identity/sessions)
- [Logout Current Session](/docs/api/identity/logout)

## 24. Change History
2026-07-08 - Initial V2 documentation.
