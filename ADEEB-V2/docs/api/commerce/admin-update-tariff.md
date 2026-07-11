---
id: Commerce.AdminUpdateTariff
title: Admin Update Commerce Tariff
method: PUT
route: /api/v2/admin/commerce/tariffs/{tariffId}
status: stable
---

## 1. Endpoint
`PUT /api/v2/admin/commerce/tariffs/{tariffId}`
## 2. Purpose
Updates a commerce tariff and optionally replaces its QR-code image.
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
`Authorization: Bearer {token}`, `Content-Type: multipart/form-data`.
## 10. Path Parameters
`tariffId`.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Multipart fields: `Name`, `Price`, `Currency`, `DurationDays`, optional `Status`, optional `QrImage`.
## 13. Field Rules
Price and duration must be positive. Currency is a 3-letter code. If `QrImage` is omitted, the existing QR image remains.
## 14. Success Response
`200 OK` updated tariff.
## 15. Error Responses
`401`, `403`, `404`, `422` ProblemDetails.
## 16. Stable Error Codes
`commerce.tariff_not_found`, `commerce.tariff.name.invalid`, `commerce.tariff.price.invalid`, `commerce.tariff.currency.invalid`, `commerce.tariff.duration.invalid`, `commerce.tariff.status.invalid`, `commerce.qr_image.invalid_type`, `commerce.image.too_large`.
## 17. Frontend Behavior
Admin UI should show current tariff values and only upload a new QR image when replacing it.
## 18. Retry Policy
Do not blindly retry after unknown upload result; reload tariff list first.
## 19. Caching
Invalidate tariff list after success.
## 20. Idempotency
Not idempotent.
## 21. Security Notes
Only content admins may update tariffs.
## 22. Example Flow
Admin edits price and duration while keeping the same QR image.
## 23. Related Endpoints
`GET /api/v2/admin/commerce/tariffs`.
## 24. Change History
2026-07-11: Added admin tariff update.
