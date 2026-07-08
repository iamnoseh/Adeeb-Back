---
id: QuestionBank.Questions
title: Question Bank Admin API
method: POST
route: /api/v2/admin/questions
status: active
---

## 1. Endpoint

`POST /api/v2/admin/questions`

Also available: `GET /api/v2/admin/questions`, `GET /api/v2/admin/questions/{id}`, `PUT /api/v2/admin/questions/{id}`, `POST /api/v2/admin/questions/{id}/archive`, `DELETE /api/v2/admin/questions/{id}`.

## 2. Purpose

Creates and manages reusable question-bank questions. This is admin/content tooling only; test runtime is not implemented in this phase.

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

The IQRA-compatible form submits one `Content`, one `Explanation`, and one answer text set. V2 stores that submitted content into Tajik, Russian, and English translations for now.

## 9. Request Headers

`Authorization: Bearer {token}`. For create/update, send `Content-Type: multipart/form-data`.

## 10. Path Parameters

`id` for detail, update, archive, and delete routes.

## 11. Query Parameters

List supports:

- `subjectId`
- `topicId`
- `type`
- `difficulty`
- `status`
- `search`
- `page`
- `pageSize`
- `sort`

## 12. Request Body

Create/update form fields:

- `SubjectId`: required subject GUID.
- `TopicId`: optional topic GUID.
- `Content`: required question text.
- `Explanation`: optional explanation.
- `Type`: numeric question type.
- `Difficulty`: numeric difficulty.
- `Status`: numeric status, default `1`.
- `AnswersJson`: JSON string for SingleChoice and Matching.
- `CorrectAnswer`: required for ClosedAnswer.
- `Image`: optional image file.

Do not send `imageUrl`; the API creates the URL after receiving `Image`.

Do not send a topic name when `TopicId` is used.

## 13. Field Rules

Enums:

- `Type`: `1 = SingleChoice`, `2 = Matching`, `3 = ClosedAnswer`.
- `Difficulty`: `1 = Easy`, `2 = Medium`, `3 = Hard`.
- `Status`: `0 = Draft`, `1 = Active`, `2 = Archived`.

SingleChoice `AnswersJson` shape:

```json
[
  { "text": "A", "isCorrect": false },
  { "text": "B", "isCorrect": true }
]
```

SingleChoice requires at least 2 options and exactly 1 correct answer.

Matching `AnswersJson` shape:

```json
[
  { "text": "Tajikistan", "matchPair": "Dushanbe" },
  { "text": "Russia", "matchPair": "Moscow" },
  { "text": "France", "matchPair": "Paris" }
]
```

Matching requires at least 3 pairs and each option must include `matchPair`.

ClosedAnswer ignores `AnswersJson` and uses `CorrectAnswer`.

Allowed image extensions: `.jpg`, `.jpeg`, `.png`, `.webp`. Maximum image size: 5 MB.

## 14. Success Response

Question response with `id`, `subjectId`, `topicId`, `topic`, `type`, `difficulty`, `status`, `content`, `imageUrl`, `translations`, and `answerOptions`.

## 15. Error Responses

Stable ProblemDetails with localized title and structured validation errors.

## 16. Stable Error Codes

`validation.failed`, `question_bank.question_not_found`, `academic.subject_not_found`, `academic.topic_not_found`, `question.image.invalid_type`, `question.image.too_large`, `question.form.invalid_json`.

## 17. Frontend Behavior

Swagger/admin clients should use the IQRA-compatible form. For image upload, choose `Image`; after success, use the returned `imageUrl` for preview.

For update, if no new `Image` is sent, the existing image URL remains unchanged.

## 18. Retry Policy

Do not blindly retry create/update with files after an unknown network result; reload the question list first.

## 19. Caching

Do not cache admin mutations. Invalidate question list and detail caches after changes.

## 20. Idempotency

GET is read-only. POST/PUT/DELETE are not idempotent.

## 21. Security Notes

Uploaded file names are not trusted. Runtime answer delivery and scoring are out of scope.

## 22. Example Flow

Create or select a subject, optionally select a topic, then create a question:

- `SubjectId = {guid}`
- `TopicId = {guid}`
- `Content = Пойтахти Тоҷикистон кадом аст?`
- `Type = 1`
- `Difficulty = 1`
- `Status = 1`
- `AnswersJson = [{"text":"Хуҷанд","isCorrect":false},{"text":"Душанбе","isCorrect":true}]`
- `Image = optional file`

## 23. Related Endpoints

`/api/v2/admin/subjects`, `/api/v2/admin/topics`, `GET /api/v2/subjects/{id}/topics`.

## 24. Change History

Updated after IQRA compatibility review: create/update now use multipart form fields and no longer expose `imageUrl` or duplicate `topic` in Swagger.
