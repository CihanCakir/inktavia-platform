# 01 — Read Vessel Reports and Discover Current ServiceRequest Structure

## Read reports first

Read these source reports:

- `reference/SOURCE_vessel-cross-module-integration-report.md`
- `reference/SOURCE_vessel-ui-bff-final-gap-report.md`
- `reference/SOURCE_admin-panel-bff-vessel-endpoint-implementation-report.md`
- `reference/SOURCE_vessel-ui-contract-audit-report.md`
- `reference/SERVICE_REQUEST_INTEGRATION_REQUIREMENT_BRIEF.md`

Extract the exact current ServiceRequest gaps.

## Discover ServiceRequest module

Run and inspect:

```bash
find . -name "*.csproj" | grep -i "servicerequest\|service-request\|service" | sort
find Modules -path "*ServiceRequest*" -name "*.cs" | sort | head -200
find Modules -path "*ServiceRequest*" -name "*Controller*.cs" -o -name "*Query*.cs" -o -name "*Command*.cs" | sort
grep -r "VesselId\|Provider\|Location\|Notes\|WorkLog\|Offer\|Assignment\|Completion\|Dispute" Modules/ServiceRequest --include="*.cs" -n | head -200
```

Read:
- ServiceRequest entities
- ServiceRequest admin controllers
- ServiceRequest query/command handlers
- abstraction request/response DTOs
- repository/DbContext configuration

## Discover AdminPanel BFF existing ServiceRequest code

Run and inspect:

```bash
find Bff/src/AdminPanel -iname "*ServiceRequest*" -type f | sort
grep -r "IServiceRequestAdminBffRemoteCall\|service-requests" Bff/src/AdminPanel --include="*.cs" -n
```

Read:
- current RemoteCall interface
- current AdminPanel BFF controller/actions
- current BFF queries/handlers/DTOs

## Output

Create `docs/reports/servicerequest-bff-contract-audit-report.md` with:
- existing module endpoints
- existing BFF endpoints
- current DTO fields
- missing fields for Vessel Detail serviceHistory
- list/detail/timeline/worklog/offers endpoints status
- whether migration is required
