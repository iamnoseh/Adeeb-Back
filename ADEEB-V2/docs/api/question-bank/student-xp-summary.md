---
id: QuestionBank.StudentXpSummary
title: Student XP Summary API
method: GET
route: /api/v2/student/xp
status: active
---

# Student XP Summary

## Endpoint

```text
GET /api/v2/student/xp
```

The endpoint requires an authenticated `User` principal and reads only that user's global XP balance.

## Response

```json
{
  "totalXp": 22,
  "updatedAtUtc": "2026-07-21T08:30:00Z"
}
```

If the student has never earned XP, `totalXp` is `0` and `updatedAtUtc` is `null`. XP is already converted from integer persistence units; clients must display it without recalculation.

## Security

The endpoint accepts no user ID, source, amount, or adjustment data. It cannot grant XP and does not expose ledger metadata or another user's balance.
