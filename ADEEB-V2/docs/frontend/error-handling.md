---
id: Frontend.ErrorHandling
title: Error Handling
module: Frontend
status: Stable
auth: Not applicable
frontendReady: true
order: 10
---
# Error Handling

## ProblemDetails Shape
```json
{
  "type": "https://api.adeeb.tj/errors/auth/invalid-credentials",
  "title": "Invalid login credentials",
  "status": 401,
  "code": "auth.invalid_credentials",
  "traceId": "...",
  "errors": null
}
```

## Frontend Rules
- `code` is for machine logic.
- `title` and validation `message` are for localized display.
- `traceId` is for support/debug correlation.
- Never branch on translated title.

## Dart Example
```dart
switch (problem.code) {
  case 'auth.invalid_credentials':
    showError(problem.title);
    break;
  case 'rate_limit.too_many_requests':
    temporarilyDisableSubmit();
    break;
}
```
