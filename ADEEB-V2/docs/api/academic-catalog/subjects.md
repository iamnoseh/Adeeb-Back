---
id: AcademicCatalog.Subjects
title: Subjects API
method: GET
route: /api/v2/subjects
status: active
---

## 1. Endpoint

`GET /api/v2/subjects`

## 2. Purpose

Returns active academic subjects for frontend lookup.

## 3. Status

Active.

## 4. Module

AcademicCatalog.

## 5. Authentication

Anonymous.

## 6. Authorization

None for public lookup. Admin management uses `/api/v2/admin/subjects` with `ContentAdmin`.

## 7. Rate Limit

Default platform limits.

## 8. Localization

Response `name` follows ADEEB language precedence.

## 9. Request Headers

`X-Adeeb-Language` optional.

## 10. Path Parameters

None.

## 11. Query Parameters

`search`, `status`, `page`, `pageSize`, `sort`.

## 12. Request Body

None.

## 13. Field Rules

Active subjects require Tajik and Russian translations.

## 14. Success Response

Paged subject list.

## 15. Error Responses

Stable ProblemDetails.

## 16. Stable Error Codes

`validation.failed`, `academic.subject_not_found`.

## 17. Frontend Behavior

Use for dropdowns, filters, and public subject navigation.

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

`GET /api/v2/subjects/{id}/topics`, `/api/v2/admin/subjects`.

## 24. Change History

Added in Subject Foundation + QuestionBank phase.
