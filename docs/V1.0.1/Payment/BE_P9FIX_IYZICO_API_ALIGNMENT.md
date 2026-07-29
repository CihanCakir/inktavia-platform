# BE-P9-fix — iyzico API alignment corrections (IYZWSv2 signing, approval endpoint, webhook V3, response signature, sub-merchant, refund) — Backend Prompt

> **Module:** `Aizen.Modules.Payment` (gateway/client fixes). **Phase:** P9 correction pass — **prerequisite for the live
> P9 sandbox gate** (§1 and §4 are BLOCKERs; without them every live call 401s / 404s).
> **Source of truth:** `docs/V1.0.1/Payment/IYZICO_API_ALIGNMENT.md` (official iyzico models + exact correction points,
> fetched 2026-07-28). Follow its §-numbering.
> **Rule:** EXTEND/FIX the existing `IyzicoHttpClient` + gateway + `RegisterSubMerchantCommand` + webhook handler; do NOT
> rewrite the working checkout/basket/pre-send-split-guard from P9. **No external keys needed for this pass** — everything
> is provable with the documented HMAC test vectors + mock HttpMessageHandler. Do NOT commit/print secrets.

## Priority order (do in this order; §1 & §4 first — they unblock the gate)

### §1 🔴 IYZWSv2 signing — fix `IyzicoHttpClient.BuildAuthHeader` + thread `uriPath`
Current (WRONG, 3 bugs): `payload = apiKey + randomKey + body`, `base64(hmac)`, authString `apiKey:randomKey:hmacB64`.
**Correct algorithm:**
```
signature (encryptedData) = HEX_lower( HMACSHA256( key = secretKey , data = randomKey + uriPath + requestBody ) )
authorizationString       = "apiKey:" + apiKey + "&randomKey:" + randomKey + "&signature:" + signature
Authorization header      = "IYZWSv2 " + base64( authorizationString )     // single space after IYZWSv2
x-iyzi-rnd header         = randomKey                                       // same randomKey (already sent ✅)
```
- `uriPath` = the endpoint path used in the call (e.g. `/payment/iyzipos/item/approve`), query-less. **`SendJsonAsync` must
  pass `path` into `BuildAuthHeader`** (it currently doesn't). Applies to POST and PUT alike.
- Use `Convert.ToHexString(hmac).ToLowerInvariant()` for the signature (NOT base64).
- **Proof unit test (no keys):** replicate the doc example — body `{"locale":"tr","binNumber":"535805","conversationId":"docsTest-v1"}`,
  path `/payment/bin/check`, a fixed randomKey + secretKey → assert the produced `encryptedData` equals the documented
  hex, and the final `Authorization` decodes to `apiKey:...&randomKey:...&signature:<hex>`. (Pin the byte-exact body
  serialization the client uses so the HMAC input matches.)

### §4 🔴 Approval endpoint — `ReleaseEscrowAsync` must use item-approve
- Remove/redirect `ApproveMarketplacePaymentAsync` → `/payment/marketplace/approval` (**no such endpoint → 404**).
- `ReleaseEscrowAsync` → `ApproveItemAsync` (`POST /payment/iyzipos/item/approve`, body `{ paymentTransactionId }`) — the
  P9 client method with the correct path.
- **`paymentTransactionId` is the per-item id from the checkout-retrieve `itemTransactions[].paymentTransactionId`** — NOT
  the payment-level `paymentId`. It must be captured/stored at capture time (see §6). One approve per split item.

### §6 🟡 Persist checkout-retrieve breakdown + map transactionStatus
- On checkout-retrieve (`/payment/iyzipos/checkoutform/auth/ecom/detail`), store per split item:
  `paymentTransactionId`, `subMerchantPayoutAmount`, `blockageRate*`, `paidPrice`. The transaction needs these for
  approve/refund/settlement.
- Map `itemTransactions[].transactionStatus`: **1 = captured & awaiting marketplace approval (funds held in pool)**,
  **2 = approved/released to sub-merchant**, 0 = fraud review, -1 = rejected. Escrow "held" = 1, "released" = 2.
  `fraudStatus` 1/0/-1. Only treat status/fraud appropriately before release.

### §2 🟠 Webhook `X-IYZ-SIGNATURE-V3` (HPP format)
Fix `ValidateWebhookSignature` (currently `base64(SHA256(WebhookSecret+token))` — wrong header, algo, inputs):
- Read header **`X-IYZ-SIGNATURE-V3`** (V1/V2 deprecated).
- HPP format (our CheckoutForm case):
  `sig = HEX_lower( HMACSHA256( key = secretKey , data = secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status ) )`
  compared case-insensitively to the header. `iyziEventType = CHECKOUT_FORM_AUTH`; `status ∈ {SUCCESS, FAILURE, ...}`.
- Webhook payload fields: `{paymentConversationId, merchantId, token, status, iyziReferenceCode, iyziEventType,
  iyziEventTime, iyziPaymentId}`. Respond **2xx**; idempotency via `iyziReferenceCode`/`iyziPaymentId` (ProcessedGatewayEvent).
- **Dev-bypass (WebhookSecret empty → true) must be OFF in prod** (guard by environment; never silently accept).
- Unit test with a documented-style payload + secret → known hex (compute expected in the test; no live keys).

### §3 🟠 Response `signature` validation (CF-retrieve + refund)
Add a response-signature validator (HMACSHA256-HEX over `:`-joined params, `secretKey`, **price fields trailing-zero
trimmed** — `10.50`→`10.5`, `10.0`→`10`):
- CF-retrieve `/payment/iyzipos/checkoutform/auth/ecom/detail`: `paymentStatus, paymentId, currency, basketId,
  conversationId, paidPrice, price, token`.
- Refund `/payment/refund`: `paymentId, price, currency, conversationId`.
- If the computed signature ≠ response `signature` → **reject the result** (do not capture/release). Unit-test the
  trailing-zero trimming + join order.

### §5 🟠 Sub-merchant — type-varied bodies + remove hardcoded TCKN + update/detail
- Replace the single `IyzicoSubMerchantRequest` with `subMerchantType`-specific request bodies (discriminator):
  - **PERSONAL:** required `subMerchantType, email, gsmNumber, address, contactName, contactSurname, subMerchantExternalId,
    identityNumber(TCKN)`.
  - **PRIVATE_COMPANY:** required `subMerchantType, email, gsmNumber, address, taxOffice, legalCompanyTitle,
    subMerchantExternalId`.
  - **LIMITED_OR_JOINT_STOCK_COMPANY:** required `+ taxNumber` (taxOffice + taxNumber + legalCompanyTitle).
  - common: `iban` (optional at create but **required before product approval** → tie to split-eligibility), `currency`
    (TRY default), `locale`, `conversationId`, `name`.
- **Remove the hardcoded `identityNumber = "11111111111"`** — collect the real TCKN/tax number in onboarding (BE-I1 KYC);
  fail loudly if the required field for the chosen type is missing.
- Add client methods: `UpdateSubMerchantAsync` (`PUT /onboarding/submerchant`, **no subMerchantType**, `subMerchantKey`+`iban`
  required per variant) and `GetSubMerchantDetailAsync` (`POST /onboarding/submerchant/detail`, `{subMerchantExternalId}`).
  Wire update/detail into BE-I1's lifecycle (profile edit → PUT; verification/refresh → detail).
- **IBAN→eligibility:** a provider with no IBAN on the sub-merchant is NOT split-eligible (extend BE-I1 `IsSplitEligible`).

### §8 🟡 Refund path/reason (P10 prep — minimal here)
- `RefundAsync` (`/payment/refund`) must send **`paymentTransactionId`** (the item breakdown id), not the payment-level id;
  add `reason ∈ {OTHER, FRAUD, BUYER_REQUEST, DOUBLE_PAYMENT}` + optional `description`; honour `retryable`. Full refund
  allocation + clawback = P10 — here just correct the request shape + response signature (§3). (Cancel `/payment/cancel`
  by `paymentId` = P10.)

### §7 (defer, note only) PreAuth/PostAuth
PreAuth CF init `/payment/iyzipos/checkoutform/initialize/preauth/ecom` + `PostAuthAsync` (`/payment/postauth`
`{paymentId, paidPrice}`) + 25-day BKM close guard = **P9-PreAuth follow-up** (Capture is MVP default). Do NOT build now;
leave a `// TODO(P9-PreAuth)` seam where the auth-mode switches the init endpoint.

## Tests (no external keys — documented vectors + mock HttpMessageHandler)
- **§1 signing:** doc bin/check vector → exact `encryptedData` hex; Authorization decodes to the `apiKey:...&randomKey:...&signature:<hex>` form; path is included; POST and PUT both signed.
- **§4 approve:** ReleaseEscrow calls `/payment/iyzipos/item/approve` with the stored `paymentTransactionId`; the dead
  `/payment/marketplace/approval` path is gone.
- **§6:** retrieve parsing stores `paymentTransactionId`/payout/blockage; status 1→held, 2→released mapping.
- **§2 webhook:** HPP V3 hex over the documented concat matches; wrong signature rejected; dev-bypass off when env=prod;
  idempotent double-notify.
- **§3 response sig:** trailing-zero trim + join order → known hex; mismatch → result rejected.
- **§5 submerchant:** each type serializes only its required fields; missing required (e.g. PERSONAL without identityNumber)
  → fail-loud; no hardcoded TCKN; update PUT omits subMerchantType; detail POST shape; no-IBAN → not split-eligible.
- **§8 refund:** request carries paymentTransactionId + reason; response signature validated.
- **No regression:** P9 pre-send split guard, basket builder, item ops, BE-P8/I1 tests still green.

## Acceptance criteria
- IYZWSv2 signing matches the documented algorithm (HMAC over randomKey+path+body → HEX; `apiKey:&randomKey:&signature:`
  base64) and is proven against the doc vector; `uriPath` threaded through. Approval uses `/payment/iyzipos/item/approve`
  with the item `paymentTransactionId`; the non-existent `/payment/marketplace/approval` is removed. Webhook validates
  `X-IYZ-SIGNATURE-V3` (HPP HMACSHA256-HEX), no prod dev-bypass. Response `signature` validated (trailing-zero). Sub-merchant
  is type-varied with no hardcoded TCKN + update/detail + IBAN→eligibility. Refund request shape corrected. All provable
  without keys (documented vectors + mocks). Build clean; no CargoDry; no secrets committed/printed.

## Verify — run and PASTE output
1. `dotnet build` Payment: 0 errors.
2. Tests green (paste): §1 signing vector, §4 approve path, §6 retrieve/status, §2 webhook V3, §3 response sig, §5
   submerchant type-varied + no-TCKN + update/detail + IBAN-eligibility, §8 refund shape, no regression.
3. State explicitly: "Signing/approval/webhook/response-sig now match the official docs; LIVE sandbox gate still needs real
   keys (BE-P9 gate)."

## Report
`REPORT_BACKEND.md` ("BE-P9-fix"): corrected IYZWSv2 signing (doc-vector proven), approval endpoint, checkout-retrieve
persistence + transactionStatus mapping, webhook V3, response signature validation, sub-merchant type-varied + update/detail
+ IBAN eligibility, refund request shape. Note: PreAuth/PostAuth = P9-PreAuth follow-up; full refund allocation/clawback =
P10; **live sandbox split gate now unblocked, awaiting keys.** Do not touch CargoDry.
