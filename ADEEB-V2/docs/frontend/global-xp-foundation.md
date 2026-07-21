---
id: Frontend.GlobalXpFoundation
title: Global XP Frontend Contract
module: Frontend
status: Stable
auth: Student
frontendReady: true
order: 70
---

# Global XP Frontend Contract

## Purpose

ADEEB has one global XP balance backed by an append-only ledger. The frontend never calculates, grants, or adjusts XP. It only renders values returned by trusted backend workflows.

The current public student contract exposes XP earned by a completed test and the student's global XP balance. It does not expose levels, ledger history, or XP from future sources.

## Units

The backend persists integer units where `1 XP = 2 units`. Public API DTOs already return converted XP values as JSON numbers. The frontend must display those values directly and must not divide, multiply, round, or reconstruct XP from question difficulty.

Valid examples are `0`, `1.5`, `2`, `2.5`, and `5`.

## Active Source

Only `TestAttempt` grants XP in the current release. Vocabulary, Duel, Mission, DailyTask, Streak, ClanCompetition, Achievement, Event, and AdminAdjustment are reserved backend source types and must not appear as active reward sources in the frontend.

## Trust Boundary

- Never send XP amount, source, balance, or reward status from the browser.
- Never derive XP from answer correctness on the client.
- Never optimistically increase a balance after submit.
- Never expose an arbitrary XP grant control.
- Treat the persisted result endpoint as the source of truth.
- Branch on stable machine fields, not localized text.

## Current API Surface

XP is returned by:

```text
POST /api/v2/student/tests/attempts/{attemptId}/submit
GET  /api/v2/student/tests/attempts/{attemptId}/result
GET  /api/v2/student/tests/history
GET  /api/v2/student/xp
```

Submit and result retrieval return the same persisted test XP breakdown. History returns the persisted `totalXp` and `xpAwarded` for every attempt. The XP summary endpoint returns the global balance used in the student top bar.

There is intentionally no frontend API function for level or ledger history until dedicated backend endpoints exist.

## Display Rules

- Show `totalXp` as the primary reward value.
- Show `answerXp` and `completionBonusXp` as the persisted breakdown.
- Show correct-answer counts by Easy, Medium, and Hard difficulty.
- Show the awarded state only when `xpAwarded` is true.
- For zero XP, show `0 XP` and a neutral explanation; do not show a success reward badge.
- Historical results without an XP settlement remain valid and display zero values.
- The top bar displays only `GET /api/v2/student/xp.totalXp`; it never sums history rows.

## Query Behavior

The result query key is scoped by attempt ID and UI language. A language switch refetches the result so localized answer content changes while numeric XP values remain stable.

The result is immutable after completion. Normal React Query caching is allowed, but a failed fetch must show the standard retry state rather than estimated reward data.

## Future Extension

A future progression extension may add levels and ledger history. Until those APIs are implemented, the frontend must not fabricate them from test history because other reward sources and administrative adjustments would make the calculation incomplete.
