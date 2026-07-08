---
id: Flow.Authentication
title: Authentication Flow
module: Flows
status: Stable
auth: Mixed
frontendReady: true
order: 10
---
# Authentication Flow

## Overview
ADEEB V2 authentication uses short-lived access tokens and rotating refresh tokens.

## Flow
```text
Register/Login
  -> receive access token + refresh token
  -> store refresh token securely
  -> call authenticated APIs with Bearer access token
  -> access token expires
  -> refresh once
  -> retry original request once
```

## Frontend Rules
- Use `Authorization: Bearer <access-token>` for authenticated routes.
- Keep refresh token in secure storage.
- Do not log tokens.
- On `401` from authenticated route, run refresh flow once.
- On refresh failure, clear auth state.

## Related Routes
- [Register](/docs/api/identity/register)
- [Login](/docs/api/identity/login)
- [Refresh Token](/docs/api/identity/refresh-token)
- [Current User](/docs/api/identity/me)
