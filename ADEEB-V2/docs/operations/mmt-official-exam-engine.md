# MMT Official Exam Engine

## Purpose

The MMT exam engine reproduces Part A as a versioned, server-authoritative exam. It is
separate from the admission simulator: the simulator evaluates a supplied total score,
while this engine selects questions, scores answers, and converts raw subtest scores by
the configured official table.

## Official structure

- Every exam has four ordered subtests: `A1`, `A2`, `A3`, and `A4`.
- Every subtest has a maximum raw score of 40.
- Single-choice answers score 1 point, matching answers score one point per correct pair,
  and short answers score 2 points.
- Question counts and total duration follow the official MMT policy for the student's
  existing cluster. They are not selected by the student or recalculated by the client.
- Scale conversion is an exact lookup by exam version, cluster, subtest, specialty
  range, and raw score. The backend does not interpolate missing values.
- `A1` uses a cluster-wide scale. `A2`-`A4` use the student's specialty range.
- A competition total is returned only when every required minimum raw score is met.

## Existing MMT workflow

The feature reuses the existing MMT catalog and student testing workflow:

- the student's existing MMT profile supplies the cluster;
- the subjects already assigned to that cluster supply `A1`-`A4`;
- the existing `MmtPractice` and `MonthlyExam` modes start and resume attempts;
- the backend applies the official question mix, duration, and raw scoring rules;
- the annual scale dataset is reference data, not a business-admin form.

There is no separate admin page for recreating clusters, test modes, or uploading a
technical JSON configuration. Annual official scale data remains versioned internally so
historical results cannot change when the next year's table is introduced.

## Student workflow

- Starting an MMT practice or monthly exam resolves the published version for the
  student's admission year and selected cluster.
- The server selects the exact number and type of questions required by each subtest.
- The attempt stores immutable exam, section, scoring, and option-order snapshots.
- Answers are autosaved through the draft-answer endpoint. A refresh or reconnect
  restores the same attempt and saved answers.
- The client timer uses the server expiry. Submission is always scored by the backend.
- Monthly exams resume an existing in-progress attempt for the same window.
- Immediate correctness is not revealed during MMT practice or monthly exams.

The result contains raw `A1`-`A4` scores and a separate scaled total for each ordered
admission choice. A failed minimum is explicit and does not receive a competition total.
Official and reference scale versions are visibly distinguished.

## Operations

Apply both generated migrations before enabling the feature:

- MMT: `AddOfficialMmtExamEngine`
- QuestionBank: `AddMmtAttemptSnapshotsAndDrafts`

Do not publish a version until its source document and checksum have been independently
reviewed. Changing the configured admission year or score tables requires a new draft,
not an update to historical attempts.
