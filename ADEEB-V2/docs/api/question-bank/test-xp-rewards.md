---
id: QuestionBank.GlobalXpFoundation
title: Global XP Foundation and Test Settlements
status: active
---

# Global XP foundation and test settlements

ADEEB stores XP as integer units (`1 XP = 2 units`). Shared contracts live in BuildingBlocks so future modules can grant XP without depending on QuestionBank:

- `Adeeb.SharedKernel.Progression`: global ledger, balance, source and entry types.
- `Adeeb.Application.Abstractions.Progression`: trusted internal grant contract and typed errors.
- QuestionBank persistence: the current transaction-aware PostgreSQL implementation.

There is no public arbitrary-grant endpoint. QuestionBank verifies attempt ownership, evaluates answers, calculates XP from the immutable attempt snapshot, and passes a server-created request to `IStudentXpService`.

## Global storage

`question_bank.xp_ledger_entries` is append-only. It records source identity, entry semantics, amount, idempotency key, balance transition, metadata and timestamp. Database uniqueness is enforced for `idempotency_key` and `(user_id, source_type, source_id)`.

`question_bank.student_xp_balances` is the global balance source of truth. Credits use a parameterized PostgreSQL `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` statement, preventing first-row races and lost updates without relying on a QuestionBank user lock.

Positive test XP creates a `Credit`. A completed attempt with zero correct answers creates a zero-value `Settlement`, preserving audit and idempotency without creating or changing a balance.

## Test settlement

`question_bank.test_xp_settlements` preserves the reportable test breakdown and references both the test attempt and global ledger entry. Attempt and ledger foreign keys use `RESTRICT`, so audit records are not silently cascade-deleted.

Cross-module user foreign keys are intentionally absent: module boundaries do not allow QuestionBank persistence to reference Identity or Students tables. User existence and ownership are established by the authenticated attempt flow; user deactivation does not erase audit history.

## Transactions and concurrency

QuestionBank starts one transaction before finalization. Answer evaluation, Red List changes, global ledger insertion, atomic balance credit, test settlement, attempt completion and result insertion all commit or roll back together.

Attempt finalization uses an attempt-scoped PostgreSQL advisory lock. Global balance concurrency is independent and handled by the atomic upsert plus ledger uniqueness. A future module requiring atomic completion must call the service inside a supported transaction boundary or introduce an outbox if persistence moves to a separate database.

## Migration

`RefactorGlobalXpFoundation` renames the balance table without resetting totals, backfills every positive and zero test reward into the ledger, converts reward rows to linked settlements, validates existing totals, and never credits migrated amounts again. Down migration is blocked if a non-test source exists because the legacy schema cannot represent it safely.

## API compatibility

Routes and duplicate-submit behavior remain unchanged. The result fields remain `easyCorrect`, `mediumCorrect`, `hardCorrect`, `answerXp`, `completionBonusXp`, `totalXp`, and `xpAwarded`.

Vocabulary, Duel, Mission, Streak, ClanCompetition and Achievement are declared source types only. They do not grant XP in this phase.
