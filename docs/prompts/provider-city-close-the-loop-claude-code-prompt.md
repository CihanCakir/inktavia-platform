# Claude Code Prompt — City codes: close the loop (the previous pass left the system broken)

**Follow-up to `docs/city-canonical-code-report.md`.** That pass changed the backend to a new city vocabulary and
listed the parts that make it actually work as "open items". The result is not a half-finished improvement — it is
a **regression**. Read this before writing any code.

## What is broken right now

The backend's canonical city code is now the **province plate code**: `34` Istanbul, `35` Izmir, `48` Muğla.
Service requests were migrated to it (`MUGLA → 48`). `ProviderRealtimeHub.CityGroup()` now uppercases.

But the producer of the provider's city was **not** migrated:

- The SPA still ships the hardcoded `CITY_OPTIONS_BY_COUNTRY` list (`onboardingFieldConfigs.ts`) with values
  `istanbul`, `izmir`, `antalya`, `bodrum`, … and still sends the field as `cityOrPort`.
- `MirrorDraftToProfile` accepts that legacy field and merely uppercases it → `UserProfile.City = "IZMIR"`.
- Requests are published to `city:35`.

`city:IZMIR` ≠ `city:35`. **Every newly onboarded provider receives nothing.** Same failure as before, new spelling.

Worse, two specific consequences you must handle:

1. **The proven backplane test no longer passes.** Profile 100011's `City` was hand-set to `izmir` during that
   test; the hub will join `city:IZMIR` while requests go to `city:35`. The six toasts that proved the backplane
   would not arrive today. Do not let that pass silently — it is the canary.
2. **Free text is being accepted as a code.** The legacy `cityOrPort` fallback takes whatever the browser sends
   (`"Bodrum"`, a district; `"Çeşme Marina"`, a port) and stores it as if it were a ReferenceData code. Nothing
   rejects it. The previous prompt explicitly banned this: a permissive matcher hides the exact class of bug we
   are removing.

## What to do — in this order

### 1. Make the producer speak the canonical vocabulary (SPA)

- Delete `CITY_OPTIONS_BY_COUNTRY` from the SPA. A hardcoded city list in the browser is how the two vocabularies
  drifted apart in the first place.
- Bind the `OperatingRegion` city select to the endpoint that already exists:
  `GET /provider/location/cities?country=TR` (ReferenceData-backed). Option value = the canonical code, label =
  the human name. The user sees "Muğla"; the wire carries `48`.
- Rename the field `cityOrPort` → `cityCode` in the schema, the form, the draft, and the review screen. The old
  name *is* the bug: a field that may hold a city **or** a port cannot be matched against a city code.

### 2. Close the free-text door (Identity)

- **Remove the legacy `cityOrPort` fallback** from `MirrorDraftToProfile`. It exists only to accept the data the
  SPA is about to stop sending.
- **Validate server-side against ReferenceData** on save/submit: an unknown city code is **rejected** — not
  stored, not uppercased into something plausible. Fail closed, with a clear validation error.
- Uppercasing/trimming stays in exactly one place. Do not sprinkle normalisation across callers.

### 3. Backfill, including the test data

- Backfill existing `UserProfile.City` values to canonical codes. Profile 100011's hand-written `izmir` must
  become `35`.
- Anything you cannot map confidently: **list it for a human.** Do not guess, do not fuzzy-match, do not pick the
  nearest string. A wrong city is worse than a null one, because a null is visible.

### 4. Prove it — the same way it broke

Re-run the two-replica backplane test end to end (`docs/prompts/realtime-two-replica-backplane-test-claude-code-prompt.md`):

- The browser connects; the hub logs `Realtime connected: provider 100011, city 35`.
- Publish requests in city `35`; **all** arrive, including those consumed by the instance holding no socket.
- If they arrive, say which instance consumed each. If they do not, report that plainly — do not adjust the
  matcher until it passes.

Then prove the vocabularies actually agree, with data rather than assertion:

- distinct `ServiceRequest.LocationCityCode` values, and
- distinct `UserProfile.City` values.

Paste both lists. **Every value on both sides must be a valid ReferenceData city code.** If a single value on
either side is not, the job is not done.

### 5. Onboarding, through the real screens

Take a **new** provider through onboarding in the browser and confirm `UserProfile.City` holds the canonical code
— not free text, not null. Then confirm the hub joins `city:{that code}`.

## Constraints

- **Do not use the "open items" section as a place to put the work that makes the feature function.** If a step
  cannot be completed, the feature must be reported as **not working**, not as done-with-follow-ups. The previous
  report described a system that delivers zero events to every provider as "complete".
- No fuzzy matching anywhere: no `contains`, no `startsWith`, no case-guessing, no "did they mean Muğla?".
  Equality on canonical codes, or rejection.
- No geo/radius logic — that is GeoDiscovery. Bodrum is a district of `48`; port-level matching is a later phase.
- Provider identity from the BFF assertion; never from body or query.
- Group membership stays server-decided. No client-callable subscribe.
