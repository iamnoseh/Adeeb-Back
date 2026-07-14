# MMT Data Management

## Scope

The MMT module owns clusters, universities, specialties, admission programs, and
passing-score history in the PostgreSQL `mmt` schema. It prepares published admission
data for the future MMT Simulator and 12-choice admission workflow; it does not create
student choices or evaluation history yet.

## Configuration

Set `ConnectionStrings:Mmt`. The module falls back to `ConnectionStrings:Default` and
then `ConnectionStrings:Identity` for its DbContext, while readiness checks require an
explicit `Mmt` or `Default` connection. Apply migration
`20260714065717_AddMmtDataManagement` through the existing database initialization
process or normal EF migration deployment process.

## Authorization

- `mmt.manage`: all admin CRUD, status, publish, and passing-score routes.
- `mmt.import`: template, preview, and confirm routes, in addition to `mmt.manage` on
  the parent route group.
- `Admin` and `SuperAdmin` receive both permissions. ContentAdmin, FinanceAdmin,
  SupportAdmin, and User do not.
- Student read routes require an authenticated principal and expose only active,
  published programs for the current year whose cluster, university, and specialty
  are active.

## Routes

All routes use the `/api/v2` convention.

- Admin catalogs: `/api/v2/admin/mmt/clusters`, `/universities`, `/specialties` with
  list, get, create, update, and `PATCH {id}/status` operations.
- Admission programs: `/api/v2/admin/mmt/admission-programs` with list, get, create,
  update, status, publish, score history, and score analytics operations.
- Passing score update/delete: `/api/v2/admin/mmt/passing-scores/{id}`.
- Import: `/api/v2/admin/mmt/import/template`, `/preview`, and `/confirm`.
- Student reads: `/api/v2/mmt/admission-programs` and `/{id}`.

## Excel import

Only `.xlsx` files are accepted. Uploads are limited to 5 MB, expanded package data
to 50 MB, package entries to 1,000, and data rows to 5,000. Preview and confirm parse
the workbook independently; confirm never trusts client-returned preview rows.

Required template columns are:

`Year`, `ClusterCode`, `ClusterName`, `UniversityFullName`,
`UniversityShortName`, `UniversityCity`, `UniversityType`, `SpecialtyCode`,
`SpecialtyName`, `AdmissionType`, `StudyForm`, `StudyLanguage`, `SeatsCount`,
`PassingScore`, `Source`, and `Note`.

`AdmissionYear` supplies a default when a row's `Year` is blank.
`CreateMissingReferences=false` makes absent references invalid. Inactive references
are always invalid. `ExistingScoreMode` values are `0` SkipExisting, `1`
UpdateExisting, and `2` FailOnExisting. FailOnExisting aborts confirmation before any
write if an existing score is detected. Other invalid rows are reported and skipped;
valid rows are imported in one transaction. Concurrent unique-key races roll back and
return `mmt.import_conflict`.

## Data rules

- Codes are Unicode Form C, whitespace-normalized, trimmed, and uppercased.
- University duplicate detection uses a normalized full-name key.
- Program identity is university + specialty + cluster + admission type + study form
  + study language + admission year.
- Passing score identity is admission program + score year.
- Scores must be greater than zero, at most 1,000, and use at most two decimal places.
- Cluster and specialty hard-delete endpoints are intentionally absent. Deactivation
  preserves referenced admission data.
- Passing-score deletion is currently allowed because locked student evaluation
  history is not part of this phase. That operation must gain a reference check when
  evaluation history is introduced.

## Next phase

Add `StudentMmtProfile`, ordered admission choices capped at 12, immutable evaluation
snapshots, and simulator evaluation using the conservative threshold returned by this
module.
