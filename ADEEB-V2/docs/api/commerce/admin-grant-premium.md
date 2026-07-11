---
id: Commerce.AdminGrantPremium
title: Admin Grant Premium Entitlement
method: POST
route: /api/v2/admin/commerce/students/{studentId}/premium-grants
status: stable
---

# Admin Grant Premium Entitlement

## 1. Endpoint

`POST /api/v2/admin/commerce/students/{studentId}/premium-grants`

## 2. Purpose

Grants a manual premium entitlement to an active student. This is an admin-only Phase 2.1 control surface for testing and operating premium access before payment-provider integration exists.

## 3. Status

Stable.

## 4. Module

Commerce.

## 5. Authentication

Bearer access token required.

## 6. Authorization

`ContentAdmin` policy. Current roles: `SuperAdmin`, `Admin`.

## 7. Rate Limit

Default authenticated/admin platform limits.

## 8. Localization

Errors are localized using normal ADEEB request localization. Stable error codes do not change by language.

## 9. Request Headers

`Authorization: Bearer {token}`.

`Content-Type: application/json`.

## 10. Path Parameters

`studentId`: target Student persona id.

## 11. Query Parameters

Not applicable.

## 12. Request Body

```json
{
  "startsAtUtc": "2026-07-11T09:00:00Z",
  "expiresAtUtc": "2026-08-11T09:00:00Z",
  "idempotencyKey": "manual-premium-6b8e65c2-2026-07"
}
```

`startsAtUtc` is optional. When omitted, the server uses the current UTC time.

`expiresAtUtc` is optional. When omitted, the grant does not expire automatically.

## 13. Field Rules

`idempotencyKey` is required and must be at most 128 characters.

`expiresAtUtc`, when supplied, must be after the effective start time.

## 14. Success Response

`200 OK`

```json
{
  "entitlementId": "51daac1e-f832-4d9e-8c25-23c06ddca5b8",
  "studentId": "6b8e65c2-24c3-4d10-b4b3-7f8ce326a8e9",
  "kind": "Premium",
  "status": "Active",
  "source": "ManualGrant",
  "startsAtUtc": "2026-07-11T09:00:00Z",
  "expiresAtUtc": "2026-08-11T09:00:00Z",
  "idempotencyKey": "manual-premium-6b8e65c2-2026-07",
  "revokeReason": null,
  "revokedAtUtc": null,
  "createdAtUtc": "2026-07-11T09:00:00Z",
  "updatedAtUtc": "2026-07-11T09:00:00Z"
}
```

Repeating the same `idempotencyKey` returns the original entitlement.

## 15. Error Responses

`401 Unauthorized` when the access token is missing or invalid.

`403 Forbidden` when the user lacks the `ContentAdmin` policy.

`404 Not Found` when the target active student does not exist.

`422 Unprocessable Entity` for invalid body fields.

## 16. Stable Error Codes

`commerce.student_not_found`, `commerce.idempotency_key.invalid`, `commerce.idempotency_key.in_use`, `commerce.expires_at.invalid`, `validation.failed`.

## 17. Frontend Behavior

Admin clients must generate a stable idempotency key per intended manual grant and reuse it when retrying after an unknown network result.

## 18. Retry Policy

Safe to retry with the same idempotency key. Do not retry with a new key unless the admin intentionally wants a second grant.

## 19. Caching

Invalidate current entitlement summary caches for the target student after success.

## 20. Idempotency

The `idempotencyKey` uniquely identifies the grant.

## 21. Security Notes

This endpoint does not process money and must not be used as proof of payment. Future payment integration must verify provider callbacks separately.

## 22. Example Flow

1. Admin opens a student's support view.
2. Admin submits a premium grant with an idempotency key.
3. Student entitlement summary returns `premiumActive: true`.

## 23. Related Endpoints

`GET /api/v2/commerce/me/entitlements`

`POST /api/v2/admin/commerce/entitlements/{entitlementId}/revoke`

## 24. Change History

2026-07-11: Added admin manual premium grant endpoint.
