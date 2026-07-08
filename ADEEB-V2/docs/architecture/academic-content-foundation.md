---
id: Architecture.AcademicContentFoundation
title: Academic Content Foundation
status: active
---

# Academic Content Foundation

ADEEB V2 separates educational taxonomy from question authoring.

`AcademicCatalog` owns subjects and topics in the `academic` schema. `QuestionBank` owns questions and answer options in the `question_bank` schema. QuestionBank validates subject/topic existence through the `IAcademicCatalogLookup` contract and does not use AcademicCatalog infrastructure directly.

Subjects and topics support multilingual translations. Active items require Tajik (`tg-TJ`) and Russian (`ru-RU`) content; English (`en-US`) is optional.

The current admin Subject endpoint intentionally accepts an IQRA-compatible `multipart/form-data` shape: `Name`, optional `Icon`, `Status`, and `DisplayOrder`. The backend stores the uploaded icon and persists the generated URL. Clients must not submit `iconUrl` directly.

The current admin Question endpoint also accepts an IQRA-compatible `multipart/form-data` shape: `SubjectId`, optional `TopicId`, `Content`, `Explanation`, `Type`, `Difficulty`, `Status`, `AnswersJson`, `CorrectAnswer`, and optional `Image`. The backend stores the uploaded image and persists the generated `imageUrl`. If `TopicId` is supplied, clients do not submit a duplicate topic name.

QuestionBank currently supports only management of reusable questions. Test catalogs, test sessions, scoring, attempts, XP, gamification, admission workflows, and AI explanations are intentionally outside this phase.

Question type values follow the IQRA enum numbers: `1 = SingleChoice`, `2 = Matching`, `3 = ClosedAnswer`. Difficulty follows `1 = Easy`, `2 = Medium`, `3 = Hard`.
