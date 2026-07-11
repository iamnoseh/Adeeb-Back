---
id: Commerce.AdminApprovePaymentReceipt
title: Admin Approve Payment Receipt
method: POST
route: /api/v2/admin/commerce/payment-receipts/{receiptId}/approve
status: stable
---

## 1. Endpoint
`POST /api/v2/admin/commerce/payment-receipts/{receiptId}/approve`
## 2. Purpose
Approves a submitted receipt and grants premium for the selected tariff duration.
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
`{ "note": "optional admin note" }`.
## 13. Field Rules
Note is optional and max 512 characters.
## 14. Success Response
`200 OK` receipt with `status: Approved`.
## 15. Error Responses
`401`, `403`, `404`, `409`, `422` ProblemDetails.
## 16. Stable Error Codes
`commerce.receipt_not_found`, `commerce.receipt_already_reviewed`, `commerce.review_note.invalid`.
## 17. Frontend Behavior
After approve, refresh receipt list and the student's entitlement state.
## 18. Retry Policy
Safe to retry only if the first result is unknown; reviewed receipts return conflict.
## 19. Caching
Invalidate entitlement summary after success.
## 20. Idempotency
Approval creates a premium entitlement with an internal receipt-based idempotency key.
## 21. Security Notes
Approval is an admin decision, not automated payment verification.
## 22. Example Flow
Admin compares check image with bank record, then approves.
## 23. Related Endpoints
`GET /api/v2/commerce/me/entitlements`.
## 24. Change History
2026-07-11: Added receipt approval.
