---
id: AcademicCatalog.Subjects
title: Public Subjects API
method: GET
route: /api/v2/subjects
status: active
---

## 1. Endpoint

`GET /api/v2/subjects`

## 2. Purpose

Returns active subjects for public lookup, dropdowns, filters, and future learning flows.

## 3. Status

Active.

## 4. Module

AcademicCatalog.

## 5. Authentication

Anonymous.

## 6. Authorization

None.

## 7. Rate Limit

Default platform limits.

## 8. Localization

`name` follows ADEEB language precedence. Internally, admin subject creation currently maps one submitted `Name` into Tajik, Russian, and English translations.

## 9. Request Headers

`X-Adeeb-Language` optional.

## 10. Path Parameters

None.

## 11. Query Parameters

`search`, `status`, `page`, `pageSize`, `sort`.

`status`: `0 = Draft`, `1 = Active`, `2 = Archived`.

## 12. Request Body

None.

## 13. Field Rules

Only active subjects are returned by public endpoints.

## 14. Success Response

Paged response with `items`, `page`, `pageSize`, and `totalCount`.

Subject fields: `id`, `code`, `name`, `iconUrl`, `displayOrder`, `status`, `translations`.

## 15. Error Responses

Stable ProblemDetails.

## 16. Stable Error Codes

`validation.failed`, `academic.subject_not_found`.

## 17. Frontend Behavior

Use this endpoint for subject dropdowns and public subject navigation. Images should be rendered from `iconUrl`.

## 18. Retry Policy

Retry only transient network errors.

## 19. Caching

Safe to cache briefly on the frontend.

## 20. Idempotency

Read-only.

## 21. Security Notes

No sensitive fields are returned.

## 22. Example Flow

Load subjects, then load `/api/v2/subjects/{id}/topics`.

## 23. Related Endpoints

`GET /api/v2/subjects/{id}`, `GET /api/v2/subjects/{id}/topics`, `/api/v2/admin/subjects`.

## 24. Change History

Updated after IQRA compatibility review: public docs now reflect numeric status values and form-based admin creation.
