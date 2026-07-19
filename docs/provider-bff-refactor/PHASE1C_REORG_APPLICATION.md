# Phase 1C — reorganize the Application layer into `{Feature}/Command|Query/{Op}/`

Mechanical restructure, **no behaviour change**. Bring every existing Command/Query under a consistent
`{Feature}/Command/{Op}/` or `{Feature}/Query/{Op}/` layout matching the reference
`ServiceRequests/Query/GetAttachmentReadUrlBff/GetAttachmentReadUrlBffQuery.cs`.

## Rules
1. **Folder:** `Aizen.Bff.MarineProvider.Application/{Feature}/{Command|Query}/{OperationName}/` — the operation's
   `*Command.cs` / `*CommandHandler.cs` / `*CommandValidator.cs` (or `*Query.cs` / `*QueryHandler.cs`) plus any
   `*Response.cs` that belongs to it live together in that folder.
2. **Command vs Query:** decided by the CQRS base type, i.e. the existing filename suffix — operations whose message
   is an `AizenCommand` (`*Command.cs`) go under `Command/`; `AizenQuery` (`*Query.cs`) go under `Query/`.
   **Judge by the type suffix, not the verb** — e.g. `GetDocumentAccessUrlCommand` is a Command (goes under
   `Command/`) even though it starts with "Get".
3. **Flatten sub-groups.** No intermediate grouping folders — Auth's `OtpLogin/` and `Password/`, and Onboarding's
   `Documents/`, are removed; the operation folder sits directly under `Auth/Command/…` / `Onboarding/Command/…`.
4. **Namespace = feature level, flat:** every moved file's namespace becomes
   `namespace Aizen.Bff.MarineProvider.Application.{Feature};` (e.g. `…Application.Auth`, `…Application.Me`,
   `…Application.Onboarding`) — matching the reference and the already-flat `ServiceRequests`/`Offers`. This
   **normalizes** the files that currently use operation-level namespaces (e.g.
   `…Application.Auth.OtpLogin.RequestOtpLogin` → `…Application.Auth`; `…Application.Me.GetProviderMe` → `…Application.Me`).
5. **Update every `using`.** Controllers and handlers that import the old operation-level namespaces
   (`using …Application.Me.GetProviderMe;` etc.) must switch to the feature-level namespace and de-duplicate
   (a controller that imported five `Me.*` operation namespaces ends up with a single `using …Application.Me;`).
6. Leave `ServiceRequests/Query/GetAttachmentReadUrlBff/` as-is (already correct). Do **not** touch `Common/`,
   `Contracts/`, `DependencyInjection.cs` logic, or any controller body/route — only file locations + namespaces +
   `using` lines.

## Full mapping (existing → target folder)
**Auth** (all Command): `EnsureProviderProfile`, `RegisterProvider`, `RequestOtpLogin`, `ResendOtpLogin`,
`VerifyOtpLogin`, `ForgotProviderPassword`, `ResendProviderPasswordOtp`, `ResetProviderPassword`,
`VerifyProviderPasswordOtp` → `Auth/Command/{Op}/` (namespace `…Application.Auth`).

**Files** (Command): `CompleteUpload`, `CreateUploadSession` → `Files/Command/{Op}/` (ns `…Application.Files`).

**Jobs** (Query): `GetProviderJobs` → `Jobs/Query/GetProviderJobs/` (ns `…Application.Jobs`).

**Me** (Query): `GetProviderMe`, `GetProviderProfile`, `GetProviderStatus` → `Me/Query/{Op}/` (ns `…Application.Me`).

**Notifications** (Command): `SubscribePush`, `UnsubscribePush` → `Notifications/Command/{Op}/`
(ns `…Application.Notifications`).

**Offers**: Query → `GetMyOffersBff`; Command → `CreateOfferBff`, `PreviewOfferBff`, `SaveOfferDraftBff`,
`SubmitOfferBff`, `UpdateOfferBff`, `WithdrawOfferBff` → `Offers/{Query|Command}/{Op}/` (ns `…Application.Offers`).

**Onboarding**: Query → `GetOnboarding`; Command → `SaveOnboardingStep`, `SubmitOnboarding`,
`AttachOnboardingDocument`, `DeleteOnboardingDocument`, `GetDocumentAccessUrl` (all Commands; `Documents/` group
removed) → `Onboarding/{Query|Command}/{Op}/` (ns `…Application.Onboarding`).

**Phone** (Command): `SendProviderPhoneOtp`, `VerifyProviderPhoneOtp` → `Phone/Command/{Op}/`
(ns `…Application.Phone`).

**ServiceRequests**: Query → `GetDiscoveryMarkersBff`, `GetDiscoverySummaryBff`, `GetOpenServiceRequestsBff`,
`GetProviderDiscoveryBff` (+ its `GetProviderDiscoveryBffResponse.cs`), `GetProviderMessages`,
`GetServiceRequestDetailBff`; Command → `SendProviderMessage` → `ServiceRequests/{Query|Command}/{Op}/`
(ns already `…Application.ServiceRequests`). `GetAttachmentReadUrlBff` already correct — leave it.

## Acceptance
- Solution builds (0 errors). Every operation lives under `{Feature}/Command/{Op}/` or `{Feature}/Query/{Op}/`;
  no operation files remain loose at a feature root or under a removed sub-group (`Auth/OtpLogin`, `Auth/Password`,
  `Onboarding/Documents` folders gone).
- `grep -rn "Application\.\(Auth\|Me\|Onboarding\|Phone\|Jobs\|Files\)\.[A-Z]" --include=*.cs` (operation-level
  namespaces) returns nothing — all normalized to feature level.
- After rebuild: smoke still 200 — `/provider/me/status`, `/provider/jobs/summary`, `/provider/service-requests/open`,
  `/provider/onboarding`, login/OTP flow reachable. Behaviour identical.

## Report
Append to `REPORT_BACKEND.md` ("Phase 1C"): reorganized all existing BFF handlers into `{Feature}/Command|Query/{Op}/`,
flattened sub-groups, normalized namespaces to feature level, updated + de-duplicated `using`s; no behaviour change.
