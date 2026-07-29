namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;

// ─── BE-P4 ProviderPlanPrice — BFF DTOs (enum fields as string per the BFF JSON contract) ──

/// <summary>Create a versioned plan price. PriceType = Launch | List; BillingPeriod = Monthly | Annual.</summary>
public sealed record CreateProviderPlanPriceBffRequest(
    long      ProviderPlanId,
    string    PriceType,
    string    BillingPeriod,
    decimal   PriceAmount,
    string    CurrencyCode,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);

/// <summary>Update a plan price. <c>Id</c> forced from the route; plan/currency are immutable on the row.</summary>
public sealed record UpdateProviderPlanPriceBffRequest(
    long      Id,
    string    PriceType,
    string    BillingPeriod,
    decimal   PriceAmount,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   Notes);

public sealed record ProviderPlanPriceCreateBffResult(long Id, string PriceCode);
public sealed record ProviderPlanPriceMutateBffResult(long Id, string? PriceCode);

/// <summary>A resolved/listed plan price row (PriceType/BillingPeriod are strings per the BFF enum contract).</summary>
public sealed record ProviderPlanPriceBffDto(
    long      PriceId,
    long      ProviderPlanId,
    string    PriceType,
    string    BillingPeriod,
    decimal   PriceAmount,
    string    CurrencyCode,
    DateTime  EffectiveFrom,
    DateTime? EffectiveTo,
    string?   PriceCode);

/// <summary>A subscription whose renewal price is about to change (upcoming-changes report).</summary>
public sealed record UpcomingPriceChangeBffItem(
    long     SubscriptionId,
    long     ProviderProfileId,
    long     ProviderPlanId,
    string   CurrencyCode,
    decimal  CurrentPaidAmount,
    decimal  UpcomingPriceAmount,
    DateTime RenewalDateUtc);
