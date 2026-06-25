# Vessel UI BFF Final Gap Report

## Implemented ✅
- 4 MVP GET endpoints (list, detail, documents, media)
- All entity fields added and EF configured
- DTO fields added to abstraction layer
- Mapping updated
- New BFF DTOs, queries, handlers created
- Build passes with 0 errors

## Known Gaps / Follow-ups

### 1. EF Migration
EF `dotnet ef migrations add` requires the Vessel API as startup project. Run:
```bash
dotnet ef migrations add AddVesselUiContractFields \
  --project Modules/Vessel/src/Aizen.Modules.Vessel.Repository \
  --startup-project Modules/Vessel/src/Aizen.Modules.Vessel
```

### 2. OwnerName Not Populated
`VesselListItemBffDto.OwnerName` is `null`. Requires Identity module join (userId → display name). Future: add identity enrichment step in `GetAdminVesselListBffQueryHandler`.

### 3. OperationalStatus on Detail Response
`VesselDetailBffDto.OperationalStatus` is `null` because `VesselDto` (from `GetVesselDetailResponse`) does not yet carry these new fields. Need to add `OperationalStatus`/`AssetType` to `VesselDto` and propagate through `VesselDetailDto`.

### 4. HeroImageUrl Not Set
`VesselDetailBffDto.HeroImageUrl` is `null`. Requires FileStorage pre-signed URL call for the cover media. Future integration via `IFileStorageAdminBffRemoteCall`.

### 5. UploadedAt on Media
`VesselMediaBffDto.UploadedAt` is `null`. `VesselMediaEntity` does not store upload timestamp. Consider using `CreateDate` from `AizenEntityWithAudit`.

### 6. Token Protection for Redis
Redis stores raw service tokens. A token encryption/protection utility should be applied before storage (documented in previous reports).

### 7. Latitude/Longitude on List Handler (EF Query)
The `GetAllVesselsAdminQueryHandler` EF query projection sets `Latitude`/`Longitude` to null because joins to `LocationSnapshots` are not included. Populate via a separate join or snapshot denormalization if needed.
