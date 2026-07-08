---
id: Frontend.RetryPolicy
title: Retry Policy
module: Frontend
status: Stable
auth: Not applicable
frontendReady: true
order: 30
---
# Retry Policy

## Login
No automatic retry.

## Register
No blind automatic retry.

## Refresh
Use single-flight refresh. No infinite loop.

## Authenticated Request
If access token expired, refresh once and retry original request once.

## 429
Respect server response. Do not aggressively retry.

## 5xx
Retry only safe/idempotent calls according to product policy.
