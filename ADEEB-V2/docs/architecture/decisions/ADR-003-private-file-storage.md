# ADR-003 Private File Storage

## Context

Payment receipt images contain security-sensitive and potentially personal financial evidence. Development needs local storage, while production needs an object-store-compatible provider.

## Problem

Public static files, client-controlled filenames, request MIME types, and unvalidated image bytes allow unauthorized disclosure, path traversal, content spoofing, decompression attacks, metadata leakage, and orphaned objects.

## Decision

Introduce `IPrivateFileStorage` in application abstractions with configuration-selected local and S3-compatible implementations. Local files live outside `wwwroot`; object keys are generated as `commerce/payment-receipts/{studentId}/{guid}.webp` and never include client filenames.

Uploads are limited to 10 MB and accepted only after signature validation, successful JPEG/PNG/WEBP decoding, width/height and 24-megapixel limits, metadata removal, and deterministic re-encoding. Database failure triggers best-effort object deletion with `CancellationToken.None`.

Reads require a Commerce permission and receipt lookup. Either the API streams the object or issues a signed URL valid for at most five minutes. Every access is audited. Idempotent retries resolve an existing receipt before upload and cannot create a second object.

## Alternatives Considered

- `wwwroot` storage: rejected because authorization cannot protect direct static URLs reliably.
- Store image bytes in PostgreSQL: rejected for current scale because it couples large-object I/O to transactional tables and backups.
- Trust extension or request MIME type: rejected because both are client-controlled.
- Virus scanning alone: insufficient without decoding, dimension limits, and re-encoding.

## Consequences

Image processing and storage become explicit dependencies with operational limits. Object and database writes cannot be one distributed transaction, so compensating cleanup and orphan reconciliation are required.

## Migration Plan

1. Add storage and validated-image abstractions.
2. Implement private local storage and configuration validation.
3. Add secure decode/re-encode validation.
4. Move existing receipt objects with a verified mapping and checksum.
5. Add protected access, audit, S3-compatible provider, and orphan cleanup.

## Rollback Plan

Retain migrated private objects and metadata. Provider selection can return to local storage if keys remain compatible. Never roll back to public static serving; use a temporary authorized API stream if signed URL delivery must be disabled.
