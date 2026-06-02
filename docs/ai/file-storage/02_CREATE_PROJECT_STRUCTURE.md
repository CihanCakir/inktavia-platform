# 02 — Create Project Structure

Create the FileStorage module projects and folders.

## Projects

```text
Aizen.Modules.FileStorage.Api
Aizen.Modules.FileStorage.Application
Aizen.Modules.FileStorage.Abstraction
Aizen.Modules.FileStorage.Domain
Aizen.Modules.FileStorage.Repository
```

Do not create `Aizen.Modules.FileStorage.Infrastructure`.

## Folder structure

```text
Aizen.Modules.FileStorage.Abstraction/
  Enum/
  Dto/File/
  Dto/UploadSession/
  Dto/Access/
  Dto/Processing/
  Request/File/
  Request/UploadSession/
  Request/Access/
  Request/Processing/
  Client/
  Message/

Aizen.Modules.FileStorage.Domain/
  Entities/File/
  Entities/UploadSession/
  Entities/Access/
  Entities/Processing/
  Documents/
  Interface/Repository/
  Interface/Service/

Aizen.Modules.FileStorage.Repository/
  Persistence/Configurations/
  Repositories/
  Providers/S3/
  Service/File/
  Service/UploadSession/
  Service/Access/
  Service/Validation/
  Service/Processing/
  Service/Cache/
  Mongo/
  Seed/

Aizen.Modules.FileStorage.Application/
  UploadSession/Commands/
  UploadSession/Queries/
  UploadSession/Validators/
  UploadSession/Mappings/
  File/Commands/
  File/Queries/
  File/Validators/
  File/Mappings/
  Access/Commands/
  Access/Queries/
  Access/Validators/
  Access/Mappings/
  Processing/Commands/
  Processing/Queries/
  Processing/Validators/
  Processing/Mappings/

Aizen.Modules.FileStorage.Api/
  Controllers/V1/
  Consumers/Upload/
  Consumers/Processing/
  Consumers/Owner/
  Consumers/Cleanup/
```

## Project references

Follow ReferenceData/Vessel project reference style.

Expected:

```text
Api -> Application, Abstraction, Repository
Application -> Domain, Abstraction
Domain -> Abstraction
Repository -> Domain, Abstraction, Aizen Core EF/Mongo/cache/message packages, AWS SDK packages
```

Every public class/interface must include DocumentationInfo.
