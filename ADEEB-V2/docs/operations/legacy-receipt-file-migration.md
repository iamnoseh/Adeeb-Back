# Legacy Receipt File Migration

Historical receipt rows may reference files under `wwwroot/uploads/commerce/receipts`, including rows rewritten to `commerce/payment-receipts/legacy/{file}` by the earlier schema migration. File I/O is performed by an idempotent application migrator, never by an EF migration.

The API blocks `/uploads/commerce/receipts` with `404` before static-file middleware, including while remediation is incomplete.

## Dry Run

Configure the API with:

```text
LegacyReceiptMigration__Enabled=true
LegacyReceiptMigration__DryRun=true
```

Start one API instance. Review the structured completion log and every `MissingSource` or `Failed` item. Dry-run candidates are reported as `Skipped`; no private object, database row, or public file is changed.

## Migration Run

After resolving dry-run failures, set `LegacyReceiptMigration__DryRun=false` and restart one instance. For each source group the migrator:

1. normalizes and bounds the path under the public receipt root;
2. identifies, decodes, validates, strips metadata, and re-encodes the image as WebP;
3. writes a content-hash-addressed private object and reads it back to verify SHA-256;
4. updates each receipt only after verification;
5. deletes the public source only after every duplicate reference is updated successfully.

The statuses are `Migrated`, `AlreadyMigrated`, `MissingSource`, `Failed`, and `Skipped`. A failed database update leaves the source in place and the verified destination reusable on restart. Missing, corrupt, traversal, and storage failures never update the row or delete the source.

## Completion Verification

- Completion log has `MissingSource=0`, `Failed=0`, and `Skipped=0` for a live run.
- No receipt key starts with `/uploads/commerce/receipts/` or `commerce/payment-receipts/legacy/`.
- `wwwroot/uploads/commerce/receipts` contains no migrated files and is not served through any alternate static-file mapping.
- Protected admin receipt-image access succeeds for migrated rows.

Disable the operation after a clean run:

```text
LegacyReceiptMigration__Enabled=false
```

Do not report migration completion while any legacy receipt remains publicly reachable.
