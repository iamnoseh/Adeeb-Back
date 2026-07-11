---
id: Commerce.AdminPaymentReceipts
title: Admin Payment Receipts
method: GET
route: /api/v2/admin/commerce/payment-receipts
status: stable
---

## 1. Endpoint
`GET /api/v2/admin/commerce/payment-receipts`
## 2. Purpose
Lists submitted student payment receipt images for admin review.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Bearer access token required.
## 6. Authorization
`ContentAdmin` policy.
## 7. Rate Limit
Default admin limits.
## 8. Localization
Errors use request localization.
## 9. Request Headers
`Authorization: Bearer {token}`.
## 10. Path Parameters
Not applicable.
## 11. Query Parameters
Optional `status`: `1 Pending`, `2 Approved`, `3 Rejected`.
## 12. Request Body
Not applicable.
## 13. Field Rules
Invalid status values are ignored and return all receipts.
## 14. Success Response
`200 OK` array of payment receipts.
## 15. Error Responses
`401`, `403` ProblemDetails.
## 16. Stable Error Codes
Not applicable.
## 17. Frontend Behavior
Admin UI should default to pending receipts.
## 18. Retry Policy
Safe to retry.
## 19. Caching
Refresh after approve/reject.
## 20. Idempotency
Read-only.
## 21. Security Notes
Receipt images may contain personal payment details; show only to admins.
## 22. Example Flow
Admin opens pending checks and reviews the uploaded image.
## 23. Related Endpoints
Approve/reject receipt endpoints.
## 24. Change History
2026-07-11: Added admin receipt list.
