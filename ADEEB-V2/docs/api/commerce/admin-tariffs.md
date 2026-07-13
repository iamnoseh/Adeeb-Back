---
id: Commerce.AdminTariffs
title: Admin Commerce Tariffs
method: GET
route: /api/v2/admin/commerce/tariffs
status: stable
---

## 1. Endpoint
`GET /api/v2/admin/commerce/tariffs`
## 2. Purpose
Lists all tariffs, including draft and archived tariffs.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Bearer access token required.
## 6. Authorization
`commerce.tariffs.view` permission.
## 7. Rate Limit
Default admin limits.
## 8. Localization
Errors use request localization.
## 9. Request Headers
`Authorization: Bearer {token}`.
## 10. Path Parameters
Not applicable.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Not applicable.
## 13. Field Rules
Not applicable.
## 14. Success Response
`200 OK` array of tariffs.
## 15. Error Responses
`401`, `403` ProblemDetails.
## 16. Stable Error Codes
Not applicable.
## 17. Frontend Behavior
Admin UI uses this to manage visible QR tariffs.
## 18. Retry Policy
Safe to retry.
## 19. Caching
Refresh after tariff creation.
## 20. Idempotency
Read-only.
## 21. Security Notes
Only content admins can view inactive tariffs.
## 22. Example Flow
Admin opens commerce settings and sees tariffs.
## 23. Related Endpoints
`POST /api/v2/admin/commerce/tariffs`.
## 24. Change History
2026-07-11: Added admin tariff list.
