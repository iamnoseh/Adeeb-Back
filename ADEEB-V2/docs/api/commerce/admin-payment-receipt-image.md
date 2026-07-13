---
id: Commerce.AdminPaymentReceiptImage
title: Admin Payment Receipt Image
method: GET
route: /api/v2/admin/commerce/payment-receipts/{receiptId}/image
status: stable
---

## 1. Endpoint
`GET /api/v2/admin/commerce/payment-receipts/{receiptId}/image`
## 2. Purpose
Streams validated private payment evidence to an authorized administrator.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Bearer access token required.
## 6. Authorization
Commerce receipt-view permission; legacy `ContentAdmin` authorization remains until the permission migration completes.
## 7. Rate Limit
Admin receipt-view limit.
## 8. Localization
ProblemDetails errors use request localization.
## 9. Request Headers
`Authorization: Bearer {token}`. Byte range headers are supported.
## 10. Path Parameters
`receiptId`: payment receipt identifier.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Not applicable.
## 13. Field Rules
The receipt and attached private object must exist.
## 14. Success Response
`200 OK` or `206 Partial Content`, `Content-Type: image/webp`.
## 15. Error Responses
`401`, `403`, `404` ProblemDetails.
## 16. Stable Error Codes
`commerce.receipt_not_found`, `commerce.receipt.image_not_found`.
## 17. Frontend Behavior
Render only while the authorized review screen is active; do not persist the image in public caches.
## 18. Retry Policy
Safe to retry.
## 19. Caching
Private; clients and proxies must not expose a public cache entry.
## 20. Idempotency
Read-only.
## 21. Security Notes
There is no public static URL. Object keys are internal and never returned in receipt JSON.
## 22. Example Flow
An authorized reviewer opens a pending receipt and the API streams its normalized WebP evidence.
## 23. Related Endpoints
`GET /api/v2/admin/commerce/payment-receipts`, `POST /api/v2/admin/commerce/payment-receipts/{receiptId}/approve`.
## 24. Change History
2026-07-13: Added protected private receipt image streaming.
