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
`commerce.payment_receipts.review` permission. Finance administrators may review; content and support administrators may not.
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
`200 OK` receipt with `status: Rejected` and `reviewedByUserId` set to the reviewing admin user id.
## 15. Error Responses
`401`, `403`, `404`, `409`, `422` ProblemDetails.
## 16. Stable Error Codes
`commerce.receipt_not_found`, `commerce.receipt_already_reviewed`, `commerce.receipt_concurrency_conflict`, `commerce.review_note.invalid`, `commerce.reviewer_required`.
## 17. Frontend Behavior
After reject, student remains Free unless another active entitlement exists.
## 18. Retry Policy
Safe to retry only if the first result is unknown; reviewed receipts return conflict.
## 19. Caching
Refresh admin receipt list.
## 20. Idempotency
Review persistence uses a transaction and PostgreSQL optimistic concurrency. Rejecting an already or concurrently reviewed receipt returns `409`.
## 21. Security Notes
Reject does not delete uploaded evidence. Concurrent approve/reject attempts cannot produce two final decisions.
## 22. Example Flow
Admin sees unreadable check and rejects with note.
## 23. Related Endpoints
`GET /api/v2/admin/commerce/payment-receipts`.
## 24. Change History
2026-07-11: Added receipt rejection.
2026-07-13: Added transactional optimistic concurrency.
