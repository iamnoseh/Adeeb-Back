---
id: Frontend.TestXpResult
title: Test XP Result Integration
module: Frontend
status: Stable
auth: Student
frontendReady: true
order: 71
---

# Test XP Result Integration

## Response Contract

The following fields are additive parts of `TestResultDto` and are present on submit and result retrieval:

```json
{
  "easyCorrect": 4,
  "mediumCorrect": 3,
  "hardCorrect": 2,
  "answerXp": 17,
  "completionBonusXp": 5,
  "totalXp": 22,
  "xpAwarded": true
}
```

| Field | Type | Meaning |
| --- | --- | --- |
| `easyCorrect` | integer | Correct answers whose immutable attempt snapshot is Easy |
| `mediumCorrect` | integer | Correct answers whose immutable attempt snapshot is Medium |
| `hardCorrect` | integer | Correct answers whose immutable attempt snapshot is Hard |
| `answerXp` | number | XP earned from correct answers |
| `completionBonusXp` | number | Completion bonus; zero when no answer is correct |
| `totalXp` | number | Persisted total for this attempt |
| `xpAwarded` | boolean | True only when `totalXp` is greater than zero |

## Zero-XP Result

```json
{
  "easyCorrect": 0,
  "mediumCorrect": 0,
  "hardCorrect": 0,
  "answerXp": 0,
  "completionBonusXp": 0,
  "totalXp": 0,
  "xpAwarded": false
}
```

This is a completed and audited settlement, not a loading or missing-data state.

## Result Page Composition

1. Keep score, percentage, correct, and wrong metrics unchanged.
2. Render a dedicated XP summary directly below the primary metrics.
3. Use `totalXp` as the visual focus.
4. Render answer XP and completion bonus as compact breakdown rows.
5. Render Easy, Medium, and Hard correct counts without recalculating reward values.
6. Keep answer review and subject/topic breakdown below the XP summary.

## History Contract

Every `TestHistoryItemDto` contains:

```json
{
  "totalXp": 22,
  "xpAwarded": true
}
```

The history page displays these persisted values beside the score. Historical attempts without a settlement return `0` and `false`; the frontend must not recalculate them.

## Localization

All visible labels and explanations must exist in both `tg-TJ` and `ru-RU`. The literal abbreviation `XP` is shared product terminology and is not translated. Numbers are formatted with the active UI locale while preserving half-XP precision.

## Error And Retry Behavior

- A failed result request uses the standard testing error state and retry action.
- Do not retain a reward value from another attempt.
- Do not replace a failed request with zero XP.
- Duplicate submit keeps the existing `test.attempt_already_submitted` behavior; the result route retrieves the persisted settlement.

## Accessibility

- The XP section has a visible heading.
- Reward status is conveyed by text as well as color.
- Numeric breakdown labels remain readable on mobile without horizontal scrolling.
- Decorative icons are hidden from assistive technology.
