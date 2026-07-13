# Commerce Query Performance

## Query Inventory

| Use case | Predicate | Order | Supporting index |
|---|---|---|---|
| Student receipt history | `student_id`, optional `status`, keyset cursor | `created_at_utc DESC, id DESC` | `ix_commerce_receipts_student_created_id`, `ix_commerce_receipts_student_status_created_id` |
| Admin receipt queue | optional `status`, keyset cursor | `created_at_utc DESC, id DESC` | `ix_commerce_receipts_status_created_id` |
| Pending review queue | `status = 1`, keyset cursor | `created_at_utc DESC, id DESC` | `ix_commerce_receipts_pending_created_id` (partial) |
| Reviewer history | `reviewed_by_user_id`, reviewed date range | receipt keyset order | `ix_commerce_receipts_reviewer_reviewed` assists filtering |
| Idempotent submission | `student_id`, `idempotency_key` | single row | `ux_commerce_payment_receipts_student_idempotency_key` (unique) |
| Public tariff list | `status = Active` | `price, name` | `ix_commerce_tariffs_status`; two-minute in-memory cache |

Receipt list queries project only list contract fields and fetch `limit + 1`. They do not materialize image object keys, admin notes, or entity graphs. Cursor pagination avoids offset cost and remains deterministic when timestamps collide.

## Plan Verification

Run against a production-like PostgreSQL dataset after applying migrations:

```sql
EXPLAIN (ANALYZE, BUFFERS, WAL, FORMAT TEXT)
SELECT id, student_id, tariff_id, status, created_at_utc
FROM commerce.payment_receipts
WHERE student_id = :'student_id'
  AND (created_at_utc, id) < (:'cursor_time', :'cursor_id')
ORDER BY created_at_utc DESC, id DESC
LIMIT 31;

EXPLAIN (ANALYZE, BUFFERS, WAL, FORMAT TEXT)
SELECT id, student_id, tariff_id, status, created_at_utc
FROM commerce.payment_receipts
WHERE status = 1
ORDER BY created_at_utc DESC, id DESC
LIMIT 31;
```

Review `actual rows`, buffer reads, sort nodes, and selected index. PostgreSQL may reasonably choose a sequential scan for tiny tables. No local `EXPLAIN ANALYZE` result is claimed because Docker/PostgreSQL was unavailable during this pass.

## Load Check

Use `tests/load/commerce.js` with valid student and finance-admin access tokens:

```powershell
$env:BASE_URL = "https://localhost:5001"
$env:STUDENT_TOKEN = "..."
$env:ADMIN_TOKEN = "..."
k6 run tests/load/commerce.js
```

The script enforces a request failure rate below 1%, p95 below 500 ms for public/student reads, and p95 below 750 ms for the admin queue. Treat these as initial smoke thresholds; production SLOs require measured traffic and capacity data.
