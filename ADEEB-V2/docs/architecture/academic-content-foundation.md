---
id: Architecture.AcademicContentFoundation
title: Academic Content Foundation
status: active
---

# Academic Content Foundation

ADEEB V2 separates educational taxonomy from question authoring.

`AcademicCatalog` owns subjects and topics in the `academic` schema. `QuestionBank` owns questions and answer options in the `question_bank` schema. QuestionBank validates subject/topic existence through the `IAcademicCatalogLookup` contract and does not use AcademicCatalog infrastructure directly.

Subjects and topics support multilingual translations. Active items require Tajik (`tg-TJ`) and Russian (`ru-RU`) content; English (`en-US`) is optional.

QuestionBank currently supports only management of reusable questions. Test catalogs, test sessions, scoring, attempts, XP, gamification, admission workflows, and AI explanations are intentionally outside this phase.
