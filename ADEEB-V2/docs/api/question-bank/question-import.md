# Question Import

## 1. Overview

Question Import lets a content admin parse `SingleChoice` and `ClosedAnswer` questions from `.docx` and text-based `.pdf` files. The flow is intentionally split into two calls:

- parse and preview: extracts and validates questions, but does not write to the database;
- confirm import: accepts the edited preview payload, revalidates it, and creates real `Question` and `AnswerOption` rows transactionally.

The preview is stateless. No import-session table, cleanup job, or temporary database state is created.

## 2. Architecture

Parse pipeline:

```text
Upload
  -> File validation
  -> Document text extraction
  -> Text normalization
  -> Question parsing
  -> Parsed-question validation
  -> Duplicate detection
  -> Preview response
```

Confirm pipeline:

```text
Edited preview
  -> Confirm endpoint
  -> Server-side revalidation
  -> Existing QuestionBank creation rules
  -> EF transaction
  -> Commit / rollback
```

## 3. Reused Existing Architecture

The implementation reuses:

- `QuestionBankService`
- `QuestionUpsertRequest`
- `Validation.ValidateQuestion`
- `Result<T>`
- module `ProblemDetails` mapping
- `ContentAdmin` authorization policy
- `QuestionBankDbContext`
- `IAcademicCatalogLookup`

Important files:

- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Endpoints/QuestionBankEndpoints.cs`
- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Contracts/QuestionImportContracts.cs`
- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Application/Import/QuestionImportService.cs`
- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Application/Import/QuestionDocumentParser.cs`
- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Infrastructure/DocumentExtraction/DocxQuestionTextExtractor.cs`
- `src/Modules/QuestionBank/Adeeb.Modules.QuestionBank/Infrastructure/DocumentExtraction/PdfQuestionTextExtractor.cs`

## 4. API - Parse Endpoint

```http
POST /api/v2/admin/questions/import/parse
```

Authorization: `ContentAdmin`.

Content type: `multipart/form-data`.

Fields:

- `SubjectId` required `Guid`
- `TopicId` optional `Guid`
- `Difficulty` required `int`; real enum values are `1=Easy`, `2=Medium`, `3=Hard`
- `File` required upload

Supported file types:

- `.docx`
- `.pdf`

Default limits from `QuestionImportOptions`:

- `MaxFileSizeBytes`: `5242880`
- `MaxQuestionsPerImport`: `200`
- `MaxQuestionTextLength`: `4000`
- `MaxOptionTextLength`: `1000`
- `MaxOptionsPerQuestion`: `8`

Response contract:

```json
{
  "fileName": "questions.docx",
  "summary": {
    "totalDetected": 1,
    "valid": 1,
    "invalid": 0,
    "warnings": 0
  },
  "questions": [
    {
      "clientKey": "q-1",
      "questionType": 1,
      "questionTypeName": "SingleChoice",
      "questionText": "Question?",
      "expectedAnswer": null,
      "options": [
        { "label": "A", "text": "Correct", "isCorrect": true },
        { "label": "B", "text": "Wrong", "isCorrect": false }
      ],
      "isValid": true,
      "errors": [],
      "warnings": []
    }
  ]
}
```

Curl example:

```bash
curl -X POST http://localhost:5238/api/v2/admin/questions/import/parse \
  -H "Authorization: Bearer <token>" \
  -F "SubjectId=<subject-guid>" \
  -F "Difficulty=1" \
  -F "File=@questions.docx"
```

## 5. API - Confirm Endpoint

```http
POST /api/v2/admin/questions/import/confirm
```

Authorization: `ContentAdmin`.

Content type: `application/json`.

Request contract:

```json
{
  "subjectId": "subject-guid",
  "topicId": null,
  "difficulty": 1,
  "questions": [
    {
      "questionType": 1,
      "questionText": "Question?",
      "options": [
        { "text": "Correct", "isCorrect": true },
        { "text": "Wrong", "isCorrect": false }
      ]
    },
    {
      "questionType": 3,
      "questionText": "2 + 5 = ?",
      "expectedAnswer": "7",
      "options": []
    }
  ]
}
```

Response contract:

```json
{
  "importedCount": 1,
  "questionIds": ["created-question-guid"]
}
```

The endpoint revalidates subject, topic, difficulty, question text, options, and existing QuestionBank rules. It does not trust `isValid`, warnings, or parser metadata from the client. It uses one EF Core transaction; one failure rolls back the full batch.

## 6. Supported Document Syntax

### SingleChoice

```text
<<<Савол?>>>
-- А) Ҷавоби дуруст
Б) Ҷавоби нодуруст
С) Ҷавоби нодуруст
Д) Ҷавоби нодуруст
```

### Direct / Closed Answer

`ClosedAnswer` uses the existing QuestionBank model: one canonical correct `AnswerOption`. Import stores the same expected answer text across the generated Tajik, Russian, and English answer translations because the uploaded file is one source stream.

```text
<<<2 + 5 = ?>>>
-- A) 7
```

The expected answer is kept as text. Numeric-looking answers such as `-7`, `2.5`, `3,14`, `1/2`, and `2025-2026` are not parsed as numbers.

Accepted label variants include Latin and Cyrillic `A/B/C/D`, `А/Б/С/Д`, and lowercase forms. Accepted separators are `)`, `.`, and `:`.

Correct marker variants:

```text
--А)
-- А)
--  А)
```

## 7. Parsing Rules

- A question begins with `<<<` and ends with `>>>`.
- Question text may be inline or multiline.
- Answer lines after a question belong to the nearest preceding question.
- A new option starts only when a line matches a supported option-prefix pattern.
- Non-empty lines after an option starts are treated as continuation lines for that option.
- Type detection is structural:
  - exactly one recognized line marked with `--` becomes `ClosedAnswer`;
  - two or more recognized option lines with exactly one `--` marker become `SingleChoice`.
- Mixed documents are supported; one upload can contain both `SingleChoice` and `ClosedAnswer` blocks.
- Malformed documents return structured per-document or per-question issues instead of crashing the import.

## 8. Validation Rules

Implemented parser/config validation:

- question text is required;
- at least two options are required;
- exactly one correct option is required;
- option text is required;
- `ClosedAnswer` requires one marked expected answer line and non-empty expected answer text;
- question text max length is `4000`;
- option text max length is `1000`;
- max options per question is `8`;
- max questions per import is `200`.

Confirm also reuses `Validation.ValidateQuestion` through `QuestionBankService`.

## 9. Duplicate Detection

- Duplicate question text inside the uploaded file adds a warning: `question_import.duplicate_in_file`.
- Exact normalized duplicate question text in the database for the selected subject/topic adds a warning: `question_import.possible_duplicate_in_database`.
- Duplicates are warnings and do not automatically block import.

## 10. DOCX Support

DOCX extraction uses `DocumentFormat.OpenXml`. Paragraphs are read in document order. Runs, tabs, and line breaks are handled conservatively. Tajik/Cyrillic Unicode text is preserved.

## 11. PDF Support

PDF extraction uses `PdfPig` with the `UglyToad.PdfPig` namespace. Only text-based PDFs are supported. Scanned or image-only PDFs are not OCR processed and produce `question_import.no_extractable_text`.

## 12. Security and Limits

The importer validates extension and content type. It does not save uploaded files permanently and does not expose server paths. CancellationToken is passed through extraction and database work.

Unsupported inputs include `.doc`, `.txt`, `.rtf`, images, archives, and executable files.

## 13. Error Handling

Errors use the existing ADEEB `ProblemDetails` contract with stable `code` values. Confirmed import codes include:

- `question_import.file_required`
- `question_import.file_too_large`
- `question_import.unsupported_extension`
- `question_import.unsupported_content_type`
- `question_import.extractor_not_found`
- `question_import.no_extractable_text`
- `question_import.no_questions_detected`
- `question_import.missing_closing_marker`
- `question_import.question_text_required`
- `question_import.expected_answer_required`
- `question_import.correct_option_required`
- `question_import.multiple_correct_options`
- `question_import.possible_duplicate_in_database`
- `academic.subject_not_found`
- `academic.topic_not_found`

## 14. Transaction Semantics

Confirm import validates all questions first, then opens one EF transaction through `QuestionBankService.CreateQuestionsAsync`. If any insert or save fails, the whole import rolls back.

## 15. Known Limitations

- no OCR;
- no scanned/image-only PDF support;
- no AI parser;
- no persistent import sessions;
- no automatic topic or difficulty detection;
- currently imports `SingleChoice` only.

## 16. Future Extensions

Future ideas only:

- OCR for scanned PDFs;
- AI-assisted parsing review;
- XLSX import;
- automatic topic classification;
- persistent import sessions for very large workflows.

## Test Coverage

Unit coverage exists in `tests/Modules/Adeeb.QuestionBank.Tests` for parser rules and DOCX/PDF extraction. Integration tests that require Docker/Testcontainers remain skipped when Docker is unavailable.
