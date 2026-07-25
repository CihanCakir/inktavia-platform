# PAY-2 — Provider Payment Profile (bank account + tax) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. Builds on **PAY-0** (BFF↔Payment bridge already in place:
> `aud-payment-api` mapper, `payment-api` BffAssertion, `PaymentProviderController` resolving `ProviderProfileId` from
> the assertion). This adds the provider's **payment profile**: bank account (IBAN) + tax info the provider manages —
> the prerequisite for getting paid (no IBAN ⇒ no payout).
>
> **Manage surface (GET + PUT), provider-scoped, security-critical.** IBAN and tax number are PII/financial: stored
> encrypted, **returned masked**, never exposed in the clear.

## Verified anchors
- `ProviderPaymentProfileEntity` (1:1 with provider): `ProviderProfileId`, `GatewayProvider` ("manual"|"iyzico"),
  `SubMerchantKey?`, `SubMerchantAccountId?`, `IbanEncrypted?` (AES-256, **stored encrypted**), `LegalName?`,
  `TaxNumber?` (mask in APIs), `Status` ("Active"|"OnHold"|"Blocked"), `VerifiedAt?`; method
  `UpdateIban(string ibanEncrypted)` (entity receives the ALREADY-ENCRYPTED value — encryption happens in Application).
- Repo `IProviderPaymentProfileRepository.GetByProviderProfileIdAsync(long pid, ct)` → entity or null (+ Add/Update).
- `PaymentProviderController` (PAY-0) resolves `ResolveProviderProfileId()` from the assertion.

## Security rules (non-negotiable)
1. **Never return raw IBAN or full tax number.** DTO exposes: `IbanMasked` (e.g. `TR** **** **** 1234` — last 4 only),
   `TaxNumberMasked` (last 2–3), `LegalName`, `GatewayProvider`, `Status`, `VerifiedAt`, `HasIban` (bool).
2. **Encrypt IBAN before persisting.** Reuse the existing platform IBAN/secret encryption path that produced
   `IbanEncrypted` (locate it — an encryptor/`IDataProtector`/AES helper in Payment or Core). If none exists yet, add a
   small AES-256-GCM encryptor whose key comes from config/secret env (`Payment:IbanEncryptionKey`), **never committed**.
   Decrypt only server-side when actually disbursing (out of scope here).
3. **Reset verification on change.** Any IBAN/tax/legal-name change → `VerifiedAt = null`, `Status = "OnHold"`
   (re-verification required). The system does NOT auto-verify or initiate any bank action — provider enters data,
   admin/gateway verifies later (separate epic).
4. Provider edits ONLY their own profile (pid from assertion).

---

## DTOs (Payment.Abstraction)
```csharp
// Response (masked)
public sealed class ProviderPaymentProfileDto
{
    public string  GatewayProvider { get; init; } = default!;
    public bool    HasIban         { get; init; }
    public string? IbanMasked      { get; init; }        // last 4 only, e.g. "TR** **** **** 1234"
    public string? LegalName       { get; init; }
    public string? TaxNumberMasked { get; init; }        // last 2–3 only
    public string  Status          { get; init; } = "OnHold";   // Active | OnHold | Blocked
    public DateTimeOffset? VerifiedAt { get; init; }
}
// Request (Abstraction/Request)
public sealed class UpsertProviderPaymentProfileRequest
{
    public string  Iban       { get; set; } = default!;   // raw; validated + encrypted server-side, never stored/returned raw
    public string? LegalName  { get; set; }
    public string? TaxNumber  { get; set; }               // raw; masked in responses
}
```

## Query + command
- `GetProviderPaymentProfile { long ProviderProfileId }` → `ProviderPaymentProfileDto?`. Handler:
  `GetByProviderProfileIdAsync(pid)`; map to masked DTO (decrypt is NOT needed for masking — derive last-4 from a
  stored `IbanLast4`/`IbanMasked` column, OR decrypt server-side to compute the mask then discard). **Prefer storing a
  small non-sensitive `IbanLast4` alongside `IbanEncrypted`** (add a nullable column + migration) so the mask never
  requires decryption on read.
- `UpsertProviderPaymentProfile { long ProviderProfileId; string Iban; string? LegalName; string? TaxNumber }` →
  `ProviderPaymentProfileDto`. Handler:
  1. Validate IBAN format (TR IBAN: 26 chars, checksum). Reject invalid → business error.
  2. Encrypt IBAN → `UpdateIban(encrypted)`; store `IbanLast4`; set `LegalName`, `TaxNumber` (encrypted or masked-store),
     `VerifiedAt=null`, `Status="OnHold"`.
  3. Create the profile if none exists (1:1 upsert), else update.
  4. Return the masked DTO.

## Controller (PaymentProviderController)
```
GET /api/v1/payment/provider/payment-profile   → AizenApiResponse<ProviderPaymentProfileDto?>
PUT /api/v1/payment/provider/payment-profile   → AizenApiResponse<ProviderPaymentProfileDto>   (body: UpsertProviderPaymentProfileRequest)
```
Resolve `ResolveProviderProfileId()`; typed + `[ProducesResponseType]`.

## BFF passthrough
- `IPaymentRemoteCall`: `GetPaymentProfile()`, `UpsertPaymentProfile([Body] UpsertProviderPaymentProfileRequest body)`.
- BFF query `GetProviderPaymentProfileBff` + command `UpsertProviderPaymentProfileBff` (resolver → `.Body`).
- BFF `PaymentController`: `GET payment-profile` + `PUT payment-profile`, typed + PRT.
  Routes: `GET/PUT /api/v1/provider/payment/payment-profile`.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Table: `provider_payment_profiles`. DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `payment-api` + `bff-marineprovider`; migration (IbanLast4)
   applied; clean boot.
2. **PUT then GET (provider2 token):**
   ```
   PUT /api/v1/provider/payment/payment-profile   body: { "iban":"TR330006100519786457841326", "legalName":"Provider 2 AS", "taxNumber":"1234567890" }
   #   200 → masked DTO: hasIban=true, ibanMasked ends "1326", status="OnHold", verifiedAt=null
   GET /api/v1/provider/payment/payment-profile
   #   200 → same masked view; NO raw iban / full taxNumber anywhere in the payload
   ```
3. **DB — stored encrypted, not plaintext:**
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"IbanEncrypted\" !~ 'TR33' AS iban_not_plaintext, \"IbanLast4\", \"Status\", \"VerifiedAt\"
       FROM provider_payment_profiles WHERE \"ProviderProfileId\"=100011;"
   #   iban_not_plaintext = t (encrypted, not the raw IBAN), IbanLast4='1326', Status='OnHold', VerifiedAt null
   ```
4. **Verification reset:** PUT again with a different IBAN → `VerifiedAt` stays null / resets, `IbanLast4` updates.
5. **Isolation + masking:** response payloads never contain the raw IBAN or full tax number (grep the HTTP body).
6. **Invalid IBAN** → business error (not 500), profile unchanged.

## Acceptance
- Provider GET returns a **masked** profile (last-4 IBAN, masked tax); PUT validates + **encrypts** IBAN, stores
  `IbanLast4`, resets verification (`VerifiedAt=null`, `Status=OnHold`). Raw IBAN/tax never leave the server.
- Provider-scoped (pid from assertion); 1:1 upsert. No auto bank action. Build clean; DB proves encryption; evidence pasted.

## Report
`REPORT_BACKEND.md` ("PAY-2"): provider payment-profile GET(masked)/PUT(encrypt+reset-verification), `IbanLast4`
column (+migration), IBAN encryption reused/established, BFF passthrough, typed + PRT. Verified: masked responses,
encrypted-at-rest (DB), verification reset, isolation, invalid-IBAN handling.
