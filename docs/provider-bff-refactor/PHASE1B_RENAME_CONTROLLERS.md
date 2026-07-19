# Phase 1B — rename controllers `Provider{X}Controller` → `{X}Controller`

Mechanical rename, **no behaviour change**. This BFF is provider-only, so the `Provider` prefix is redundant.
Confirmed: no external references to these class names and no target-name collisions in the BFF assembly. Routes are
explicit string literals, so dropping the prefix does not affect any URL.

## The 14 renames (file + class name)
`Bff/src/MarineProvider/Aizen.Bff.MarineProvider/Controllers/V1/`:
| Old file / class | New file / class |
|---|---|
| `ProviderAuthController` | `AuthController` |
| `ProviderCargoDryController` | `CargoDryController` |
| `ProviderCatalogController` | `CatalogController` |
| `ProviderJobsController` | `JobsController` |
| `ProviderLocationController` | `LocationController` |
| `ProviderMeController` | `MeController` |
| `ProviderNotificationsController` | `NotificationsController` |
| `ProviderOffersController` | `OffersController` |
| `ProviderServiceRequestsController` | `ServiceRequestsController` |
| `ProviderTemplateController` | `TemplateController` |

`Controllers/V1/Auth/`, `Controllers/V1/Files/`, `Controllers/V1/Onboarding/`:
| Old | New |
|---|---|
| `Auth/ProviderOtpLoginController` | `Auth/OtpLoginController` |
| `Auth/ProviderPasswordRecoveryController` | `Auth/PasswordRecoveryController` |
| `Files/ProviderFileController` | `Files/FileController` |
| `Onboarding/ProviderOnboardingController` | `Onboarding/OnboardingController` |

## Rules
- Rename the **file** and the **class name** only. Keep everything else byte-identical: `[Route(...)]`,
  `[Tags(...)]`, `[Authorize(Policy = ...)]`, base class `AizenWebApiController`, constructor, all action methods,
  attributes, and `[ProducesResponseType]` where present.
- Do **not** touch routes, tags, policies, request/response types, or the Application layer. Structure only.
- Controllers are auto-discovered (`AddControllers`) — no DI registration to update. If any test or comment
  references the old class name, update it.

## Acceptance
- Solution builds (0 errors). `grep -rn "class Provider.*Controller" Bff/src/MarineProvider --include=*.cs`
  (excluding `bin/obj`) returns **nothing**.
- After `docker compose build bff-marineprovider bff-marineprovider-2 && docker compose up -d …`: the same smoke
  set still 200 — `/provider/me/status`, `/provider/jobs/summary`, `/provider/cargodry/overview`,
  `/provider/service-requests/open`. All routes unchanged (no URL moved).

## Report
Append to `REPORT_BACKEND.md` ("Phase 1B"): dropped the `Provider` prefix from all 14 controller classes/files;
routes/tags/policies unchanged; no behaviour change; smoke green.
