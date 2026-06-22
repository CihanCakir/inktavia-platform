# 01 — Read Reports and Confirm Current Response

Read these reports if they exist:

```text
docs/reports/vessel-list-enrichment-audit-report.md
docs/reports/vessel-owner-identity-bulk-enrichment-report.md
docs/reports/vessel-location-status-mapping-report.md
docs/reports/vessel-register-bootstrap-endpoint-report.md
docs/reports/vessel-register-create-endpoint-report.md
docs/reports/vessel-list-register-final-gap-report.md
docs/reports/vessel-demo-seed-audit-report.md
docs/reports/vessel-demo-seed-implementation-report.md
docs/reports/vessel-demo-seed-final-gap-report.md
docs/reports/vessel-demo-seed-smoke-test-report.md
```

Then inspect current source code:

```bash
find . -path "*AdminVessels*" -name "*.cs" | sort
find . -path "*Vessel*" -name "*ListItem*.cs" | sort
find . -path "*Vessel*" -name "*GetAllVesselsAdmin*" | sort
find . -path "*Identity*" -name "*GetUserProfilesByUserIds*" | sort

grep -r "OwnerName\|OwnerUserId\|OwnerAvatarUrl\|OwnerProfileId" --include="*.cs" Bff Modules | head -200
grep -r "OperationalStatusLabel\|AssetTypeLabel\|StatusLabel\|LastLocationText" --include="*.cs" Bff Modules | head -200
grep -r "CoverMediaUrl\|ThumbnailUrl\|IsCover\|IsPrimary\|VesselMedia" --include="*.cs" Bff Modules | head -240
grep -r "GetUserProfilesByUserIds\|profiles/bulk\|UserProfileListItemDto" --include="*.cs" Modules/Identity Bff | head -240
```

Confirm the actual current response shape either from a local curl test or from the latest user-provided sample. Treat current source code and runtime response as the source of truth.
