# 05 - Generate Module Local Postman Docs

For every active module create:

```text
Modules/<ModuleName>/docs/postman/<ModuleName>.ControllerApiTests.postman_collection.json
Modules/<ModuleName>/docs/postman/<ModuleName>.postman-testing-guide.md
Modules/<ModuleName>/docs/postman/<ModuleName>.postman-validation-report.md
```

Group by actual controller and action.

Every request must include:

- normalized URL
- correct module base URL variable
- Authorization Bearer `{{active_access_token}}` if needed
- `X-Aizen-User-Token: {{X-Aizen-User-Token}}` if user context is needed
- meaningful body/params from DTOs
- post-response tests
- id extraction scripts when relevant
