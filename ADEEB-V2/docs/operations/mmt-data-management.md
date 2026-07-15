# MMT Data Management Operations

## Scope

The MMT module owns two completed phases in the PostgreSQL `mmt` schema:

- Phase 1 owns the admin catalog, Excel import, admission programs, publication state,
  and passing-score history.
- Phase 2 owns the authenticated student's MMT profile, up to 12 ordered admission
  choices, score simulation, evaluation history, and immutable result snapshots.

The module does not implement a full exam/question engine. Simulation accepts a total
score already calculated by another process and must not be presented as an official
admission decision.

## Correct MMT Domain

- `Specialty` is a reusable academic specialty and does not own a passing score.
- `University` is an educational institution and does not own a passing score.
- `AdmissionProgram` is the selectable combination of university, specialty, cluster,
  admission type, study form, study language, admission year, and seats.
- `PassingScoreHistory` belongs to exactly one `AdmissionProgram`; its identity within
  that program is `Year + DistributionRound`.
- The same specialty may appear at many universities with different scores. The same
  university/specialty pair may also have separate Budget/Contract, form, and language
  combinations.

For example, Law at DMT Budget 2025 Main may be 288, Law at DMT Contract 2025 Main
may be 231, and Law at Khujand State University Budget 2025 Main may be 260. These are
three different admission programs, not three scores attached to a global specialty.
Passing scores are historical simulation data and never guarantee official admission.

## Configuration

Set a dedicated MMT connection string whenever possible:

```powershell
dotnet user-secrets set "ConnectionStrings:Mmt" "Host=localhost;Port=5432;Database=adeeb_mmt;Username=adeeb;Password=<local-password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
```

`MmtDbContext` resolves `ConnectionStrings:Mmt`, then `Default`, then `Identity`.
Readiness checks require `Mmt` or `Default`; an empty effective connection string fails
startup. Do not commit credentials to `appsettings*.json`.

The active simulator year is configured independently:

```json
{
  "Mmt": {
    "CurrentAdmissionYear": 2027
  }
}
```

When `CurrentAdmissionYear` is `null`, the application clock's UTC year is used. The
same active year controls student program visibility, profile access, choices, and
simulation. Admin catalog queries may inspect any valid year from 2000 through 2100.

## Migrations

Apply the MMT migrations in order:

1. `20260714065717_AddMmtDataManagement`
2. `20260714104237_AddMmtSimulatorPhase2`
3. `20260714124749_AddMmtLocalization`
4. `20260715072716_AddMmtClusterSubjects`
5. `20260715105433_AddMmtDistributionRound`

The localization migration backfills Russian fields from existing values before
removing temporary column defaults. Existing rows therefore remain readable and can
be translated incrementally through language-specific admin updates.

The distribution-round migration backfills every existing score as `Main`, replaces
the old program/year unique index, and preserves all score rows.

With the API configuration available, this command applies every pending MMT migration:

```powershell
dotnet ef database update --project src/Modules/Mmt/Adeeb.Modules.Mmt/Adeeb.Modules.Mmt.csproj --startup-project src/Hosts/Adeeb.Api/Adeeb.Api.csproj --context MmtDbContext
```

The host also calls `MmtDatabaseInitializer` when `DatabaseInitialization:AutoMigrate`
is enabled. Keep automatic migration disabled in production unless the deployment
procedure explicitly owns schema migration.

## Authorization

- Authenticated users can read current published programs and manage only their own
  profile, choices, and evaluations.
- `mmt.manage` is required by the parent admin group for catalog CRUD, publication,
  scores, imports, and simulator inspection.
- Import routes additionally require `mmt.import`; callers therefore need both
  `mmt.manage` and `mmt.import`.
- `Admin` and `SuperAdmin` receive both permissions by the current role mapping.

## Admin Workflow

### 1. Maintain reference data

Create or activate clusters, universities, and specialties under
`/api/v2/admin/mmt`. A program can be published only when it and all three references
are active.

### 2. Import Excel data

1. Download `GET /api/v2/admin/mmt/import/template`.
2. Fill the `.xlsx` template without renaming required columns.
3. Send `POST /api/v2/admin/mmt/import/preview` as `multipart/form-data`.
4. Review every invalid or duplicate row.
5. Send the original workbook to `POST /api/v2/admin/mmt/import/confirm`.

Preview and confirm parse the workbook independently; confirm does not trust rows sent
back by a client. Limits are 5 MB upload, 50 MB expanded package data, 1,000 package
entries, and 5,000 data rows.

Required columns are:

`Year`, `ClusterCode`, `ClusterName`, `UniversityFullName`,
`UniversityShortName`, `UniversityCity`, `UniversityType`, `SpecialtyCode`,
`SpecialtyName`, `AdmissionType`, `StudyForm`, `StudyLanguage`, `SeatsCount`,
`PassingScore`, `Source`, and `Note`. New templates also include
`DistributionRound` (`Main`, `Repeat`, `Additional`, or `Other`). For backward
compatibility, an older workbook without that column is imported as `Main`.

Important form fields:

- `CreateMissingReferences`: creates missing active cluster, university, and specialty
  records when true. Inactive references remain invalid.
- `AdmissionYear`: supplies a default only when a row's `Year` is blank.
- `ExistingScoreMode`: `0` skip, `1` update, `2` fail confirmation.
- `PublishAdmissionPrograms`: publishes imported programs during confirmation when true.

Valid rows are written in one transaction. `mmt.import_conflict` means a concurrent
catalog change won the unique-key race; download/review fresh data and retry preview
and confirm.

### 3. Publish programs

Review the program with `GET /api/v2/admin/mmt/admission-programs/{id}`, then call:

```http
PATCH /api/v2/admin/mmt/admission-programs/{id}/publish
Content-Type: application/json

{"isPublished":true}
```

Publication does not override inactive references. Deactivating a program also
unpublishes it.

### 4. Verify student visibility

Use an authenticated student token and call:

```http
GET /api/v2/mmt/admission-programs?clusterId={clusterId}&page=1&pageSize=10
```

A visible program must be active, published, in the configured active admission year,
and linked to active cluster, university, and specialty records.

## Passing-Score Corrections

Use `PUT /api/v2/admin/mmt/passing-scores/{id}` to correct a row in place. Score
uniqueness is `AdmissionProgramId + Year + DistributionRound`. Use
`ExistingScoreMode=1` only when an import is intentionally authoritative. After a
correction, verify analytics at
`GET /api/v2/admin/mmt/admission-programs/{id}/passing-scores/analytics`.

Avoid deleting score rows after evaluations exist. Existing evaluation snapshots keep
their original `passingScoreUsed` and `conservativeThresholdUsed`, so deletion does not
rewrite history, but it changes future simulations and reduces the audit trail. If a
row is wrong, update it and preserve source/note context. Deletion is technically
available only for an intentionally invalid or duplicate historical row.

Do not delete or directly rewrite student profiles, evaluations, or snapshot rows.
Snapshots are the evidence of what the simulator used at evaluation time. Catalog
hard-delete endpoints are intentionally absent; deactivate or unpublish catalog data.

## Troubleshooting Missing Student Programs

Check these conditions in order:

1. `Mmt:CurrentAdmissionYear` matches the program year.
2. The admission program has `isActive=true` and `isPublished=true`.
3. Its cluster, university, and specialty are all active.
4. Frontend filters use the correct IDs and numeric enum values.
5. The student is authenticated and calling `/api/v2/mmt`, not the admin route.
6. The program was not deactivated after it was added to a profile.

Changing a student's profile cluster atomically clears choices from the old cluster.
A program that later becomes inactive, unpublished, or moves out of the active year is
rejected by choice replacement and by simulation.

## Simulator Semantics

- A user has at most one active profile per admission year.
- `PUT /api/v2/mmt/profile/choices` replaces the full list atomically. An empty list
  clears it; a non-empty list must have unique programs and priorities consecutive from
  1 through its count, with a maximum count of 12.
- Simulation accepts a total score from 0 through 1,000 with at most two decimal places.
- The conservative threshold is the maximum of the latest Main-distribution score and
  the rounded average of the latest three Main-distribution score records. Repeat,
  Additional, and Other rounds are retained for history but do not affect simulation.
- The first ordered choice whose threshold is reached is accepted.
- Evaluation and all choice snapshots are saved in one repeatable-read transaction.
- Missing threshold data yields null readiness/missing-score values and
  `MMT.NoThresholdData`.
- `ExamSessionId` is currently null and reserved for a future exam engine.

## Known Remaining Product Gaps

- Full exam/question execution and score calculation are not implemented.
- XP/AdeebCoin reward integration is not implemented.
- Scheduled exam opening on the 1st and 16th is not implemented.
- Import has no persisted `ImportBatch` audit aggregate; workbook results are returned
  to the caller but are not stored as a batch history.
- Simulation is advisory and must not be treated as an official admission result.
