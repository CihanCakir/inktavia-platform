# Endpoint Creation Policy

The user asked to create missing endpoints when no corresponding endpoint exists.

Apply this policy carefully:

## Implement the endpoint when all are true

1. The endpoint is required by the active React Admin Web UI.
2. The endpoint belongs to an active domain:
   - Identity
   - ReferenceData
   - Vessel
   - FileStorage
   - ServiceRequest
3. The internal module has a real controller/command/query/RemoteCall contract, or the BFF can safely aggregate existing active endpoints.
4. The endpoint does not require inventing domain data models.

## Do not implement active business logic when the module is inactive/future

Future/inactive examples:

```text
Payment
Profile
Commerce
Seller
Provider
Payout
Inventory
Reporting if no real module exists
Notification if no real module exists
CargoDry if no real module exists
```

For these endpoints choose one:

1. Keep feature flag disabled and update tests to expect 404/501.
2. Create a controlled BFF placeholder returning 501 Not Implemented with a clear envelope.
3. Create local/demo-only mock endpoint only if explicitly needed for Admin Web visual testing and guarded by configuration.

Do not add fake production business logic.
