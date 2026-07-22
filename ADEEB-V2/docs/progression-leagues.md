# Progression leagues

`Progression` owns league definitions, 10-day seasons, membership, league score events, movement results, and student league views. The QuestionBank XP ledger remains the source of truth for all XP.

## Rules

- A season lasts exactly 10 x 24 hours.
- League score includes positive learning/reward XP earned inside the season window.
- Administrative adjustments and settlement entries are excluded.
- Initial membership uses contiguous lifetime-XP thresholds. Later seasons use promotion and relegation results.
- Movement per league is `min(10, floor(activeParticipants / 3))`.
- Ties are ordered by score, then earliest time reaching the score, then user ID.
- Students with zero lifetime XP are unranked. Suspended or closed students are excluded.

## Delivery and reconciliation

Every XP grant writes an outbox record in the same database transaction as the ledger entry. The Progression consumer stores the ledger ID with a unique constraint, making duplicate delivery harmless. At season closure, scores are reconciled from the authoritative XP ledger before final ranks are written.

The first season is started by an administrator. When automatic renewal is enabled, the background worker closes an expired season and creates its successor under a PostgreSQL advisory transaction lock.

## Operations

The database connection string is resolved from `Progression`, then `Default`, then `Identity`. Readiness health includes `progression-db`. League images accept PNG, JPEG, or WebP up to 2 MB and are validated from file content.

Changing thresholds, order, or status is blocked during an active season. Disable automatic renewal, allow the season to finish, edit the structure, and start the next season manually. Names and avatars remain editable during a season.

