---
id: QuestionBank.Questions
title: Question Bank Admin API
method: GET
route: /api/v2/admin/questions
status: active
---

## 1. Endpoint

`GET /api/v2/admin/questions`

## 2. Purpose

Lists managed question-bank items for content administrators.

## 3. Status

Active.

## 4. Module

QuestionBank.

## 5. Authentication

Bearer access token required.

## 6. Authorization

`ContentAdmin` policy. Current roles: `SuperAdmin`, `Admin`.

## 7. Rate Limit

Default platform limits.

## 8. Localization

Response `content` follows ADEEB language precedence.

## 9. Request Headers

`Authorization: Bearer {token}`, `X-Adeeb-Language` optional.

## 10. Path Parameters

None for list. Details use `{id}`.

## 11. Query Parameters

`subjectId`, `topicId`, `type`, `difficulty`, `status`, `search`, `page`, `pageSize`, `sort`.

## 12. Request Body

Create/update use JSON question payload with translations and answer options.

## 13. Field Rules

SingleChoice requires exactly 4 options and 1 correct answer. Matching requires exactly 4 unique right-side pairs. ClosedAnswer requires 1 canonical correct answer.

## 14. Success Response

Paged question list or question details.

## 15. Error Responses

Stable ProblemDetails.

## 16. Stable Error Codes

`validation.failed`, `question_bank.question_not_found`, `academic.subject_not_found`, `academic.topic_not_found`.

## 17. Frontend Behavior

Use only inside admin/content tooling.

## 18. Retry Policy

Retry only transient network errors.

## 19. Caching

Do not cache admin mutations.

## 20. Idempotency

GET is read-only. POST/PUT/DELETE are not idempotent unless the client retries after no response and then reloads state.

## 21. Security Notes

Runtime test delivery is not implemented in this phase.

## 22. Example Flow

Create subject, create topic, then create active question with Tajik and Russian translations.

## 23. Related Endpoints

`/api/v2/admin/subjects`, `/api/v2/admin/topics`.

## 24. Change History

Added in Subject Foundation + QuestionBank phase.
