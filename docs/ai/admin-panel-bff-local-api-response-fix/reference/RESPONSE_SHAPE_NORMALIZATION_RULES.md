# Response Shape Normalization Rules

Local reports include expected response shapes. Do not blindly match shapes with fake data.

## Rules

- Preserve the existing Aizen response envelope if the BFF convention uses it.
- If frontend expects a normalized shape, map the BFF response consistently.
- Return proper HTTP status codes for transport-level failures.
- Return proper domain error envelopes for domain-level failures.
- Do not return `header.isSuccess = true` for invalid credentials, invalid refresh token, or failed remote module operations.
- Unknown IDs should return 404 or the repository-standard not-found envelope.
- Missing validation fields should return 400/422 consistently.
