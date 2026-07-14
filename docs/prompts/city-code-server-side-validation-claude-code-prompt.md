# Claude Code Prompt — Validate city codes server-side (close the last hole)

**Follow-up to `docs/city-close-the-loop-report.md`.** The vocabularies now agree and the two-replica backplane
test passes end to end (`city:35` on both sides, 0/6 consumed by the socket-less instance, all six delivered).
One hole is left, and it is the one that lets the whole thing rot again.

## The hole

`ProviderOnboardingDomainService` checks only that `OperatingRegion.cityCode` is **present**:

```csharp
var cityCode = orStep.TryGetProperty("cityCode", out var cc) ? cc.GetString() : null;
if (string.IsNullOrWhiteSpace(cityCode))
    missing.Add("OperatingRegion: city code is required.");
```

The comment above it says *"Reject unknown codes"* — the code does not. Anything non-empty is accepted, trimmed,
uppercased and written to `UserProfile.City`.

The SPA now sends real ReferenceData codes, so nothing looks wrong today. But **the server is trusting the client
to be correct.** Onboarding is an HTTP endpoint: anyone posting `cityCode: "BODRUM"`, `"Çeşme Marina"` or `"asdf"`
gets a provider whose `City` matches no city group. They will be approved, they will log in, and they will never
be told about a single job — silently, exactly as before. Fixing the SPA fixed one caller, not the contract.

## What to do

### 1. Validate against ReferenceData, fail closed

- On **save-step** and again on **submit**, resolve `OperatingRegion.cityCode` (and `country`) against
  ReferenceData. Unknown code → **reject** with a clear validation error. Do not store it, do not normalise it
  into something plausible, do not fall back to null.
- Validate on submit as well as save: a draft can be written by one call and submitted by another, and the second
  is the one that mirrors into `UserProfile`.
- Use the module's existing ReferenceData access pattern and the existing validation/Result conventions. Cache the
  lookup — the city list changes rarely.
- Normalisation stays in the single place it already lives. Do not add a second one.

### 2. The same hole exists on the other side

A service request published with a `LocationCityCode` that is not a ReferenceData code goes to a group **no one
has joined**. The event is accepted, logged as "Pushed", and reaches nobody. Validate `LocationCityCode` the same
way where a request is created/published, and reject unknown codes there too.

Check whether anything else writes a city code (admin screens, seeds, imports, mock data) and bring it under the
same rule. A single unvalidated writer restores the bug.

### 3. Prove the rejection, not just the happy path

The happy path already passes and proves nothing about this change. Show the **refusal**:

- POST an onboarding step with `cityCode: "BODRUM"` → rejected, with the error message.
- POST with `cityCode: "35"` → accepted.
- Create/publish a service request with an invalid `LocationCityCode` → rejected.
- Confirm no `UserProfile.City` or `LocationCityCode` row was written in the rejected cases.

Paste the actual request/response for each. A test that only exercises valid input cannot fail, and therefore
cannot tell us anything.

### 4. Re-confirm nothing regressed

Re-run the city-35 realtime check: provider connects (`Realtime connected: provider 100011, city 35`), six
requests published in `35`, all six arrive. Report which instance consumed each.

## Constraints

- **Fail closed.** An unknown code is an error, never a null, never a guess, never "best effort".
- No fuzzy matching: no `contains`, no `startsWith`, no case-guessing, no district→province inference. Bodrum is
  not a city; `48` is. Port/district refinement belongs to GeoDiscovery.
- Do not weaken or bypass validation to make an existing seed/mock pass — fix the seed.
- Provider identity from the BFF assertion; never from body or query.
- If a step cannot be completed, report the feature as **not done** — not as "done with open items".
