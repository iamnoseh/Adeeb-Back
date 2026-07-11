---
id: Commerce.AdminRejectPaymentReceipt
title: Admin Reject Payment Receipt
method: POST
route: /api/v2/admin/commerce/payment-receipts/{receiptId}/reject
status: stable
---

## 1. Endpoint
`POST /api/v2/admin/commerce/payment-receipts/{receiptId}/reject`
## 2. Purpose
Rejects a submitted receipt without granting premium.
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
`Authorization: Bearer {token}`, `Content-Type: application/json`.
## 10. Path Parameters
`receiptId`.
## 11. Query Parameters
Not applicable.
## 12. Request Body
`{ "note": "optional rejection note" }`.
## 13. Field Rules
Note is optional and max 512 characters.
## 14. Success Response
`200 OK` receipt with `status: Rejected`.
## 15. Error Responses
`401`, `403`, `404`, `409`, `422` ProblemDetails.
## 16. Stable Error Codes
`commerce.receipt_not_found`, `commerce.receipt_already_reviewed`, `commerce.review_note.invalid`.
## 17. Frontend Behavior
After reject, student remains Free unless another active entitlement exists.
## 18. Retry Policy
Safe to retry only if the first result is unknown; reviewed receipts return conflict.
## 19. Caching
Refresh admin receipt list.
## 20. Idempotency
Rejecting an already reviewed receipt returns conflict.
## 21. Security Notes
Reject does not delete uploaded evidence.
## 22. Example Flow
Admin sees unreadable check and rejects with note.
## 23. Related Endpoints
`GET /api/v2/admin/commerce/payment-receipts`.
## 24. Change History
2026-07-11: Added receipt rejection.
