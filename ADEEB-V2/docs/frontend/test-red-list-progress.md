---
id: Frontend.TestRedListProgress
title: Red List Progress in Test Attempts
module: Frontend
status: Stable
auth: Student
frontendReady: true
order: 72
---

# Red List Progress in Test Attempts

`TestQuestionDto.redListProgress` is `null` for a regular question. A question selected from the student's active Red List receives this snapshot:

```json
{
  "correctStreak": 1,
  "requiredCorrectStreak": 3,
  "correctAnswersRemaining": 2
}
```

The values are captured when the attempt starts and remain stable when the attempt is resumed. They do not expose correctness, the correct option, or an answer mapping.

The student UI shows a compact Red List label and segmented `correctStreak / requiredCorrectStreak` progress above the question.

## Immediate answer check

Subject tests and Red List practice support per-question checking:

```text
POST /api/v2/student/tests/attempts/{attemptId}/questions/{questionId}/check
```

The request uses the same answer fields as final submission, except `questionId` comes from the route. A successful response includes correctness, correction/explanation, and an optional `redList` transition. Repeating the same request returns the stored result and does not update progress or XP again. MMT practice and monthly exams reject immediate checking and continue to conceal correctness until completion.

Red List action values are:

```text
0 None
1 Added
2 Progressed
3 Reset
4 Mastered
```

When action `4` is reached, the response includes `masteryBonusXp = 1`. `masteryBonusAwarded` is true only for the first successful ledger grant, and `totalXp` contains the new global XP balance. The UI may show an auto-dismiss congratulation toast only when `masteryBonusAwarded` is true.

Final submission reuses already checked answers. Unchecked answers are still finalized server-side, including Red List transitions and the one-time mastery bonus. Red List practice uses the reduced reward multiplier configured by `TestXpRewards:RedListPracticeRewardPercent` (50 percent by default); the separate mastery bonus remains exactly 1 XP.
