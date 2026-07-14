# Private Payment Receipt Storage

Payment receipt images are stored outside `wwwroot`. The default local root is `data/private`; production can select the S3-compatible provider through `PrivateFileStorage:Provider=S3`.

S3 configuration requires `Bucket`, `AccessKey`, and `SecretKey`. `ServiceUrl`, `Region`, and `ForcePathStyle` support AWS S3 and MinIO-compatible deployments. Credentials must be supplied through environment variables or a secret manager and must never be committed.

Uploads are decoded as JPEG, PNG, or WebP, limited to 10 MB, 6000 pixels per axis, and 24 million total pixels, stripped of metadata, and re-encoded to WebP. Generated keys have the form `commerce/payment-receipts/{studentId}/{guid}.webp`.

The `SecurePrivatePaymentReceiptStorage` migration renames the database URL field and rewrites legacy `/uploads/commerce/receipts/*` values to `commerce/payment-receipts/legacy/*`. Before deployment, copy the corresponding legacy files from `wwwroot/uploads/commerce/receipts` into that private prefix. Verify checksums and remove the public copies before enabling the new application version.

Rollback cannot safely restore public access. If application rollback is required, retain private objects and serve them through an authorized compatibility endpoint. Do not move receipt evidence back into `wwwroot`.

An hourly-delayed daily cleanup removes unattached objects older than 24 hours. Database-save failures also trigger immediate best-effort deletion with a non-request cancellation token.
