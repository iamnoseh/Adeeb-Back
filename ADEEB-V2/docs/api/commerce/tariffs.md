---
id: Commerce.Tariffs
title: Commerce Tariffs
method: GET
route: /api/v2/commerce/tariffs
status: stable
---

## 1. Endpoint
`GET /api/v2/commerce/tariffs`
## 2. Purpose
Lists active tariffs with QR-code image URLs for students.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Not required.
## 6. Authorization
Public read of active tariffs only.
## 7. Rate Limit
Default platform limits.
## 8. Localization
Errors use request localization.
## 9. Request Headers
Optional `X-Adeeb-Language`.
## 10. Path Parameters
Not applicable.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Not applicable.
## 13. Field Rules
Not applicable.
## 14. Success Response
`200 OK` array of tariffs: `tariffId`, `name`, `price`, `currency`, `durationDays`, `qrImageUrl`, `status`.
## 15. Error Responses
Standard ProblemDetails for unexpected failures.
## 16. Stable Error Codes
Not applicable.
## 17. Frontend Behavior
Show active tariffs and let the student scan `qrImageUrl` to pay outside ADEEB.
## 18. Retry Policy
Safe to retry.
## 19. Caching
May be cached briefly; refresh before payment.
## 20. Idempotency
Read-only.
## 21. Security Notes
QR images are admin-uploaded static files.
## 22. Example Flow
Open premium screen, load tariffs, display QR image.
## 23. Related Endpoints
`POST /api/v2/commerce/tariffs/{tariffId}/payment-receipts`.
## 24. Change History
2026-07-11: Added tariff listing.
