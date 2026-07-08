---
id: AcademicCatalog.AdminTopics
title: Admin Topics API
method: POST
route: /api/v2/admin/topics
status: active
---

## 1. Endpoint

`POST /api/v2/admin/topics`

Also available: `GET /api/v2/admin/topics`, `PUT /api/v2/admin/topics/{id}`, `POST /api/v2/admin/topics/{id}/archive`, `DELETE /api/v2/admin/topics/{id}`.

## 2. Purpose

Manages normalized topics under subjects.

## 3. Status

Active.

## 4. Module

AcademicCatalog.

## 5. Authentication

Bearer access token required.

## 6. Authorization

`ContentAdmin` policy.

## 7. Rate Limit

Default platform limits.

## 8. Localization

Topic entities still use translation-aware storage. Current admin topic endpoint accepts translation payload, unlike IQRA where question `Topic` was only a string.

## 9. Request Headers

`Authorization: Bearer {token}`.

## 10. Path Parameters

`id` for update, archive, and delete.

## 11. Query Parameters

List supports `subjectId`, `search`, `status`, `page`, `pageSize`, `sort`.

## 12. Request Body

JSON body:

- `subjectId`: required subject GUID.
- `code`: required topic code.
- `displayOrder`: number.
- `status`: numeric.
- `translations`: list of `{ language, name, description }`.

## 13. Field Rules

`language`: `0 = tg-TJ`, `1 = ru-RU`, `2 = en-US`.

`status`: `0 = Draft`, `1 = Active`, `2 = Archived`.

Active topics require Tajik and Russian translations.

## 14. Success Response

Topic response with `id`, `subjectId`, `code`, `name`, `displayOrder`, `status`, `translations`.

## 15. Error Responses

Stable ProblemDetails.

## 16. Stable Error Codes

`validation.failed`, `academic.subject_not_found`, `academic.topic_not_found`, `academic.topic_code_exists`.

## 17. Frontend Behavior

When creating a question, use `topicId`; do not also send topic name.

## 18. Retry Policy

Reload list after uncertain mutation results.

## 19. Caching

Invalidate topic cache after mutations.

## 20. Idempotency

Mutations are not idempotent.

## 21. Security Notes

Topic mutation is admin-only.

## 22. Example Flow

Create subject, then create topic for that subject, then create questions using the topic ID.

## 23. Related Endpoints

`GET /api/v2/subjects/{id}/topics`, `/api/v2/admin/questions`.

## 24. Change History

Added to clarify that V2 question forms use `topicId` and no duplicate `topic` string.
