# 06 - Complete Vessel, FileStorage and ServiceRequest Mapping Gaps

Audit and complete missing AdminPanel BFF mappings for Vessel, FileStorage and ServiceRequest.

## Vessel areas

- vessel list/filter/detail
- vessel ownerships
- vessel status
- vessel archive/restore if present
- vessel specification
- vessel engines
- vessel documents
- vessel media
- vessel location snapshot
- vessel status history

## FileStorage areas

- file metadata
- read signed URL
- upload session if needed by admin
- file access/ownership review if present
- soft delete/restore if present

## ServiceRequest areas

- admin service request list/filter/detail
- service request operation detail
- status history
- offers
- assignments
- messages
- work logs
- completion
- disputes
- attachments
- admin status/dispute/completion actions

Use AizenRemoteCall and forward tokens.

Generate:

```text
Bff/src/AdminPanel/docs/postman/vessel-filestorage-servicerequest-gap-coverage.md
```
