# FIX — keyed-service DI: make `AizenServiceProvider` support `IKeyedServiceProvider`

> **Root-caused, single-file Core fix.** `POST offer-boost` (and every gateway-backed payment) 500s with
> `InvalidOperationException: This service provider doesn't support keyed services` at
> `PaymentGatewayResolver.Resolve()` → `_provider.GetKeyedService<IPaymentGatewayProvider>(activeKey)`. The services ARE
> registered keyed (`AddKeyedScoped<IPaymentGatewayProvider, ...>("manual"/"iyzico")`), and the underlying container
> (Autofac.Extensions.DependencyInjection **9.0.0**) DOES support keyed services — but the Aizen decorator injected
> everywhere as `IServiceProvider` doesn't expose it.
>
> **This is a Core/IOC change (fleet-wide surface) — but it's the correct root cause, additive (implementing a missing
> interface), and low-risk (pure delegation to the already-capable underlying provider). It unblocks multiple modules at
> once.** Do NOT rewrite the resolvers to avoid keyed services; fix the decorator.

## Root cause (confirmed)
`Core/IOC/src/Aizen.Core.IOC/AizenServiceProvider.cs` wraps an `AutofacServiceProvider` and implements
`IServiceProvider, ISupportRequiredService, IServiceProviderIsService, IDisposable, IAsyncDisposable` — **but not
`Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider`.** `GetKeyedService<T>(key)` (the MS extension) requires
the provider to implement `IKeyedServiceProvider`; when it doesn't, it throws "doesn't support keyed services".
`AutofacServiceProvider` (Autofac.Extensions.DI 9.0) DOES implement `IKeyedServiceProvider`, so the decorator can simply
delegate.

**Impact (both currently broken/latent at runtime):**
- Payment: `PaymentGatewayResolver.Resolve()` → all gateway-backed flows (offer-boost checkout, subscription checkout,
  refunds, the P9 live gate — anything that resolves a gateway).
- Notification: keyed `INotificationDispatcher` (InApp/Push/Email) in `Modules/Notification/.../DependencyInjection.cs`.

## Change (minimal)
1. In `AizenServiceProvider`: add `IKeyedServiceProvider` to the implemented interfaces and delegate to the underlying
   provider:
   ```csharp
   public object? GetKeyedService(Type serviceType, object? serviceKey)
       => ((IKeyedServiceProvider)_autofacServiceProvider).GetKeyedService(serviceType, serviceKey);

   public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
       => ((IKeyedServiceProvider)_autofacServiceProvider).GetRequiredKeyedService(serviceType, serviceKey);
   ```
   (Both `AizenServiceProvider` constructors wrap an `AutofacServiceProvider`, which supports keyed — so the cast is
   safe. If you prefer, guard with a helpful exception if the cast ever fails.)
2. **Scoped resolution:** verify the scoped provider path is covered — `AizenServiceScopeFactory` /
   `AizenServiceScope` (Core/IOC) must also expose keyed services (a scoped `IServiceProvider` that resolves the keyed
   gateway inside a request scope). If the scope's `ServiceProvider` is an `AizenServiceProvider`, step 1 covers it; if
   it's a separate wrapper, apply the same `IKeyedServiceProvider` delegation there. Also register the factory's
   `AizenServiceProvider` `.As<IKeyedServiceProvider>()` in `AizenServiceProviderFactory.CreateBuilder` if needed so it's
   resolvable as that interface.

Keep it to Core/IOC — no changes to `PaymentGatewayResolver`, the gateway registrations, or the Notification
registrations (they're already correct).

## Verification
1. Rebuild + restart the affected services (payment-api at minimum; the Core change is shared, so any service that
   resolves keyed services benefits): `docker compose up -d --build payment-api`.
2. **Payment:** with a real provider offer, `POST /api/v1/provider/payment/offer-boost { offerId, currencyCode:'TRY' }`
   no longer 500s — it returns a `PurchaseOfferBoostResult` with `CheckoutFormContent`/`RedirectUrl` (the gateway
   resolved). On-screen (provider-web): the P11 boost action now opens the iyzico checkout (the FE was already built).
   `PAYMENT_GATEWAY_ACTIVE` = `manual` by default → the ManualPaymentGatewayProvider resolves; set to `iyzico` where the
   real gateway is wanted (P9 keys still gate the live sandbox).
3. **Notification:** keyed dispatcher resolution works (send an in-app/push/email notification path that resolves the
   keyed `INotificationDispatcher` → no "doesn't support keyed services").
4. A quick unit/integration test asserting `provider.GetKeyedService<IPaymentGatewayProvider>("manual")` returns the
   Manual provider through the Aizen container (guards against regression).
5. Nothing else changed; no module registrations touched; build 0.

## Report
`docs/V1.0.1/Payment/REPORT_FIX_KEYED_SERVICE_DI.md`: the decorator change (+ scope wrapper if needed), the offer-boost
before/after (500 → CheckoutFormContent), the Notification keyed-dispatch confirmation, and the regression test. **This
unblocks the P11 boost checkout end-to-end (and subscription/gateway payments + Notification dispatch).** After this, the
P11 paid→Active branch can be exercised (manual gateway or seed), and the last wave item is the P12 reporting dashboard.
