# 00 — Master Prompt: FileStorage Module

You are GitHub Copilot Agent. Create the complete `Aizen.Modules.FileStorage` module for Inktavia Marine OS.

## Goal

Build a centralized AWS S3 based FileStorage module that can be used by Vessel, ServiceProvider, CargoDry, Maintenance, Payment/Invoice and Profile modules.

## Required architecture

```text
Aizen.Modules.FileStorage.Api
Aizen.Modules.FileStorage.Application
Aizen.Modules.FileStorage.Abstraction
Aizen.Modules.FileStorage.Domain
Aizen.Modules.FileStorage.Repository
```

Do not create `FileStorage.Infrastructure`.

## Required capabilities

```text
1. Create upload session
2. Generate AWS S3 pre-signed upload URL
3. Complete upload session
4. Validate S3 object existence/checksum/content type/size
5. Store file metadata in PostgreSQL
6. Generate pre-signed read URL
7. Validate file ownership for external modules
8. Link file to owner module/entity
9. Manage private/public visibility
10. Soft delete file metadata and optionally delete S3 object
11. Track processing jobs
12. Publish async processing messages via IAizenMessagePublisher
13. Consume RabbitMQ/Aizen MessageBus request/response messages
14. Maintain message contracts under Abstraction/Message
15. Maintain consumers under Api/Consumers
16. Use DocumentationInfo everywhere
```

## Execution order

```text
01_ANALYZE_EXISTING_ARCHITECTURE.md
02_CREATE_PROJECT_STRUCTURE.md
03_ABSTRACTION_CONTRACTS_AND_MESSAGES.md
04_DOMAIN_ENTITIES_DOCUMENTS.md
05_REPOSITORY_CONTEXT_S3_SERVICES.md
06_APPLICATION_COMMANDS_QUERIES.md
07_MESSAGE_CONSUMERS_AND_PUBLISHERS.md
08_API_CONTROLLERS_STARTER_DI.md
09_CACHE_SECURITY_VALIDATION.md
10_BUILD_MIGRATION_FINAL_REPORT.md
```

## Additional step: RemoteCall Integration

After the FileStorage API, consumers and validation prompts are executed, run:

```text
11_REMOTE_CALL_INTEGRATION.md
```

This step ensures FileStorage synchronous calls use `IAizenRemoteCall` instead of a custom Refit/external client structure.
