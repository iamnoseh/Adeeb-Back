---
id: Auth.Register
title: Register
module: Identity
method: POST
route: /api/v2/auth/register
status: Stable
auth: Anonymous
frontendReady: true
order: 10
---
# Register

## 1. Endpoint
`POST /api/v2/auth/register`

## 2. Purpose
Корбари нав месозад, password-ро hash мекунад, session-и device-aware месозад ва access/refresh token бармегардонад.

## 3. Status
Stable.

## 4. Module
Identity.

## 5. Authentication
Anonymous.

## 6. Authorization
Not applicable.

## 7. Rate Limit
Default: 3 requests / 5 minutes by endpoint, IP and anonymous user partition.

## 8. Localization
Errors бо `X-Adeeb-Language`, `Accept-Language`, ё default `tg-TJ` localize мешаванд.

## 9. Request Headers
- `Content-Type: application/json`
- `Accept-Language: tg-TJ | ru-RU | en-US`
- Optional: `X-Adeeb-Language`
- Optional device headers: `X-Adeeb-Device-Id`, `X-Adeeb-Device-Name`, `X-Adeeb-Platform`, `X-Adeeb-App-Version`

## 10. Path Parameters
Not applicable.

## 11. Query Parameters
Not applicable.

## 12. Request Body
```json
{
  "email": "noseh@example.com",
  "phoneNumber": "+992900000000",
  "password": "StrongPassword123",
  "firstName": "Noseh",
  "lastName": "Tagaymurodzoda",
  "language": "tg-TJ",
  "device": {
    "deviceId": "device-123",
    "deviceName": "Samsung S24",
    "platform": "android",
    "appVersion": "1.0.0"
  }
}
```
`device` optional аст; агар набошад backend аз request metadata fallback месозад.

## 13. Field Rules
- `email`: required, valid email, unique normalized email.
- `phoneNumber`: optional, 7-15 digits after normalization, unique when supplied.
- `password`: min 8, uppercase, lowercase, digit required, symbol optional.
- `firstName`, `lastName`: required, max 80.
- `language`: optional; supported `tg-TJ`, `ru-RU`, `en-US`; default `tg-TJ`.
- `device.deviceId`: optional with device; max 128.
- `device.deviceName`: optional with device; max 120.
- `device.platform`: optional with device; max 40.
- `device.appVersion`: optional; max 40.

## 14. Success Response
`200 OK`
```json
{
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "email": "noseh@example.com",
    "phoneNumber": "+992900000000",
    "firstName": "Noseh",
    "lastName": "Tagaymurodzoda",
    "preferredLanguage": "tg-TJ",
    "role": "User"
  },
  "tokens": {
    "accessToken": "<access-token>",
    "refreshToken": "<refresh-token>",
    "accessTokenExpiresAtUtc": "2026-07-08T07:37:27+00:00",
    "accessTokenExpiresAtDushanbe": "2026-07-08T12:37:27+05:00"
  },
  "session": {
    "id": "00000000-0000-0000-0000-000000000000",
    "deviceName": "Samsung S24"
  }
}
```

## 15. Error Responses
- `409` duplicate email or phone.
- `422` validation failure.
- `429` rate limit.

## 16. Stable Error Codes
- `auth.email_already_exists`
- `auth.phone_already_exists`
- `validation.failed`
- `auth.email.required`
- `auth.email.invalid`
- `auth.password.required`
- `auth.password.policy`
- `auth.language.unsupported`
- `rate_limit.too_many_requests`

## 17. Frontend Behavior
On `200`, persist refresh token securely, keep access token in memory where practical, set authenticated user state, then navigate to app home. On validation or conflict, show localized title/message and highlight fields using `errors`.

## 18. Retry Policy
Do not blindly retry register. User action required after validation/conflict. For `429`, wait before enabling submit.

## 19. Caching
Do not cache.

## 20. Idempotency
Not idempotent. Same email/phone later returns conflict.

## 21. Security Notes
Never log password or returned tokens. Raw refresh token is returned only once to client.

## 22. Example Flow
Register -> receive tokens -> store refresh token -> call authenticated route with Bearer access token.

## 23. Related Endpoints
- [Login](/docs/api/identity/login)
- [Refresh Token](/docs/api/identity/refresh-token)
- [Current User](/docs/api/identity/me)

## 24. Change History
2026-07-08 - Initial V2 documentation.
