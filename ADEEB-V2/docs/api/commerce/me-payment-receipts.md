---
id: Commerce.MePaymentReceipts
title: Current Student Payment Receipts
method: GET
route: /api/v2/commerce/me/payment-receipts
status: stable
---

## 1. Endpoint
`GET /api/v2/commerce/me/payment-receipts`
## 2. Purpose
Lists the authenticated student's own submitted payment receipts.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Bearer access token required.
## 6. Authorization
Authenticated active student self-access only.
## 7. Rate Limit
Default authenticated limits.
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
Invalid status values are ignored and return all current student receipts.
## 14. Success Response
`200 OK` array of payment receipt responses.
## 15. Error Responses
`401 Unauthorized`, `409 Conflict` when no active student persona exists.
## 16. Stable Error Codes
`commerce.student_required`.
## 17. Frontend Behavior
Use this endpoint to show Pending, Approved, and Rejected receipt states after upload.
## 18. Retry Policy
Safe to retry.
## 19. Caching
Refresh after receipt upload and after admin review.
## 20. Idempotency
Read-only.
## 21. Security Notes
The client cannot choose another student id.
## 22. Example Flow
Student uploads a receipt, then opens payment history to see `Pending`.
## 23. Related Endpoints
`POST /api/v2/commerce/tariffs/{tariffId}/payment-receipts`.
## 24. Change History
2026-07-11: Added current student payment receipt history.
