# AWS S3 Setup — Dev / Test / Prod

This document describes how to set up AWS S3 buckets for FileStorage in cloud environments.

---

## Recommended Bucket Names

| Environment | Bucket Name |
|---|---|
| Development | `inktavia-filestorage-dev` |
| Test | `inktavia-filestorage-test` |
| Production | `inktavia-filestorage-prod` |

---

## Recommended AWS Region

`eu-central-1` (Frankfurt) — aligns with the primary deployment region.

---

## IAM Policy — Minimum Required Permissions

Create a dedicated IAM user per environment with the following policy (replace `BUCKET_NAME`):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "FileStorageObjectAccess",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:HeadObject"
      ],
      "Resource": "arn:aws:s3:::BUCKET_NAME/*"
    },
    {
      "Sid": "FileStorageBucketAccess",
      "Effect": "Allow",
      "Action": [
        "s3:ListBucket"
      ],
      "Resource": "arn:aws:s3:::BUCKET_NAME"
    }
  ]
}
```

---

## Bucket Public Access Block

All FileStorage buckets must have **Block all public access** enabled. Files are served exclusively via pre-signed URLs — no public read should ever be configured.

---

## Signed URL Model

- **Upload:** Pre-signed `PUT` URL with `Content-Type` header enforced, valid for `UploadUrlExpirationMinutes` (default 15 min).
- **Read:** Pre-signed `GET` URL valid for `ReadUrlExpirationMinutes` (default 60 min).
- Clients upload directly to S3 using the signed URL — the FileStorage API never proxies file bytes.

---

## CORS Configuration for Direct Browser Upload

If browsers upload directly via the signed PUT URL, configure the bucket CORS:

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["PUT"],
    "AllowedOrigins": ["https://your-frontend-domain.com"],
    "ExposeHeaders": ["ETag"]
  }
]
```

---

## Lifecycle Recommendations

- **Abort incomplete multipart uploads after 7 days** — prevents orphan part charges.
- **Transition old file versions** (if versioning enabled) to `S3 Glacier Instant Retrieval` after 90 days.
- **Expire soft-deleted objects** after 30 days (can be driven by FileStorage cleanup jobs + S3 lifecycle rules).

---

## Environment Variable Mapping

Inject via Kubernetes secrets — never hardcode:

| Variable | Source |
|---|---|
| `S3ObjectStorage__AccessKey` | Kubernetes Secret `filestorage-s3-secret` → `access-key` |
| `S3ObjectStorage__SecretKey` | Kubernetes Secret `filestorage-s3-secret` → `secret-key` |
| `S3ObjectStorage__Provider` | Helm values (value: `"AWS"`) |
| `S3ObjectStorage__Region` | Helm values (value: `"eu-central-1"`) |
| `S3ObjectStorage__BucketName` | Helm values (per environment) |
| `S3ObjectStorage__ForcePathStyle` | Helm values (value: `"false"`) |
| `S3ObjectStorage__UseHttp` | Helm values (value: `"false"`) |

---

## Secret Handling Rules

1. **Never commit** AWS `AccessKey` or `SecretKey` to source control.
2. Create a Kubernetes secret per cluster:

```bash
kubectl create secret generic filestorage-s3-secret \
  --from-literal=access-key=AKIAIOSFODNN7EXAMPLE \
  --from-literal=secret-key=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY \
  -n filestorage
```

3. Reference from Helm values using `secretKeyRef` (already configured in `values-dev.yaml`, `values-test.yaml`, `values-prod.yaml`).
4. Rotate credentials regularly and update the secret without redeploying the chart.

---

## Helm Secret Example

```yaml
# values-prod.yaml (excerpt)
- name: S3ObjectStorage__AccessKey
  valueFrom:
    secretKeyRef:
      name: filestorage-s3-secret
      key: access-key
- name: S3ObjectStorage__SecretKey
  valueFrom:
    secretKeyRef:
      name: filestorage-s3-secret
      key: secret-key
```
