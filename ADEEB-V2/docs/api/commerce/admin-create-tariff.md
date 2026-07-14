---
id: Commerce.AdminCreateTariff
title: Admin Create Commerce Tariff
method: POST
route: /api/v2/admin/commerce/tariffs
status: stable
---

## 1. Endpoint
`POST /api/v2/admin/commerce/tariffs`
## 2. Purpose
Creates a tariff with a QR-code image that students scan to pay externally.
## 3. Status
Stable.
## 4. Module
Commerce.
## 5. Authentication
Bearer access token required.
## 6. Authorization
`commerce.tariffs.manage` permission.
## 7. Rate Limit
Default admin limits.
## 8. Localization
Errors use request localization.
## 9. Request Headers
`Authorization: Bearer {token}`, `Content-Type: multipart/form-data`.
## 10. Path Parameters
Not applicable.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Multipart fields: `Name`, `Price`, `Currency`, `DurationDays`, optional `Status`, required `QrImage`.
## 13. Field Rules
`Price` must be greater than zero, no greater than `9999999999999999.99`, and contain at most two fractional digits. Values with additional fractional precision are rejected rather than rounded.
Price and duration must be positive. Currency is a 3-letter code. QR image is required; jpg, jpeg, png, webp; max 10 MB.
## 14. Success Response
`200 OK` created tariff.
## 15. Error Responses
`401`, `403`, `422` ProblemDetails.
## 16. Stable Error Codes
`commerce.tariff.name.invalid`, `commerce.tariff.price.invalid`, `commerce.tariff.currency.invalid`, `commerce.tariff.duration.invalid`, `commerce.tariff.qr_image.required`, `commerce.qr_image.invalid_type`, `commerce.image.too_large`.
## 17. Frontend Behavior
Admin uploads the QR image received from bank/payment provider and students later scan it.
## 18. Retry Policy
Do not blindly retry after unknown upload result; reload tariff list first.
## 19. Caching
Invalidate tariff list after success.
## 20. Idempotency
Not idempotent.
## 21. Security Notes
QR image file name is generated server-side.
## 22. Example Flow
Admin creates a 30-day premium tariff with QR image.
## 23. Related Endpoints
`GET /api/v2/commerce/tariffs`.
## 24. Change History
2026-07-11: Added admin tariff creation.
