---
name: City group realtime bug
description: Onboarding never populates UserProfile.City so no provider ever joins a city group — ServiceRequestPublished events never arrive
type: project
---

Provider profile 100011 has City = null because onboarding never populates UserProfile.City. The hub joins `city:{cityCode}` from `resolution.Profile?.City`, so when City is null, the provider never joins a city group and `ServiceRequestPublished` events (pushed to `city:{code}`) can never arrive.

**Why:** The `MirrorDraftToProfile` method in `ProviderOnboardingDomainService.SubmitAsync` calls `profile.SetLocation(city, country)` from the BusinessIdentity draft, but this sets `LocationCity`/`LocationCountry` — not `City`. The `OrganizerProfileDetailDto.City` field is mapped from a different property. Need to trace the exact mapping to confirm which property the DTO reads.

**How to apply:** This is a production bug — no provider in production currently receives `ServiceRequestPublished` realtime events. Fix the onboarding→profile City population, or fix the DTO mapping, so the hub can join city groups. Do NOT work around it in the hub/consumer.
