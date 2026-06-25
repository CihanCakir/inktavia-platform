# RUN THIS FIRST — Vessel List Enrichment + Register BFF

You are implementing the remaining Vessel Admin Web backend/BFF gaps for Inktavia Marine OS.

Current situation:
- Vessel list rows load successfully, but some table columns are empty: owner, last location, and status.
- Vessel list BFF handler maps `OwnerName`, `Latitude`, `Longitude`, `LastPositionDate`, `OperationalStatus`, `AssetType`, and `OwnershipStatus`, but the upstream data is incomplete or not enriched.
- The React Admin Web Vessel Register page calls `GET /api/v1/admin-panel/vessels/register` and receives `404`.
- Entity IDs in this system are `long` / `long?`, not `Guid`, except specific FileStorage `FileId` fields proven in source.

Your task:
1. Audit the current Vessel list BFF and Vessel module list query.
2. Ensure the Vessel module exposes enough identifiers for BFF enrichment: current owner user/profile IDs, ownership status, latest location snapshot, operational status, asset type, status.
3. Implement BFF bulk enrichment for owner display information via Identity/Profile service using unique owner IDs from the page result.
4. Fix last location and status mapping for the main Vessel table.
5. Implement `GET /api/v1/admin-panel/vessels/register` for register-page bootstrap/options.
6. Implement or validate create/register endpoint used by the register form.
7. Preserve auth, response envelope, long ID conventions, and existing route conventions.
8. Build, smoke test, and write final reports.

Follow all reference documents and numbered prompts in this package. Do not write code until you complete the audits in steps 01 and 02.
