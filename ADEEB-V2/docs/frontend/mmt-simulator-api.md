---
id: Frontend.MmtSimulatorApi
title: MMT Simulator API
module: Mmt
status: Active
auth: Bearer token
frontendReady: true
order: 50
---
# MMT Simulator API

## Overview

The frontend workflow is:

1. Load active, published admission programs for the server-controlled admission year.
2. Create or update the authenticated student's MMT profile and cluster.
3. Replace the student's ordered list of up to 12 admission choices.
4. Submit an already-calculated total score for simulation.
5. Display the returned result and immutable choice snapshots.
6. Load the student's evaluation history and details when needed.
7. Admin users may inspect profiles and evaluations without changing snapshots.

The simulator does not calculate answers or run an exam. Its result is advisory, not an
official admission result.

## Authentication And Authorization

Send `Authorization: Bearer <access-token>` on every route in this document.

| Route group | Requirement |
|---|---|
| `/api/v2/mmt/**` | Authenticated user |
| `/api/v2/admin/mmt/**` | `mmt.manage` |
| `/api/v2/admin/mmt/import/**` | Both `mmt.manage` and `mmt.import` |

Student reads and writes derive ownership from the token's user ID. A caller cannot
select another user's profile or history by supplying a user ID.

## Student Endpoints

### Admission programs

| Method | Path | Result |
|---|---|---|
| `GET` | `/api/v2/mmt/admission-programs` | `PagedResponse<AdmissionProgramListItemDto>` |
| `GET` | `/api/v2/mmt/admission-programs/{id}` | `AdmissionProgramDto` |

Supported list filters are `clusterId`, `universityId`, `specialtyId`,
`admissionType`, `studyForm`, `studyLanguage`, `search`, `page`, and `pageSize`.
`pageSize` is clamped to 1-100. The backend always restricts student results to the
active `Mmt:CurrentAdmissionYear` and to active/published programs with active
references. The student client must not try to override the year or status.

Example response:

```json
{
  "items": [
    {
      "id": "10000000-0000-0000-0000-000000000001",
      "universityId": "20000000-0000-0000-0000-000000000001",
      "universityName": "Tajik National University",
      "specialtyId": "30000000-0000-0000-0000-000000000001",
      "specialtyCode": "LAW-01",
      "specialtyName": "Law",
      "mmtClusterId": "40000000-0000-0000-0000-000000000002",
      "clusterCode": "C2",
      "clusterName": "Cluster 2",
      "admissionType": 0,
      "studyForm": 0,
      "studyLanguage": 0,
      "admissionYear": 2027,
      "seatsCount": 50,
      "isPublished": true,
      "isActive": true,
      "latestPassingScore": 292.0
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

### Student profile

| Method | Path | Body | Result |
|---|---|---|---|
| `GET` | `/api/v2/mmt/profile` | None | `StudentMmtProfileDto` |
| `PUT` | `/api/v2/mmt/profile` | `UpsertStudentMmtProfileDto` | `StudentMmtProfileDto` |

`GET` returns `404 mmt.student_profile_not_found` when the authenticated user has no
active profile for the active year.

```http
PUT /api/v2/mmt/profile
Content-Type: application/json

{
  "mmtClusterId": "40000000-0000-0000-0000-000000000002",
  "admissionYear": 2027,
  "goalAdmissionProgramId": "10000000-0000-0000-0000-000000000001"
}
```

`admissionYear` may be null. If supplied, it must equal the server's active year. The
goal program is optional but, when supplied, must be active, published, and match the
profile cluster and year. Changing the cluster clears existing choices atomically.

Example response:

```json
{
  "id": "50000000-0000-0000-0000-000000000001",
  "userId": "60000000-0000-0000-0000-000000000001",
  "cluster": {
    "id": "40000000-0000-0000-0000-000000000002",
    "name": "Cluster 2",
    "code": "C2",
    "description": null,
    "isActive": true,
    "createdAtUtc": "2026-07-14T07:00:00Z",
    "updatedAtUtc": "2026-07-14T07:00:00Z"
  },
  "admissionYear": 2027,
  "goalAdmissionProgramId": "10000000-0000-0000-0000-000000000001",
  "isActive": true,
  "choicesCount": 0,
  "createdAtUtc": "2026-07-14T07:00:00Z",
  "updatedAtUtc": "2026-07-14T07:00:00Z"
}
```

### Ordered choices

| Method | Path | Body | Result |
|---|---|---|---|
| `GET` | `/api/v2/mmt/profile/choices` | None | `StudentAdmissionChoiceDto[]` |
| `PUT` | `/api/v2/mmt/profile/choices` | `UpsertAdmissionChoicesDto` | `StudentAdmissionChoiceDto[]` |

`PUT` replaces the complete choice set in one transaction. Send `{"choices":[]}` to
clear it.

```http
PUT /api/v2/mmt/profile/choices
Content-Type: application/json

{
  "choices": [
    {
      "admissionProgramId": "10000000-0000-0000-0000-000000000001",
      "priorityOrder": 1
    },
    {
      "admissionProgramId": "10000000-0000-0000-0000-000000000002",
      "priorityOrder": 2
    }
  ]
}
```

The response is ordered by `priorityOrder`; each item embeds an
`AdmissionProgramListItemDto`.

### Simulate and history

| Method | Path | Body/query | Result |
|---|---|---|---|
| `POST` | `/api/v2/mmt/evaluations/simulate` | `SimulateMmtEvaluationDto` | `MmtEvaluationDto` |
| `GET` | `/api/v2/mmt/evaluations` | `studentMmtProfileId`, `admissionYear`, `page`, `pageSize` | `PagedResponse<MmtEvaluationListItemDto>` |
| `GET` | `/api/v2/mmt/evaluations/{id}` | None | `MmtEvaluationDto` |

`userId` in the list query is ignored for student callers; ownership always comes from
the access token.

```http
POST /api/v2/mmt/evaluations/simulate
Content-Type: application/json

{
  "totalScore": 287.50
}
```

Example detail response from simulation or `GET /evaluations/{id}`:

```json
{
  "id": "70000000-0000-0000-0000-000000000001",
  "userId": "60000000-0000-0000-0000-000000000001",
  "studentMmtProfileId": "50000000-0000-0000-0000-000000000001",
  "examSessionId": null,
  "totalScore": 287.50,
  "admissionYear": 2027,
  "clusterId": "40000000-0000-0000-0000-000000000002",
  "evaluatedAtUtc": "2026-07-14T08:30:00Z",
  "acceptedChoicePriority": 2,
  "acceptedAdmissionProgramId": "10000000-0000-0000-0000-000000000002",
  "missingScoreForGoal": 4.50,
  "readinessPercentage": 98.46,
  "motivationalMessageKey": "MMT.Accepted",
  "createdAtUtc": "2026-07-14T08:30:00Z",
  "choices": [
    {
      "id": "80000000-0000-0000-0000-000000000001",
      "priorityOrder": 1,
      "admissionProgramId": "10000000-0000-0000-0000-000000000001",
      "universityName": "Tajik National University",
      "specialtyCode": "LAW-01",
      "specialtyName": "Law",
      "clusterCode": "C2",
      "admissionType": 0,
      "studyForm": 0,
      "studyLanguage": 0,
      "admissionYear": 2027,
      "passingScoreUsed": 292.0,
      "conservativeThresholdUsed": 292.0,
      "studentScore": 287.50,
      "isAccepted": false,
      "missingScore": 4.50
    },
    {
      "id": "80000000-0000-0000-0000-000000000002",
      "priorityOrder": 2,
      "admissionProgramId": "10000000-0000-0000-0000-000000000002",
      "universityName": "Russian-Tajik Slavonic University",
      "specialtyCode": "ECO-02",
      "specialtyName": "Economics",
      "clusterCode": "C2",
      "admissionType": 1,
      "studyForm": 0,
      "studyLanguage": 1,
      "admissionYear": 2027,
      "passingScoreUsed": 280.0,
      "conservativeThresholdUsed": 285.0,
      "studentScore": 287.50,
      "isAccepted": true,
      "missingScore": 0
    }
  ]
}
```

Snapshot names, enum values, scores, thresholds, and acceptance are historical data.
Do not replace them with the current catalog record when rendering old evaluations.

## Admin Endpoints

All paths below start with `/api/v2/admin/mmt` and require `mmt.manage`.

### Catalogs

| Resource | Operations |
|---|---|
| `/clusters` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/status` |
| `/universities` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/status` |
| `/specialties` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/status` |

Catalog lists accept `search`, `isActive`, `page`, and `pageSize`.

### Admission programs and scores

| Method | Path |
|---|---|
| `GET`, `POST` | `/admission-programs` |
| `GET`, `PUT` | `/admission-programs/{id}` |
| `PATCH` | `/admission-programs/{id}/status` |
| `PATCH` | `/admission-programs/{id}/publish` |
| `GET`, `POST` | `/admission-programs/{id}/passing-scores` |
| `GET` | `/admission-programs/{id}/passing-scores/analytics` |
| `PUT`, `DELETE` | `/passing-scores/{id}` |

Admin program lists support all fields in `AdmissionProgramFilter`, including
`admissionYear`, `isPublished`, and `isActive`.

### Excel import

| Method | Path | Notes |
|---|---|---|
| `GET` | `/import/template` | Downloads `mmt-import-template.xlsx` |
| `POST` | `/import/preview` | `multipart/form-data`, no writes |
| `POST` | `/import/confirm` | `multipart/form-data`, transactional writes |

Import routes require both `mmt.manage` and `mmt.import`. Preview fields are `file`,
`createMissingReferences`, `existingScoreMode`, and optional `admissionYear`. Confirm
adds `publishAdmissionPrograms`.

### Student inspection

| Method | Path | Filters/result |
|---|---|---|
| `GET` | `/student-profiles` | `userId`, `admissionYear`, `isActive`, pagination |
| `GET` | `/student-profiles/{id}` | `StudentMmtProfileDto` |
| `GET` | `/evaluations` | `userId`, `studentMmtProfileId`, `admissionYear`, pagination |
| `GET` | `/evaluations/{id}` | `MmtEvaluationDto` |

Admin inspection is read-only. There is no endpoint that mutates an evaluation or its
snapshots.

## DTO Contracts

JSON property names use camel case. `Guid` values are UUID strings and timestamps are
ISO 8601 UTC values.

### AdmissionProgramListItemDto

| Field | Type | Nullable |
|---|---|---|
| `id` | UUID | No |
| `universityId`, `specialtyId`, `mmtClusterId` | UUID | No |
| `universityName`, `specialtyCode`, `specialtyName` | string | No |
| `clusterCode`, `clusterName` | string | No |
| `admissionType`, `studyForm`, `studyLanguage` | integer enum | No |
| `admissionYear` | integer | No |
| `seatsCount` | integer | Yes |
| `isPublished`, `isActive` | boolean | No |
| `latestPassingScore` | decimal | Yes |

### AdmissionProgramDto

| Field | Type | Nullable |
|---|---|---|
| `id` | UUID | No |
| `university` | `UniversityDto` | No |
| `specialty` | `SpecialtyDto` | No |
| `cluster` | `MmtClusterDto` | No |
| `admissionType`, `studyForm`, `studyLanguage` | integer enum | No |
| `admissionYear` | integer | No |
| `seatsCount` | integer | Yes |
| `isPublished`, `isActive` | boolean | No |
| `latestPassingScore` | decimal | Yes |
| `averagePassingScoreLast3Years` | decimal | Yes |
| `conservativeThreshold` | decimal | Yes |
| `createdAtUtc`, `updatedAtUtc` | timestamp | No |

### StudentMmtProfileDto

| Field | Type | Nullable |
|---|---|---|
| `id`, `userId` | UUID | No |
| `cluster` | `MmtClusterDto` | No |
| `admissionYear` | integer | No |
| `goalAdmissionProgramId` | UUID | Yes |
| `isActive` | boolean | No |
| `choicesCount` | integer | No |
| `createdAtUtc`, `updatedAtUtc` | timestamp | No |

### UpsertStudentMmtProfileDto

| Field | Type | Nullable |
|---|---|---|
| `mmtClusterId` | UUID | No |
| `admissionYear` | integer | Yes |
| `goalAdmissionProgramId` | UUID | Yes |

### StudentAdmissionChoiceDto

| Field | Type | Nullable |
|---|---|---|
| `id` | UUID | No |
| `priorityOrder` | integer | No |
| `admissionProgram` | `AdmissionProgramListItemDto` | No |
| `createdAtUtc`, `updatedAtUtc` | timestamp | No |

### UpsertAdmissionChoicesDto

| Field | Type | Nullable |
|---|---|---|
| `choices` | `AdmissionChoiceInputDto[]` | No |

Each input item contains non-null `admissionProgramId` (UUID) and `priorityOrder`
(integer).

### SimulateMmtEvaluationDto

| Field | Type | Nullable |
|---|---|---|
| `totalScore` | decimal | No |

### MmtEvaluationListItemDto

| Field | Type | Nullable |
|---|---|---|
| `id`, `clusterId` | UUID | No |
| `totalScore` | decimal | No |
| `admissionYear` | integer | No |
| `evaluatedAtUtc` | timestamp | No |
| `acceptedChoicePriority` | integer | Yes |
| `acceptedAdmissionProgramId` | UUID | Yes |
| `missingScoreForGoal`, `readinessPercentage` | decimal | Yes |
| `motivationalMessageKey` | string | No |

### MmtEvaluationDto

| Field | Type | Nullable |
|---|---|---|
| `id`, `userId`, `studentMmtProfileId`, `clusterId` | UUID | No |
| `examSessionId` | UUID | Yes; currently null |
| `totalScore` | decimal | No |
| `admissionYear` | integer | No |
| `evaluatedAtUtc`, `createdAtUtc` | timestamp | No |
| `acceptedChoicePriority` | integer | Yes |
| `acceptedAdmissionProgramId` | UUID | Yes |
| `missingScoreForGoal`, `readinessPercentage` | decimal | Yes |
| `motivationalMessageKey` | string | No |
| `choices` | `MmtAdmissionChoiceSnapshotDto[]` | No |

### MmtAdmissionChoiceSnapshotDto

| Field | Type | Nullable |
|---|---|---|
| `id`, `admissionProgramId` | UUID | No |
| `priorityOrder` | integer | No |
| `universityName`, `specialtyCode`, `specialtyName`, `clusterCode` | string | No |
| `admissionType`, `studyForm`, `studyLanguage` | integer enum | No |
| `admissionYear` | integer | No |
| `passingScoreUsed`, `conservativeThresholdUsed` | decimal | Yes |
| `studentScore` | decimal | No |
| `isAccepted` | boolean | No |
| `missingScore` | decimal | Yes |

### PagedResponse

Every paged response contains `items`, `page`, `pageSize`, and `totalCount`. Requested
page values below 1 become 1; page size is clamped to 1-100.

### ProblemDetails

```json
{
  "type": "https://api.adeeb.tj/errors/validation/failed",
  "title": "The submitted data is invalid",
  "status": 422,
  "instance": "/api/v2/admin/mmt/admission-programs/10000000-0000-0000-0000-000000000001/passing-scores",
  "code": "validation.failed",
  "traceId": "0HN...",
  "errors": {
    "passingScore": [
      {
        "code": "mmt.score.invalid",
        "message": "Passing score is invalid"
      }
    ]
  }
}
```

`errors` is present only for field-level validation results. Branch on `code`, never on
localized `title` or validation `message`. Status mapping is: validation `422`, not
found `404`, conflict `409`, unauthorized `401`, forbidden `403`.

## Enum Mapping

### AdmissionType

| Value | Name |
|---:|---|
| 0 | Budget |
| 1 | Contract |

### StudyForm

| Value | Name |
|---:|---|
| 0 | FullTime |
| 1 | PartTime |
| 2 | Distance |
| 3 | Other |

### StudyLanguage

| Value | Name |
|---:|---|
| 0 | Tajik |
| 1 | Russian |
| 2 | English |
| 3 | Other |

### UniversityType

| Value | Name |
|---:|---|
| 0 | Public |
| 1 | Private |
| 2 | Other |

### ExistingScoreMode

| Value | Name |
|---:|---|
| 0 | SkipExisting |
| 1 | UpdateExisting |
| 2 | FailOnExisting |

## Frontend Validation Rules

- A profile requires a non-empty active cluster ID.
- Optional `admissionYear` must equal the server-configured current admission year.
- A goal program must be active, published, and in the profile's cluster and year.
- Choices may contain at most 12 items; an empty array clears the list.
- Priorities must be unique and consecutive from 1 through the number of choices.
- The same admission program cannot appear twice.
- Every choice must be active, published, in the profile cluster, and in the active year.
- Changing the profile cluster clears incompatible choices; reload choices after update.
- Simulation accepts only an already-calculated total score from 0 through 1,000 with
  at most two decimal places.
- Simulation requires at least one choice even though clearing choices is supported.
- Revalidate on server errors because publication and score data can change after load.

## Result UX

Do not label a non-accepted simulation as "failed". Use the machine key to choose the
presentation while displaying localized copy:

| Key | UI meaning |
|---|---|
| `MMT.Accepted` | At least one choice reaches its conservative threshold |
| `MMT.NearMiss` | No accepted choice; nearest gap is at most 10% of its threshold |
| `MMT.ProgressNeeded` | More score progress is needed |
| `MMT.NoThresholdData` | No selected choice has usable score history |

When accepted, highlight `acceptedChoicePriority` and
`acceptedAdmissionProgramId`. Otherwise show the nearest available `missingScore`.
Show `readinessPercentage` when non-null. If threshold data is unavailable, hide
percentage/gap widgets and display: "Маълумоти балли гузариш ҳоло пурра нест".

The top-level `missingScoreForGoal` and `readinessPercentage` refer to the explicit
goal program, or priority 1 when no goal was selected. They do not necessarily refer to
the nearest or accepted choice.

## Example Frontend Flow

1. The user selects Cluster 2 and the client calls `PUT /api/v2/mmt/profile`.
2. The client loads current Cluster 2 programs and submits 12 unique choices with
   priorities 1-12.
3. The user completes an external score flow and the client submits `287.50` to
   `/evaluations/simulate`.
4. If priority 1 needs `292.00` but priority 2 needs `285.00`, the response sets
   `acceptedChoicePriority` to 2 and `MMT.Accepted`.
5. The result card highlights priority 2 and the snapshot list shows all 12 choices as
   evaluated. A later catalog or score edit must not change this historical screen.
6. If no threshold is reached, the client uses the returned message key, nearest
   `missingScore`, and optional goal readiness instead of showing failure copy.

## Stable MMT Error Codes

These are actual module codes. Several frontend situations intentionally share one
code because the backend does not reveal internal catalog state to student routes.

| Code | HTTP | Meaning/action |
|---|---:|---|
| `mmt.user_required` | 401 | Token has no usable user ID; reauthenticate |
| `mmt.student_profile_not_found` | 404 | No active profile for current year; open profile setup |
| `mmt.evaluation_not_found` | 404 | Evaluation absent or not owned by this student |
| `mmt.program_not_found` | 404 | Program is absent or not student-visible |
| `mmt.reference_inactive` | 409 | Selected profile cluster/reference is missing or inactive |
| `mmt.admission_year_unavailable` | 409 | Requested profile year is not active |
| `mmt.goal_program_invalid` | 409 | Goal is unpublished, inactive, wrong cluster, or wrong year |
| `mmt.choice_program_invalid` | 409 | At least one choice is unpublished, inactive, wrong cluster, or wrong year |
| `mmt.too_many_choices` | 422 | More than 12 choices |
| `mmt.duplicate_choice_program` | 422 | Duplicate admission program |
| `mmt.duplicate_choice_priority` | 422 | Duplicate priority |
| `mmt.invalid_choice_order` | 422 | Priorities are not consecutive from 1 |
| `mmt.choices_required` | 409 | Simulation attempted with no choices |
| `mmt.evaluation_score_invalid` | 422 | Score outside 0-1000 or more than two decimal places |
| `mmt.profile_conflict` | 409 | Concurrent active-profile creation/update; reload and retry |
| `mmt.choice_update_conflict` | 409 | Concurrent choice replacement; reload and retry |
| `mmt.program_exists` | 409 | Duplicate admin admission program |
| `mmt.score_exists` | 409 | Duplicate program/year passing score |
| `mmt.import_file_invalid` | 422 | Workbook/form input invalid; inspect `errors` |
| `mmt.import_existing_score` | 409 | `FailOnExisting` found an existing score |
| `mmt.import_conflict` | 409 | Concurrent catalog/import write; preview fresh data and retry |
| `mmt.program_publish_invalid` | 409 | Program or references cannot be published |

Additional admin not-found and duplicate codes exist for cluster, university,
specialty, and score resources. Handle unknown `mmt.*` codes with the localized title
and log `traceId` for support.

## Known Remaining Product Gaps

- Full exam/question execution and total-score calculation are not implemented.
- XP/AdeebCoin reward integration is not implemented.
- Scheduled exam opening on the 1st and 16th is not implemented.
- Excel imports do not yet persist an `ImportBatch` audit history.
- `examSessionId` is reserved and currently null.
- Simulation must never be presented as an official admission result.
