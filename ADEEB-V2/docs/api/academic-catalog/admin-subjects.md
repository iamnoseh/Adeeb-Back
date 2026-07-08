---
id: AcademicCatalog.AdminSubjects
title: Admin Subjects API
method: POST
route: /api/v2/admin/subjects
status: active
---

## 1. Endpoint

`POST /api/v2/admin/subjects`

Also available: `GET /api/v2/admin/subjects`, `GET /api/v2/admin/subjects/{id}`, `PUT /api/v2/admin/subjects/{id}`, `POST /api/v2/admin/subjects/{id}/archive`, `DELETE /api/v2/admin/subjects/{id}`.

## 2. Purpose

Creates and manages multilingual subjects using a multipart admin form.

## 3. Status

Active.

## 4. Module

AcademicCatalog.

## 5. Authentication

Bearer access token required.

## 6. Authorization

`ContentAdmin` policy. Current roles: `SuperAdmin`, `Admin`.

## 7. Rate Limit

Default platform limits.

## 8. Localization

Admin clients submit Tajik and Russian content explicitly. Response `name` follows request localization, while `translations` contains all submitted translations.

## 9. Request Headers

`Authorization: Bearer {token}`. For create/update, send `Content-Type: multipart/form-data`.

## 10. Path Parameters

`id` for detail, update, archive, and delete routes.

## 11. Query Parameters

List supports `search`, `status`, `page`, `pageSize`, `sort`.

## 12. Request Body

Create/update form fields:

- `NameTg`: required Tajik subject name.
- `NameRu`: required Russian subject name.
- `NameEn`: optional English subject name.
- `DescriptionTg`: optional Tajik description.
- `DescriptionRu`: optional Russian description.
- `DescriptionEn`: optional English description.
- `Name`: optional legacy fallback; if sent without `NameTg`/`NameRu`, it is copied into Tajik and Russian for backward compatibility.
- `Icon`: optional image file.
- `Status`: numeric, default `1`.
- `DisplayOrder`: numeric, default `0`.

`Icon` is uploaded as a file. The API creates and stores the URL automatically; clients do not send `iconUrl`.

## 13. Field Rules

`Status`: `0 = Draft`, `1 = Active`, `2 = Archived`.

Allowed icon extensions: `.jpg`, `.jpeg`, `.png`, `.svg`, `.webp`. Maximum icon size: 10 MB.

## 14. Success Response

Subject response with `id`, `code`, `name`, `iconUrl`, `displayOrder`, `status`, `translations`.

## 15. Error Responses

Stable ProblemDetails with localized title and validation errors.

## 16. Stable Error Codes

`validation.failed`, `academic.subject_code_exists`, `academic.subject_not_found`, `academic.icon.invalid_type`, `academic.icon.too_large`.

## 17. Frontend Behavior

Swagger and admin clients should submit a multilingual form:

- `NameTg = Математика`
- `NameRu = Математика`
- `DescriptionTg = optional`
- `DescriptionRu = optional`
- `Icon = selected file`
- `Status = 1`
- `DisplayOrder = 0`

## 18. Retry Policy

Do not blindly retry create/update with files after an unknown network result; reload list first.

## 19. Caching

Invalidate subject list caches after create/update/archive/delete.

## 20. Idempotency

Mutations are not idempotent.

## 21. Security Notes

Only content administrators may mutate subjects. Uploaded file values are not trusted as paths.

## 22. Example Flow

Login as `SuperAdmin`, authorize Swagger with bearer token, open `POST /api/v2/admin/subjects`, fill `NameTg` and `NameRu`, optionally choose `Icon`, and execute.

## 23. Related Endpoints

`GET /api/v2/subjects`, `/api/v2/admin/topics`, `/api/v2/admin/questions`.

## 24. Change History

Updated subject form to support explicit Tajik/Russian translations while preserving legacy `Name` fallback.
