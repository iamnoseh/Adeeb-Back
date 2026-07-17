# ADEEB Vocabulary Frontend Contract

## Scope

Vocabulary is a first-class learning area for students and a content-review area for admins. The frontend uses the backend module under:

- Student: `/api/v2/students/me/vocabulary`
- Admin: `/api/v2/admin/vocabulary`

The module has no audio, no typing answer, no vocabulary-specific streak, and no XP/coin flow in phase 1. Student activity streak remains the global `/student` activity feature.

## Student Flow

1. Student opens `/student/vocabulary`.
2. If no course exists, the page shows active learning languages and CEFR levels.
3. Saving a course calls `PUT /course` with `languageId` and `level`.
4. Dashboard loads with:
   - `GET /dashboard`
   - `GET /today`
   - `GET /mistakes?page=1&pageSize=10`
   - `GET /history?page=1&pageSize=10`
5. Daily practice starts with `POST /sessions` and `{ mode: 0 }`.
6. Mistake review starts with `{ mode: 1 }`.
7. Free practice starts with `{ mode: 2, level?, topicId? }`.
8. Test starts with `{ mode: 3, questionCount: 10 | 20 | 30 }`.

The backend may resume an unfinished session for the same mode. The UI must treat the returned session as the source of truth.

## Session UX Rules

- Translation, fill blank, synonym, and antonym use one selected option.
- Odd word replacement uses a selected token index and replacement option.
- Word order uses ordered option IDs.
- Practice modes show feedback immediately.
- Test mode hides correctness until complete.
- Correct answers are never assumed on the client; they only come from answer feedback or completion.
- Answer buttons become locked after an accepted answer.
- Complete calls `POST /sessions/{id}/complete`.

## Admin Flow

Admin content pages use the same reusable list shell patterns as existing admin pages:

- Languages: active learning languages.
- Topics: per language and CEFR level.
- Words: target word, translations, examples, status.
- Questions: generated/reviewed drafts, publish/archive lifecycle.
- Daily words: scheduled word-of-day records.

List endpoints default to `page=1&pageSize=10` and max 50. Frontend pages should not fetch unbounded data.

## Localization

The API already respects `X-Adeeb-Language` and `Accept-Language`, set globally by `httpClient`. UI strings live under the `vocabulary` translation namespace:

```ts
t("vocabulary.title")
```

Target words remain in the learning language; explanations, translations and feedback are localized by the backend.

## Query Keys

All query keys include a language marker where localized data is returned:

- `["vocabulary", "student", "dashboard", uiLanguage]`
- `["vocabulary", "student", "session", sessionId, uiLanguage]`
- `["vocabulary", "admin", resource, query, uiLanguage]`

After course changes or session completion, invalidate the student vocabulary root.

## Visual Direction

Student pages follow the current ADEEB student dashboard design:

- white cards, soft borders, compact cards, purple primary action;
- no large explanatory marketing blocks;
- no role display;
- no raw native selects where `SelectField` is available;
- stable card dimensions and short text.

Admin pages follow existing dense admin surfaces and reusable table/list controls.
