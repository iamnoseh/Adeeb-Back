---
id: Flow.Localization
title: Localization Flow
module: Flows
status: Stable
auth: Mixed
frontendReady: true
order: 30
---
# Localization Flow

## Supported Cultures
- `tg-TJ`
- `ru-RU`
- `en-US`

## Precedence
1. Authenticated user's stored preferred language.
2. `X-Adeeb-Language`.
3. `Accept-Language`.
4. Default `tg-TJ`.

## Frontend Rule
Frontend branches on stable `code`. Frontend displays localized `title` or validation `message`.

## Example
```json
{
  "status": 401,
  "code": "auth.invalid_credentials",
  "title": "Invalid login credentials",
  "traceId": "..."
}
```
