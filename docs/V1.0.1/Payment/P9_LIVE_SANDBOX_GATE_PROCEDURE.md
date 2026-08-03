# P9 — iyzico live sandbox split gate (PRODUCTION GATE) — run procedure

> **The last production gate.** BE-P9 code is complete; this verifies the marketplace split against the **real iyzico
> sandbox** — the one thing that cannot be proven without live keys. **You supply the keys in your own shell/container
> env; they are never written to a file, a prompt, or a log.** (Handling/printing secret keys is prohibited — export
> them yourself; the test + services read them from env.)
>
> **Canonical gate figures (BE-P9):** Service 5000 @0.12 + Travel 800 (exempt) → **ProviderNet 5200**; + platform fee →
> **CustomerTotal 5974**; **retained = 5974 − 5200 = 774** (commission + platform fee). Split must reconcile to the
> kuruş.

## Step 1 — get sandbox keys
From the iyzico **sandbox** merchant panel, obtain the sandbox `ApiKey` + `SecretKey` (they look like `sandbox-…`).
Base URL is `https://sandbox-api.iyzipay.com`.

## Step 2 — automated portion (network-verifiable; ~30s)
In the `addesso-project` shell, export the keys **in your env only** and run the keys-gated test:
```bash
export Iyzico__ApiKey=<your-sandbox-apikey>
export Iyzico__SecretKey=<your-sandbox-secretkey>
export IYZICO_LIVE_SANDBOX=1
dotnet test Modules/Payment/tests/Aizen.Modules.Payment.Domain.UnitTests --filter Category=LiveSandbox
```
`IyzicoLiveSandboxSplitTests` (skips silently without the env) then, against the real sandbox:
1. registers a sandbox **sub-merchant** → a real `SubMerchantKey`;
2. runs the **pre-send split guard** on the split basket (Price=CustomerTotal 5974, SubMerchantPrice=ProviderNet 5200,
   retained 774) — must pass;
3. **initializes a checkout form** carrying that split → iyzico must accept it and return a form token.
**Pass = the test is green** (not skipped). A skip means the env wasn't picked up (check the three exports; `apiKey` must
not contain "placeholder").

## Step 3 — manual portion (complete a real test payment + read back the split)
The automated test stops at "iyzico accepted the split checkout". Finish the loop by hand with an iyzico **test card**:
1. Open the checkout form the init returned (or drive it through the provider boost flow / a service-request payment)
   and pay with an iyzico sandbox **test card** (e.g. `5528790000000008`, any future expiry, any CVC).
2. Let the **webhook** land (capture) → the transaction moves to captured/paid.
3. Run the **item approve / ReleaseEscrow** step (the marketplace approval) for the transaction.
4. On the **iyzico sandbox dashboard** (or via the API) read back the payment's `price`, the sub-merchant's
   `subMerchantPrice`, and the **retained** amount, and confirm **kuruşu kuruşuna**:
   - `price == 5974.00` (CustomerTotal)
   - `Σ subMerchantPrice == 5200.00` (ProviderNet to the provider's sub-merchant)
   - `retained == 774.00` (Inktavia = commission + platform fee)
That equality **is** the production gate (§10.3 / §21).

## Step 4 — (optional) wire the live gateway so the app uses iyzico end-to-end
To make the running stack actually charge via iyzico (e.g. the P11 boost checkout returns a real iyzico form instead of
the manual gateway), set in the **payment-api container env** (compose `.env`, your values):
```
PAYMENT_GATEWAY_ACTIVE=iyzico
Iyzico__ApiKey=<sandbox>       Iyzico__SecretKey=<sandbox>
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
```
Restart payment-api. Then the boost/subscription/service-request checkouts resolve `IyzicoMarketplacePaymentGatewayProvider`
(the keyed-DI fix makes this resolution work) and return real `CheckoutFormContent`/`RedirectUrl`. Complete with a test
card as above. **Keep keys in `.env` / env only — do not commit them.**

## What Claude Code can do here
If you export the three env vars in the Claude Code shell, it can **run Step 2** and report green/skip, and it can help
drive Step 3/4 (bring up payment-api with the gateway env, trace the webhook + approve, and read back the numbers). It
must **never echo or store the key values** — only confirm the split equality. Ask it to append the result to
`docs/V1.0.1/Payment/REPORT_P9_LIVE_SANDBOX_GATE.md` (masked: the SubMerchantKey and any card/token redacted; only the
5974 / 5200 / 774 reconciliation shown).

## Outcome
Green Step 2 + a reconciled Step 3 (5974 / 5200 / 774) **passes the P9 production gate** — the marketplace split is
proven against real iyzico. That clears the last go-live gate for payments (subject to the other non-code gates: VAT/YMM
sign-off, profit-protection engine — see the roadmap).
