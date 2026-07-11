---
id: Commerce.AdminArchiveTariff
title: Admin Archive Commerce Tariff
method: POST
route: /api/v2/admin/commerce/tariffs/{tariffId}/archive
status: stable
---

## 1. Endpoint
`POST /api/v2/admin/commerce/tariffs/{tariffId}/archive`
## 2. Purpose
Soft-archives a tariff so students no longer see it.
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
`tariffId`.
## 11. Query Parameters
Not applicable.
## 12. Request Body
Not applicable.
## 13. Field Rules
Not applicable.
## 14. Success Response
`200 OK` tariff with `status: Archived`.
## 15. Error Responses
`401`, `403`, `404` ProblemDetails.
## 16. Stable Error Codes
`commerce.tariff_not_found`.
## 17. Frontend Behavior
After archive, hide the tariff from student tariff lists and keep it visible in admin lists.
## 18. Retry Policy
Safe to retry; archiving an already archived tariff returns archived state.
## 19. Caching
Invalidate tariff list after success.
## 20. Idempotency
Archive is idempotent for the same tariff.
## 21. Security Notes
Archive does not delete historical receipts or entitlements.
## 22. Example Flow
Admin archives an old QR tariff after replacing it with a new tariff.
## 23. Related Endpoints
`GET /api/v2/commerce/tariffs`, `GET /api/v2/admin/commerce/tariffs`.
## 24. Change History
2026-07-11: Added admin tariff archive.
