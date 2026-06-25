# 06 - AWS S3 Dev Test Prod Guide

Generate documentation for AWS S3 setup for Dev/Test/Prod environments.

Required documentation:

```text
docs/filestorage/aws-s3-dev-test-prod-setup.md
```

Include:

- Recommended bucket names:
  - `inktavia-filestorage-dev`
  - `inktavia-filestorage-test`
  - `inktavia-filestorage-prod`
- Recommended AWS region strategy.
- IAM user/role policy minimum permissions.
- Environment variable mapping.
- Secret handling rules.
- Bucket public access block recommendation.
- Signed URL usage model.
- CORS note for browser/mobile direct upload if required.
- Lifecycle and retention recommendations.

Do not include real AWS credentials.

Provide an example IAM policy limited to the selected bucket pattern. Use placeholders.
