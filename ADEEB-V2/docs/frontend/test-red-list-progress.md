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

The student UI shows a compact Red List label and segmented `correctStreak / requiredCorrectStreak` progress above the question. Progress is updated by the backend only after the whole attempt is submitted.
