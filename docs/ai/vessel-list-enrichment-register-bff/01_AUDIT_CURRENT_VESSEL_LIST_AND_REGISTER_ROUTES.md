# 01 — Audit Current Vessel List and Register Routes

Run and inspect:

```bash
find Bff/src/AdminPanel -path "*AdminVessels*" -name "*.cs" | sort
find Bff/src/AdminPanel -name "*Vessels*Controller*.cs" -o -name "*Vessel*Controller*.cs" | sort
grep -r "vessels/register\|Register" Bff/src/AdminPanel Modules/Vessel/src --include="*.cs" | head -120
grep -r "GetAdminVesselListBffQueryHandler\|AdminVesselListBffResponse\|VesselListItemBffDto" Bff/src/AdminPanel --include="*.cs" -n
```

Read fully:

- `GetAdminVesselListBffQueryHandler`
- `GetAdminVesselListBffQuery`
- `VesselListItemBffDto`
- `AdminVesselListBffResponse`
- `AdminVesselsController`
- Existing register/create vessel endpoints, if any
- `IVesselAdminBffRemoteCall`

Then audit Vessel module list query:

```bash
grep -r "GetAllVesselsAdminQuery\|GetAdminVesselList\|VesselList" Modules/Vessel/src --include="*.cs" -n
find Modules/Vessel/src -name "*VesselAdminController*.cs" -o -name "*GetAllVesselsAdmin*" | sort
```

Determine why these are empty in UI:

- `OwnerName`
- `Latitude`
- `Longitude`
- `LastPositionDate`
- `OperationalStatus`
- `Status`

Document findings in:

```text
docs/reports/vessel-list-enrichment-audit-report.md
```

Do not implement code until this report is drafted.
