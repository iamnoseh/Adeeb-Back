---
id: Frontend.Localization
title: Frontend Localization
module: Frontend
status: Stable
auth: Not applicable
frontendReady: true
order: 40
---
# Frontend Localization

## API Languages
ADEEB API supports `tg-TJ`, `ru-RU`, and `en-US`.

## Headers
Send one of:
```http
X-Adeeb-Language: tg-TJ
Accept-Language: tg-TJ
```

## Display Rule
Use server localized text for API errors. Flutter/React owns static UI labels.

## Stable Codes
Do not translate stable error codes.
