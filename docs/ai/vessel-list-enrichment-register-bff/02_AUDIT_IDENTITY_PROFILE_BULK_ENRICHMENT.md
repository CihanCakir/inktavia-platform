# 02 — Audit Identity/Profile Bulk Enrichment Capability

The owner column must not be solved with an N+1 query. The BFF must collect unique owner IDs from the vessel page and call Identity/Profile in bulk.

Audit available Identity/Profile endpoints and BFF remote clients:

```bash
find Bff/src/AdminPanel -name "*Identity*RemoteCall*.cs" -o -name "*Profile*RemoteCall*.cs" -o -name "*User*RemoteCall*.cs" | sort
grep -r "Get.*User\|Profile\|ByIds\|Bulk\|UserIds\|UserProfile" Bff/src/AdminPanel Modules/Identity/src --include="*.cs" -n | head -200
find Modules/Identity/src -name "*Controller*.cs" | sort
```

Required owner enrichment input fields from Vessel list items:

- `OwnerUserId` (`long?`) or `UserId` (`long?`)
- `OwnerProfileId` / `UserProfileId` (`long?`)
- `OwnerName` if already denormalized
- `OwnershipStatus`

If Vessel module list response does not expose owner IDs, extend it additively.

Preferred BFF enrichment output:

```csharp
public sealed class OwnerDisplayBffDto
{
    public long? UserId { get; set; }
    public long? ProfileId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}
```

If Identity/Profile already has a bulk endpoint, use it.

If no bulk endpoint exists, add a minimal admin-safe bulk endpoint in Identity module or BFF remote contract according to existing architecture:

```http
GET /api/v1/admin/users/profiles/bulk?userIds=10003&userIds=10004
```

or equivalent existing route style.

Rules:

- Use `long` IDs.
- Include service token + user token on BFF → Identity calls.
- Null-safe: if Identity/Profile is down, preserve list and add warning, not hard failure.
- Never call Identity once per row.

Document result in:

```text
docs/reports/vessel-owner-identity-bulk-enrichment-report.md
```
