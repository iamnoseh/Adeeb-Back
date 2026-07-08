---
id: Flow.RefreshToken
title: Refresh Token Flow
module: Flows
status: Stable
auth: Anonymous
frontendReady: true
order: 20
---
# Refresh Token Flow

## Overview
Refresh tokens rotate. A refresh token used successfully once becomes invalid.

## Single-Flight Rule
```text
/profile  --\
/missions ----> ONE refresh request -> new tokens -> retry all once
/league   --/
```

Use a single-flight lock in Flutter/web clients. Do not fire three refresh calls for three concurrent `401` responses.

## Success Behavior
- Save new refresh token atomically.
- Replace access token.
- Retry failed requests exactly once.

## Failure Behavior
- Clear access token and refresh token.
- Navigate to login.
- Do not retry refresh in a loop.

## Security Note
If an old rotated token is reused, the server revokes active sessions in that token family.
