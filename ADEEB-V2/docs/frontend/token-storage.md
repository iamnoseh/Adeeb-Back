---
id: Frontend.TokenStorage
title: Token Storage
module: Frontend
status: Stable
auth: Not applicable
frontendReady: true
order: 20
---
# Token Storage

## Access Token
Prefer memory storage. It is short-lived and sent as `Authorization: Bearer <access-token>`.

## Refresh Token
Store securely:
- Flutter mobile: platform secure storage.
- Web: use the safest available app strategy; avoid logging or exposing token values.

## Rotation
Every successful refresh returns a new refresh token. Replace the old token immediately.

## Failure
If refresh fails, clear local tokens and navigate to login.
