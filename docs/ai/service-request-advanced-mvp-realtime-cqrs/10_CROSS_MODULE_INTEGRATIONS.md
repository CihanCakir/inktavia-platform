# Inktavia Marine OS - ServiceRequest Advanced MVP Realtime CQRS

Use this prompt with GitHub Copilot Agent inside the repository.

All generated implementation must be in English and must follow the existing Aizen/Inktavia architecture.

## Step 10 - Cross-module integrations

Wire or prepare required ServiceRequest integrations without implementing unrelated modules inside ServiceRequest.

## Identity integration

Use existing user/profile/current context mechanism.

Required checks:

- authenticated user
- current profile if profile model is used
- admin/operator role
- provider profile/team access
- owner identity for vessel access

## Vessel integration

Use existing Vessel module service/contracts if available.

Required checks:

- vessel exists
- current user can create request for vessel
- current user can list vessel requests
- provider/admin can view vessel-limited data only when authorized

Suggested service contract if missing:

```text
IVesselAccessService
```

Do not implement full Vessel logic in ServiceRequest.

## FileStorage integration

Use FileStorage module to validate and reference uploaded files.

Rules:

- Store only `FileId` in ServiceRequest attachments.
- Do not store binary data.
- Validate ownership/access/content purpose if FileStorage exposes those capabilities.
- Use signed URL DTO only if current FileStorage module exposes it.

## ReferenceData integration

Validate:

```text
ServiceCategoryId
ServiceTypeId
CurrencyCode
UnitCode
Location references if used
```

Do not duplicate lookup data.

## ProviderOperations integration

Use ProviderOperations module if present.

Required checks:

- provider profile exists
- provider is approved/active if business requires it
- provider supports selected service category/type
- provider can serve request location if service area exists
- team member belongs to provider profile

If ProviderOperations is not implemented yet, create lightweight interface contracts and TODO documentation without hardcoding fake provider logic.

## Payment integration point

Do not implement Payment here.

Prepare logical integration event after offer acceptance:

```text
ServiceRequestOfferAcceptedIntegrationEvent
```

Payload should include:

```text
ServiceRequestId
AcceptedOfferId
OwnerUserId
ProviderProfileId
VesselId
CurrencyCode
TotalAmount
OfferItems
```

## Notification integration point

Do not implement Notification here.

Add domain/integration events that Notification can consume later for offline push/email/SMS.

## Review integration point

Do not implement Review here.

Prepare future event after completion approval:

```text
ServiceRequestCompletedIntegrationEvent
```

## CargoDry integration point

Do not implement CargoDry here.

Allow service request item/offer item types to represent CargoDry installation, replacement, renewal, product usage, or inspection through ReferenceData categories and item types.

## GeoDiscovery integration point

Do not implement GeoDiscovery here.

Keep location snapshot and provider eligibility hooks clean for later nearby provider discovery.

## Output required

Create/update:

```text
ai/service-request-advanced-mvp-realtime-cqrs/reports/10_CROSS_MODULE_INTEGRATION_REPORT.md
```
