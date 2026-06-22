# Vessel Owner Name Final Gap Report

## Summary
After applying all fixes, the following gaps remain that cannot be resolved by code changes alone.

## Gap 1 — Keycloak Role Assignment (Required)

**Status:** ⚠️ Requires Keycloak admin action

**Description:** The `identity.admin` client role must exist on the `identity-api` Keycloak client AND be assigned to the `admin-panel-bff` service account.

**Required Keycloak steps:**
1. In Keycloak admin console → `inktavia-realm` → Clients → `identity-api`
2. Create client role `identity.admin` (if it doesn't exist)
3. Go to Clients → `admin-panel-bff` → Service accounts roles
4. Assign `identity.admin` from client `identity-api` to the `admin-panel-bff` service account

**Impact if not done:** The `Authorization: Bearer <admin-panel-bff-keycloak-service-token>` sent to Identity will not contain `identity.admin`, and the bulk profiles endpoint will return 403. The prior code checked only for `Admin` realm role; the fix now checks for `Admin OR identity.admin`. If Keycloak already assigns `Admin` realm role to the service account, this works without the above steps.

## Gap 2 — Profile Photo URL (Acceptable)

**Status:** ℹ️ Expected — no action required

**Description:** `ownerAvatarUrl` will remain `null` for all seeded vessel owners because `identity-profiles.json` seed data does not include `profilePhotoUrl` values. This is acceptable per spec.

**Resolution:** Upload profile photos through the Identity profile management flow, or add `profilePhotoUrl` values to the seed data if test images are available.

## Gap 3 — FileStorage Integration (Future)

**Status:** ℹ️ Future — not in scope

**Description:** When Identity stores `profilePhotoUrl` as a FileStorage `FileId` or pre-signed URL token rather than a direct CDN URL, the BFF will need to call FileStorage's `CreateReadUrl` to generate a live URL before returning it to the React client. This is out of scope for the current enrichment fix.

## Gap 4 — Vessel 5-Minute Cache

**Status:** ℹ️ Known behavior

**Description:** `GetAllVesselsAdminQueryHandler` implements `IAizenQueryHandlerCacheable` with a 5-minute Redis TTL. After applying seed data or DB changes, the owner names will not appear in the vessel list until the cache expires or Redis is flushed.

**Resolution:**
```bash
redis-cli FLUSHDB
```

## Build Validation
```
Build succeeded.
857 Warning(s)
0 Error(s)
```

All pre-existing. No new warnings introduced.

## Risks
- If both `Admin` realm role AND `identity.admin` client role are absent from the Keycloak service account, enrichment falls back silently (vessel list still works, ownerName = null, warnings array contains `"Identity"`).
- No breaking changes introduced. All other enrichment fields (`lastLocationText`, `assetType`, `operationalStatus`, `ownershipStatusLabel`, `statusLabel`) remain unaffected.
