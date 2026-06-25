# Output and Reporting Requirements

Generate:

```text
docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json
docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json
docs/postman/admin-panel-bff/README.md
docs/reports/admin-panel-bff-postman-generation-report.md
docs/reports/admin-panel-bff-postman-endpoint-coverage-report.md
docs/reports/admin-panel-bff-postman-auth-model-report.md
```

Reports must include:

```text
Catalog file read
Controllers scanned, if any
Folders generated
Requests generated
Endpoints excluded
Auth model applied
Variables generated
Scripts added
Validation commands
Validation results
Remaining gaps
```

Validate generated JSON with `python -m json.tool`.
