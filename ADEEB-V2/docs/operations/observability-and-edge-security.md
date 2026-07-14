# Observability and Edge Security

## Request Edge

Trusted forwarded headers run first. Correlation and security headers run before authentication, followed by authentication, claim-based localization, authorization, and rate limiting. `X-Correlation-ID` is accepted only when it is 1-128 characters of ASCII letters, digits, `-`, `_`, `.`, or `:`; otherwise the server trace id is used.

Responses include `X-Correlation-ID`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`, and `Permissions-Policy`. CORS origins are explicit under `Cors:AllowedOrigins`; wildcard origins and credential reflection are not used. Production startup rejects `AllowedHosts=*`.

## OpenTelemetry

The API emits ASP.NET Core, outbound HTTP, .NET runtime, and Commerce telemetry with:

- `service.name= Adeeb.Api`
- assembly service version
- machine instance id
- `deployment.environment.name`

Commerce receipt submit/approve/reject operations emit activities, operation counters, outcome tags, and latency histograms. Configure export only when a collector exists:

```text
OpenTelemetry__OtlpEndpoint=http://otel-collector:4317
```

No endpoint means telemetry remains instrumented but no OTLP exporter is created.

## Rate Limits

The host applies a configurable global concurrency limiter. Receipt uploads and admin review actions also use authenticated user plus trusted forwarded client IP partitions. Configure positive values under `RateLimits`; invalid zero or negative values fail startup.

## Health

`/health/live` is process-only. `/health/ready` checks all module databases and private file storage. The storage probe writes and deletes a one-byte private object; a failed probe makes readiness return `503`.
