---
id: Commerce.AdminRevokeEntitlement
title: Admin Revoke Commerce Entitlement
method: POST
route: /api/v2/admin/commerce/entitlements/{entitlementId}/revoke
status: stable
---

# Admin Revoke Commerce Entitlement

## 1. Endpoint

`POST /api/v2/admin/commerce/entitlements/{entitlementId}/revoke`

## 2. Purpose

Revokes an existing commerce entitlement. Revoked premium entitlements no longer make the student premium-active.

## 3. Status

Stable.

## 4. Module

Commerce.

## 5. Authentication

Bearer access token required.

## 6. Authorization

`commerce.entitlements.grant` permission.

## 7. Rate Limit

Default authenticated/admin platform limits.

## 8. Localization

Errors are localized using normal ADEEB request localization. Stable error codes do not change by language.

## 9. Request Headers

`Authorization: Bearer {token}`.

`Content-Type: application/json`.

## 10. Path Parameters

`entitlementId`: commerce entitlement id.

## 11. Query Parameters

Not applicable.

## 12. Request Body

```json
{
  "reason": "admin correction"
}
```

`reason` is optional.

## 13. Field Rules

`reason`, when supplied, must be at most 256 characters.

## 14. Success Response

`200 OK`

```json
{
  "entitlementId": "51daac1e-f832-4d9e-8c25-23c06ddca5b8",
  "studentId": "6b8e65c2-24c3-4d10-b4b3-7f8ce326a8e9",
  "kind": "Premium",
  "status": "Revoked",
  "source": "ManualGrant",
  "startsAtUtc": "2026-07-11T09:00:00Z",
  "expiresAtUtc": "2026-08-11T09:00:00Z",
  "idempotencyKey": "manual-premium-6b8e65c2-2026-07",
  "revokeReason": "admin correction",
  "revokedAtUtc": "2026-07-11T10:00:00Z",
  "createdAtUtc": "2026-07-11T09:00:00Z",
  "updatedAtUtc": "2026-07-11T10:00:00Z"
}
```

## 15. Error Responses

`401 Unauthorized` when the access token is missing or invalid.

`403 Forbidden` when the user lacks `commerce.entitlements.grant`.

`404 Not Found` when the entitlement does not exist.

`422 Unprocessable Entity` for invalid body fields.

## 16. Stable Error Codes

`commerce.entitlement_not_found`, `commerce.revoke_reason.invalid`, `validation.failed`.

## 17. Frontend Behavior

Admin clients should refresh the student's entitlement summary after revocation.

## 18. Retry Policy

Safe to retry. Revoking an already revoked entitlement returns the revoked entitlement state.

## 19. Caching

Invalidate current entitlement summary caches for the affected student after success.

## 20. Idempotency

Revocation is idempotent for the same entitlement id.

## 21. Security Notes

Only content administrators may revoke entitlements. Revocation does not refund payments; future payment/refund flows must model that explicitly.

## 22. Example Flow

1. Admin reviews an entitlement.
2. Admin submits revoke with a short reason.
3. Student entitlement summary falls back to `Free` unless another active premium entitlement exists.

## 23. Related Endpoints

`GET /api/v2/commerce/me/entitlements`

`POST /api/v2/admin/commerce/students/{studentId}/premium-grants`

## 24. Change History

2026-07-11: Added admin commerce entitlement revocation endpoint.
