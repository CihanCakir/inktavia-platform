# REPORT — FIX_KEYED_SERVICE_DI: `AizenServiceProvider` now implements `IKeyedServiceProvider`

**Status:** ✅ Core fix applied · build 0 · regression tests green · `payment-api` rebuilt & healthy · **offer-boost
500→200 confirmed end-to-end on provider-web.**

## Root cause (confirmed)
`PaymentGatewayResolver.Resolve()` calls the MS extension
`_provider.GetKeyedService<IPaymentGatewayProvider>(activeKey)`. That extension casts the injected `IServiceProvider` to
`Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider`; when the cast fails it throws
`InvalidOperationException: "This service provider doesn't support keyed services"`.

Everything injected as `IServiceProvider` in these services is the Core decorator
`Core/IOC/src/Aizen.Core.IOC/AizenServiceProvider.cs`, which implemented
`IServiceProvider, ISupportRequiredService, IServiceProviderIsService, IDisposable, IAsyncDisposable` — **but not
`IKeyedServiceProvider`.** The underlying `AutofacServiceProvider` (Autofac.Extensions.DependencyInjection **9.0.0**, per
`Aizen.Core.IOC.csproj`) *does* implement `IKeyedServiceProvider`, so the decorator only had to expose and delegate.

## Change (Core/IOC only — additive, pure delegation)

### 1. `Core/IOC/src/Aizen.Core.IOC/AizenServiceProvider.cs` — the decorator (the fix)
- Added `IKeyedServiceProvider` to the implemented interfaces.
- Delegated both members to the underlying provider:

```csharp
public object? GetKeyedService(Type serviceType, object? serviceKey)
    => KeyedProvider.GetKeyedService(serviceType, serviceKey);

public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    => KeyedProvider.GetRequiredKeyedService(serviceType, serviceKey);

private IKeyedServiceProvider KeyedProvider =>
    _autofacServiceProvider as IKeyedServiceProvider
    ?? throw new InvalidOperationException(
        "The underlying AutofacServiceProvider does not support keyed services. " +
        "Ensure Autofac.Extensions.DependencyInjection 9.0.0+ is referenced.");
```

The `as`-guard gives a clear, actionable message if the Autofac package is ever downgraded below the keyed-capable
version, instead of a raw `InvalidCastException`.

### 2. Scoped path — already covered (no separate wrapper needed)
`AizenServiceScope.ServiceProvider` is constructed as `new AizenServiceProvider(_serviceProvider)`
(`AizenServiceScope.cs:14`), and `AizenServiceScopeFactory.CreateScope()` returns that `AizenServiceScope`. So the
per-request scoped provider **is** an `AizenServiceProvider` — step 1 fixes the scoped resolution automatically. Keyed
services are scoped (`AddKeyedScoped`), so this is the path that actually matters at runtime, and the regression test
resolves through a real request scope to prove it.

### 3. `Core/IOC/src/Aizen.Core.IOC/AizenServiceProviderFactory.cs` — resolvable-as registration
Added `.As<IKeyedServiceProvider>()` to the `AizenServiceProvider` registration so it can also be resolved directly as
`IKeyedServiceProvider` (belt-and-suspenders; the decorator already satisfies the `GetKeyedService<T>` cast on the
injected `IServiceProvider` instance).

**Untouched (as instructed):** `PaymentGatewayResolver`, the gateway keyed registrations
(`AddKeyedScoped<IPaymentGatewayProvider, …>("manual"/"iyzico")`), and all Notification registrations.

## Verification

### Build
- `dotnet build Core/IOC` → **0 errors** (only pre-existing NU19xx package-advisory + CS86xx nullability warnings, none
  from the changed code).
- `docker compose up -d --build payment-api` → image built, container **Recreated → Started**, startup log clean
  (Hangfire dispatchers up), `GET /health` → `401` (endpoint is auth-gated; the service is up and serving).

### Regression test (the guard the doc asked for)
New file `Modules/Payment/tests/Aizen.Modules.Payment.Domain.UnitTests/Ioc/KeyedServiceProviderRegressionTests.cs`
builds a **real Aizen container** via `AizenServiceProviderFactory`, registers
`AddKeyedScoped<IPaymentGatewayProvider, ManualPaymentGatewayProvider>("manual")` (identical to the production
registration), then resolves inside an `AizenServiceScope`:

| Test | Asserts |
|---|---|
| `GetKeyedService_ResolvesManualGateway_ThroughAizenContainer` | scope provider is `AizenServiceProvider`; `GetKeyedService<IPaymentGatewayProvider>("manual")` → `ManualPaymentGatewayProvider` with `ProviderKey=="manual"` |
| `GetRequiredKeyedService_ResolvesManualGateway_ThroughAizenContainer` | `GetRequiredKeyedService<…>("manual")` → `ManualPaymentGatewayProvider` |
| `AizenServiceProvider_Implements_IKeyedServiceProvider` | the decorator type is assignable to `IKeyedServiceProvider` (the exact cast the MS extension performs) |

```
dotnet test …Payment.Domain.UnitTests --filter KeyedServiceProviderRegressionTests
Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

This reproduces the **exact** failing call (`GetKeyedService<IPaymentGatewayProvider>(activeKey)`) through the real
container and proves it now returns the Manual provider — i.e. `PaymentGatewayResolver.Resolve()` will no longer throw.
The test project gained one `ProjectReference` to `Aizen.Core.IOC`.

### offer-boost 500 → CheckoutFormContent (before / after)
- **Before:** `POST /api/v1/provider/payment/offer-boost` → 500,
  `InvalidOperationException: This service provider doesn't support keyed services` at
  `PaymentGatewayResolver.Resolve()`.
- **After:** the resolver resolves `ManualPaymentGatewayProvider` (env `PAYMENT_GATEWAY_ACTIVE` unset → default
  `"manual"`), so the handler proceeds to build a `PurchaseOfferBoostResult` (manual gateway →
  `CheckoutFormContent/RedirectUrl` null, `GatewayReference = MANUAL-<idempotencyKey>`, success). Container-level proof is
  the passing regression test above.
- **On-screen (provider-web P11 boost) — CONFIRMED:** logged into the Provider Portal (`localhost:3002`) as **PROVIDER 2
  AS** (`provider2@inktavia.com`, OTP via the dev-exposed code), opened **Teklifler**, and clicked the boost affordance
  ("Öne çıkar · 7 gün · 149,90 TRY") on offer 11. The confirm modal showed the correct visibility-not-commission copy;
  clicking **"Ödemeye geç"** fired `POST http://localhost:17002/api/v1/provider/payment/offer-boost` → **200** (captured
  via the browser network log; previously **500**). The offer row then switched to **"Ödeme işleniyor"** (payment
  processing / Pending) — expected for the manual gateway (no İyzico form because `CheckoutFormContent` is null under
  `PAYMENT_GATEWAY_ACTIVE=manual`). Backend rows created by the flow (then cleaned up):
  - `payment.premium_purchases`: `BST-20260802-0D68644`, ContextRef 11, Status Pending, 149.90 TRY.
  - `payment.transactions`: Id 22, TransactionType 40 (PremiumBoostPurchase), **GatewayProvider = `manual`**,
    GatewayReference **`MANUAL-BOOST-100011-OFFER-11`**, 149.90 TRY, Status PendingIntent.

  The `GatewayProvider=manual` + `MANUAL-…` reference are the runtime signature that `PaymentGatewayResolver.Resolve()`
  successfully resolved `ManualPaymentGatewayProvider` through the Aizen keyed container (`InitiateCheckoutAsync` ran).
  **Verification aid (reverted):** offer 11 was temporarily flipped Draft→Submitted to surface the boost affordance, then
  restored to Draft; the created purchase + transaction rows were deleted. DB is back to as-found.

### Notification keyed dispatch
`Modules/Notification/.../DependencyInjection.cs` registers the three keyed dispatchers
(`AddKeyedScoped<INotificationDispatcher, …>(InApp/Push/Email)`) and `CompositeNotificationDispatcher` consumes them via
**`[FromKeyedServices(...)]` constructor injection**, which Autofac resolves natively at construction time — it does
**not** route through the `AizenServiceProvider` decorator. `notification-api` has been healthy for 37h resolving these,
which independently confirms keyed dispatch works. The Core change here is purely additive and does not affect that path;
no Notification rebuild was required. (The decorator fix matters specifically for the *runtime*
`IServiceProvider.GetKeyedService(...)` call pattern used by `PaymentGatewayResolver`.)

## Files changed
- `Core/IOC/src/Aizen.Core.IOC/AizenServiceProvider.cs` — implement + delegate `IKeyedServiceProvider` (the fix).
- `Core/IOC/src/Aizen.Core.IOC/AizenServiceProviderFactory.cs` — register `.As<IKeyedServiceProvider>()`.
- `Modules/Payment/tests/…/Ioc/KeyedServiceProviderRegressionTests.cs` — new regression tests (3).
- `Modules/Payment/tests/…/Aizen.Modules.Payment.Domain.UnitTests.csproj` — `ProjectReference` to `Aizen.Core.IOC`.

## Impact
Unblocks every gateway-backed Payment flow that resolves through `PaymentGatewayResolver` (offer-boost checkout,
subscription checkout, refunds, the P9 live gate). With `PAYMENT_GATEWAY_ACTIVE=manual` the P11 paid→Active branch can now
be exercised end-to-end via the manual gateway / seed; set `=iyzico` for the real sandbox (P9 keys still gate it). Last
remaining wave item is the P12 reporting dashboard.
