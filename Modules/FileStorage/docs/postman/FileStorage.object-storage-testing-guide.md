# FileStorage Object Storage Testing Guide (Local)

Step-by-step guide for testing FileStorage object storage flows using Postman and local MinIO.

---

## Prerequisites

1. Docker running with `docker compose up minio minio-init file-storage-api -d`
2. Postman installed
3. A valid Keycloak access token

---

## Setup Postman

1. Import `FileStorage.ObjectStorage.postman_collection.json`
2. Import `FileStorage.ObjectStorage.Local.postman_environment.json`
3. Select the **FileStorage - Object Storage - Local** environment in Postman
4. Set `active_access_token` to your Keycloak Bearer token
5. Set `X-Aizen-User-Token` to the user context token if required by your auth middleware

---

## Test Flow

### Step 1 — Create Upload Session

Run request **01 - Create Upload Session**.

- Body: `fileName`, `contentType`, `fileSizeBytes`, `category`, `visibility`
- On success: `uploadSessionCode` and `signedUploadUrl` are auto-saved to environment variables

**Expected:** HTTP 200 or 201

---

### Step 2 — Upload File to Signed URL

Run request **02 - [Manual] Upload File to Signed URL**.

This request goes **directly to MinIO** using the `signedUploadUrl` from step 1.

1. In the Body tab, select **binary**
2. Pick a local file matching the `contentType` set in step 1
3. Send the request

**Expected:** HTTP 200 (from MinIO/S3 — no body returned)

> ⚠️ The `Content-Type` header **must** match what was declared in step 1. MinIO enforces this on signed URLs.

---

### Step 3 — Complete Upload Session

Run **03 - Complete Upload Session**.

The FileStorage API verifies the object exists in MinIO and transitions the file status to `Uploaded`.

- On success: `fileId` is saved to environment

**Expected:** HTTP 200 or 201 with `fileId` in response

---

### Step 4 — Get File By ID

Run **04 - Get File By ID**.

Verifies the file record is persisted and readable.

**Expected:** HTTP 200

---

### Step 5 — Get File Metadata

Run **05 - Get File Metadata**.

Returns rich metadata (content type, size, checksum, etc.).

**Expected:** HTTP 200

---

### Step 6 — Create Read URL

Run **06 - Create Read URL**.

Returns a pre-signed GET URL. Copy the `signedReadUrl` and open it in a browser or use `curl` to download the file.

**Expected:** HTTP 200 with `signedReadUrl`

---

### Step 7 — Soft Delete File

Run **07 - Soft Delete File**.

Marks the file as deleted. The object remains in MinIO until background cleanup runs.

**Expected:** HTTP 200 or 204

---

## MinIO Console Verification

Open http://localhost:9001 (credentials: `minioadmin` / `minioadmin`).

Navigate to **Buckets → inktavia-filestorage-local → Objects** to verify the uploaded file exists after steps 1-3, and confirm it remains after soft delete (step 7).

---

## Troubleshooting

| Issue | Resolution |
|---|---|
| 403 on signed URL PUT | Ensure `Content-Type` header matches the declared content type |
| 404 on complete session | MinIO object not found — repeat step 2 |
| Connection refused on `localhost:9000` | Check `docker compose ps` — MinIO may not be running |
| Bucket not found | Run `docker compose up minio-init` to recreate the bucket |
