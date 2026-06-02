# 10 — Build, Migration and Final Report

## Run

```bash
dotnet restore
dotnet build
```

Fix all errors.

## Migration

Create migration for FileStorageDbContext.

Validate:

```text
schema file_storage
file tables created
indexes created
Mongo documents not included in EF migration
```

## Validation checklist

```text
[ ] FileStorage.Infrastructure was not created
[ ] AWS S3 provider is implemented
[ ] Binary file is not stored in database
[ ] File metadata is stored in PostgreSQL
[ ] Message contracts are under Abstraction/Message
[ ] Message results inherit AizenMessageResult
[ ] Messages inherit AizenBaseMessage
[ ] Consumers are under Api/Consumers
[ ] Request/response consumers use AizenBaseMessageConsumer<TMessage,TResult>
[ ] Fire-and-forget consumers use AizenBaseMessageConsumer<TMessage>
[ ] Application handlers publish messages with IAizenMessagePublisher
[ ] CreateUploadSession returns pre-signed upload URL
[ ] CompleteUploadSession validates S3 object and publishes FileUploadedMessage
[ ] LinkFileToOwner publishes FileLinkedToOwnerMessage
[ ] Read URL is not persisted permanently
[ ] Delete is soft delete by default
[ ] DocumentationInfo exists on public classes/interfaces
[ ] dotnet build succeeds
```

## Final report

Produce:

```text
1. Created projects
2. Created Abstraction contracts
3. Created Message and MessageResult contracts
4. Created Consumers
5. Created Domain entities/documents
6. Created Repository context/configurations/repositories/services
7. Created AWS S3 provider
8. Created Application commands/queries
9. Created API controllers/endpoints
10. Message publisher usage
11. Cache/security strategy
12. Migration result
13. Build result
14. Remaining manual tasks
```

## Additional RemoteCall validation

Also validate:

```text
[ ] FileStorage synchronous calls use IAizenRemoteCall.
[ ] No custom Refit/external client structure was created.
[ ] RemoteCall contracts are under the existing repository convention.
[ ] IFileStorageRemoteCall inherits IAizenRemoteCall.
[ ] RemoteCall methods use AizenRemoteCall attributes.
[ ] Request bodies use AizenRemoteCallBody.
[ ] Authorization headers use AizenRemoteCallHeader("Authorization").
[ ] dotnet build succeeds.
```
