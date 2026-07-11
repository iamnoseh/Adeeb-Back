---
id: Commerce.MeEntitlements
title: Commerce Current Entitlements
method: GET
route: /api/v2/commerce/me/entitlements
status: stable
---

# Commerce Current Entitlements

## 1. Endpoint

`GET /api/v2/commerce/me/entitlements`

## 2. Purpose

Returns the authenticated student's current commerce entitlement summary. This is the Phase 2 read foundation for future premium and payment flows.

## 3. Status

Stable.

## 4. Module

Commerce.

## 5. Authentication

Required. The endpoint uses the authenticated JWT `sub` claim to resolve the current Identity user and then the linked Student persona.

## 6. Authorization

Authenticated student self-access only. The client cannot supply another student id.

## 7. Rate Limit

Uses the default authenticated API rate-limit behavior. No commerce write or payment rate limit applies because this endpoint is read-only.

## 8. Localization

Errors are localized using normal ADEEB request localization. Stable error codes do not change by language.

## 9. Request Headers

`Authorization: Bearer <access-token>` is required.

`X-Adeeb-Language` is optional and overrides response error language when supplied with a supported culture.

## 10. Path Parameters

Not applicable.

## 11. Query Parameters

Not applicable.

## 12. Request Body

Not applicable.

## 13. Field Rules

Not applicable.

## 14. Success Response

`200 OK`

```json
{
  "studentId": "6b8e65c2-24c3-4d10-b4b3-7f8ce326a8e9",
  "accessLevel": "Free",
  "premiumActive": false,
  "premiumUntilUtc": null,
  "source": "default"
}
```

When an active premium entitlement exists, `accessLevel` is `Premium`, `premiumActive` is `true`, and `source` identifies the entitlement source such as `Trial`, `Payment`, or `ManualGrant`.

## 15. Error Responses

`401 Unauthorized` when the access token is missing or invalid.

`409 Conflict` when the authenticated identity does not have an active Student persona.

## 16. Stable Error Codes

`commerce.student_required`: an active Student persona is required before commerce entitlement information can be resolved.

## 17. Frontend Behavior

Use this endpoint to decide whether to show free or premium UI state. Do not infer premium status from Identity claims. If the response is `commerce.student_required`, call the student self-provisioning flow before retrying.

## 18. Retry Policy

Safe to retry on transient network failures because the endpoint is read-only.

## 19. Caching

Clients may cache briefly for a single foreground session, but should refresh after login, refresh-token renewal, purchase completion, or app foregrounding.

## 20. Idempotency

Not applicable for this read endpoint.

## 21. Security Notes

The client cannot choose the target student. Commerce resolves the student through the authenticated principal and Students module contract.

## 22. Example Flow

1. User logs in.
2. Client calls `GET /api/v2/commerce/me/entitlements`.
3. Client renders free or premium affordances from `premiumActive`.

## 23. Related Endpoints

`GET /api/v2/students/me`

`POST /api/v2/students/me/provision`

## 24. Change History

2026-07-11: Added Commerce Phase 2 entitlement summary endpoint.
