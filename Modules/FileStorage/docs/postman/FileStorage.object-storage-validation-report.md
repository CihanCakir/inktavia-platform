# FileStorage Object Storage — Validation Report

_Use this template to record results after running the object storage Postman collection._

---

## Environment

| Item | Value |
|---|---|
| Date | |
| Tester | |
| Environment | Local / Dev / Test / Prod |
| Provider | MinIO / AWS S3 |
| FileStorage API URL | |
| MinIO/S3 Endpoint | |
| Bucket | |

---

## Checklist

### Infrastructure

- [ ] MinIO container running (`docker compose ps minio`)
- [ ] `minio-init` completed successfully (bucket created)
- [ ] FileStorage API running (`docker compose ps file-storage-api`)
- [ ] `S3ObjectStorage__Provider` env var = `MinIO` in container
- [ ] `S3ObjectStorage__ServiceUrl` env var = `http://minio:9000`

### Postman Flow Results

| Step | Request | Expected Status | Actual Status | Pass/Fail | Notes |
|---|---|---|---|---|---|
| 01 | Create Upload Session | 200/201 | | | |
| 02 | Upload File to Signed URL | 200 | | | |
| 03 | Complete Upload Session | 200/201 | | | |
| 04 | Get File By ID | 200 | | | |
| 05 | Get File Metadata | 200 | | | |
| 06 | Create Read URL | 200 | | | |
| 07 | Soft Delete File | 200/204 | | | |

### MinIO Console Verification

- [ ] Object visible in bucket after step 03
- [ ] Object still present after soft delete (step 07)
- [ ] Object metadata (size, content-type) matches uploaded file

---

## Issues Found

| # | Severity | Description | Status |
|---|---|---|---|
| | | | |

---

## Sign-off

| Role | Name | Date | Approved |
|---|---|---|---|
| Tester | | | |
| Reviewer | | | |
