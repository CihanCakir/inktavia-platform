using System.Text.Json.Serialization;

namespace Aizen.Modules.Payment.Application.Gateway.Iyzico.Models;

// ── Shared ────────────────────────────────────────────────────────────────────

public abstract class IyzicoBaseRequest
{
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "tr";

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;
}

public abstract class IyzicoBaseResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errorGroup")]
    public string? ErrorGroup { get; set; }

    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Status == "success";
}

// ── Buyer / Address ───────────────────────────────────────────────────────────

public sealed class IyzicoBuyer
{
    [JsonPropertyName("id")]         public string Id          { get; set; } = string.Empty;
    [JsonPropertyName("name")]       public string Name        { get; set; } = string.Empty;
    [JsonPropertyName("surname")]    public string Surname     { get; set; } = string.Empty;
    [JsonPropertyName("email")]      public string Email       { get; set; } = string.Empty;
    [JsonPropertyName("identityNumber")] public string IdentityNumber { get; set; } = "11111111111";
    [JsonPropertyName("gsmNumber")]  public string? GsmNumber  { get; set; }
    [JsonPropertyName("registrationAddress")] public string RegistrationAddress { get; set; } = "N/A";
    [JsonPropertyName("city")]       public string City        { get; set; } = "Istanbul";
    [JsonPropertyName("country")]    public string Country     { get; set; } = "Turkey";
    [JsonPropertyName("ip")]         public string Ip          { get; set; } = "85.34.78.112";
}

public sealed class IyzicoAddress
{
    [JsonPropertyName("contactName")]   public string ContactName   { get; set; } = string.Empty;
    [JsonPropertyName("city")]          public string City          { get; set; } = "Istanbul";
    [JsonPropertyName("country")]       public string Country       { get; set; } = "Turkey";
    [JsonPropertyName("address")]       public string Address       { get; set; } = "N/A";
    [JsonPropertyName("zipCode")]       public string? ZipCode      { get; set; }
}

// ── Basket item ───────────────────────────────────────────────────────────────

public sealed class IyzicoBasketItem
{
    [JsonPropertyName("id")]              public string Id              { get; set; } = string.Empty;
    [JsonPropertyName("name")]            public string Name            { get; set; } = string.Empty;
    [JsonPropertyName("category1")]       public string Category1       { get; set; } = "Services";
    [JsonPropertyName("itemType")]        public string ItemType        { get; set; } = "VIRTUAL";
    [JsonPropertyName("price")]           public string Price           { get; set; } = "0.0";

    /// <summary>
    /// Iyzico Marketplace: sub-merchant key for this basket item.
    /// Required for marketplace split payments.
    /// </summary>
    [JsonPropertyName("subMerchantKey")]
    public string? SubMerchantKey { get; set; }

    /// <summary>
    /// Amount to transfer to sub-merchant (net payout).
    /// Must be less than Price.
    /// </summary>
    [JsonPropertyName("subMerchantPrice")]
    public string? SubMerchantPrice { get; set; }
}

// ── CheckoutForm Initialize ───────────────────────────────────────────────────

public sealed class IyzicoCheckoutFormRequest : IyzicoBaseRequest
{
    [JsonPropertyName("price")]           public string Price           { get; set; } = "0.0";
    [JsonPropertyName("paidPrice")]       public string PaidPrice       { get; set; } = "0.0";
    [JsonPropertyName("currency")]        public string Currency        { get; set; } = "TRY";
    [JsonPropertyName("basketId")]        public string BasketId        { get; set; } = string.Empty;
    [JsonPropertyName("paymentGroup")]    public string PaymentGroup    { get; set; } = "PRODUCT";
    [JsonPropertyName("callbackUrl")]     public string CallbackUrl     { get; set; } = string.Empty;
    [JsonPropertyName("enabledInstallments")] public List<int> EnabledInstallments { get; set; } = [1];
    [JsonPropertyName("buyer")]           public IyzicoBuyer Buyer      { get; set; } = new();
    [JsonPropertyName("shippingAddress")] public IyzicoAddress ShippingAddress { get; set; } = new();
    [JsonPropertyName("billingAddress")]  public IyzicoAddress BillingAddress  { get; set; } = new();
    [JsonPropertyName("basketItems")]     public List<IyzicoBasketItem> BasketItems { get; set; } = [];
}

public sealed class IyzicoCheckoutFormResponse : IyzicoBaseResponse
{
    /// <summary>Iyzico checkout token — used as GatewayReference and to retrieve result.</summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>HTML snippet to embed in the payment page (for hosted form flow).</summary>
    [JsonPropertyName("checkoutFormContent")]
    public string? CheckoutFormContent { get; set; }

    [JsonPropertyName("tokenExpireTime")]
    public long? TokenExpireTime { get; set; }
}

// ── CheckoutForm Retrieve ─────────────────────────────────────────────────────

public sealed class IyzicoRetrieveCheckoutRequest : IyzicoBaseRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public sealed class IyzicoRetrieveCheckoutResponse : IyzicoBaseResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string? PaymentStatus { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("paidPrice")]
    public string? PaidPrice { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("basketId")]
    public string? BasketId { get; set; }

    // BE-P9-fix §3/§6: payment id + response signature (validated against paymentStatus:paymentId:currency:basketId:...).
    [JsonPropertyName("paymentId")]
    public string? PaymentId { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("paymentItems")]
    public List<IyzicoPaymentItem>? PaymentItems { get; set; }
}

/// <summary>
/// One per-split payment item from CF-retrieve. BE-P9-fix §6: carries the item <c>paymentTransactionId</c> (used for
/// approve/refund), the sub-merchant payout + blockage, and the <c>transactionStatus</c> (1 = held/awaiting approval,
/// 2 = released, 0 = fraud review, -1 = rejected).
/// </summary>
public sealed class IyzicoPaymentItem
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("paidPrice")]
    public string? PaidPrice { get; set; }

    [JsonPropertyName("subMerchantKey")]
    public string? SubMerchantKey { get; set; }

    [JsonPropertyName("subMerchantPrice")]
    public string? SubMerchantPrice { get; set; }

    [JsonPropertyName("subMerchantPayoutAmount")]
    public decimal? SubMerchantPayoutAmount { get; set; }

    [JsonPropertyName("merchantPayoutAmount")]
    public decimal? MerchantPayoutAmount { get; set; }

    [JsonPropertyName("blockageRateAmountMerchant")]
    public decimal? BlockageRateAmountMerchant { get; set; }

    [JsonPropertyName("blockageRateAmountSubMerchant")]
    public decimal? BlockageRateAmountSubMerchant { get; set; }

    /// <summary>1 = held/awaiting marketplace approval, 2 = released, 0 = fraud review, -1 = rejected.</summary>
    [JsonPropertyName("transactionStatus")]
    public int? TransactionStatus { get; set; }
}

// ── Marketplace Approval ──────────────────────────────────────────────────────

public sealed class IyzicoApprovalRequest : IyzicoBaseRequest
{
    /// <summary>Iyzico paymentTransactionId from the payment item — NOT our internal ID.</summary>
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;
}

public sealed class IyzicoApprovalResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }
}

// ── SubMerchant ───────────────────────────────────────────────────────────────

/// <summary>
/// BE-P9-fix §5 — sub-merchant create. The body is <c>subMerchantType</c>-discriminated: only the fields required for the
/// chosen type are serialized (nulls are omitted), and <see cref="IyzicoSubMerchantRequestBuilder"/> validates them + fails
/// loud on a missing required field. No hardcoded identity/tax number.
/// </summary>
public sealed class IyzicoSubMerchantRequest : IyzicoBaseRequest
{
    [JsonPropertyName("subMerchantExternalId")]
    public string SubMerchantExternalId { get; set; } = string.Empty;

    [JsonPropertyName("subMerchantType")]
    public string SubMerchantType { get; set; } = "PRIVATE_COMPANY";  // PERSONAL | PRIVATE_COMPANY | LIMITED_OR_JOINT_STOCK_COMPANY

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("gsmNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GsmNumber { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("contactName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactName { get; set; }

    [JsonPropertyName("contactSurname"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContactSurname { get; set; }

    [JsonPropertyName("identityNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdentityNumber { get; set; }

    [JsonPropertyName("taxOffice"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("legalCompanyTitle"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegalCompanyTitle { get; set; }

    [JsonPropertyName("iban"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Iban { get; set; }   // optional at create, required before product approval (→ split-eligibility)

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
}

/// <summary>BE-P9-fix §5 — sub-merchant update: PUT /onboarding/submerchant, NO subMerchantType; subMerchantKey + iban.</summary>
public sealed class IyzicoUpdateSubMerchantRequest : IyzicoBaseRequest
{
    [JsonPropertyName("subMerchantKey")]
    public string SubMerchantKey { get; set; } = string.Empty;

    [JsonPropertyName("iban")]
    public string Iban { get; set; } = string.Empty;

    [JsonPropertyName("address"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address { get; set; }

    [JsonPropertyName("email"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    [JsonPropertyName("gsmNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GsmNumber { get; set; }

    [JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("identityNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdentityNumber { get; set; }

    [JsonPropertyName("taxOffice"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("legalCompanyTitle"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegalCompanyTitle { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
}

/// <summary>BE-P9-fix §5 — sub-merchant detail lookup by external id.</summary>
public sealed class IyzicoSubMerchantDetailRequest : IyzicoBaseRequest
{
    [JsonPropertyName("subMerchantExternalId")]
    public string SubMerchantExternalId { get; set; } = string.Empty;
}

public sealed class IyzicoSubMerchantDetailResponse : IyzicoBaseResponse
{
    [JsonPropertyName("subMerchantKey")]  public string? SubMerchantKey  { get; set; }
    [JsonPropertyName("subMerchantType")] public string? SubMerchantType { get; set; }
    [JsonPropertyName("iban")]            public string? Iban            { get; set; }
    [JsonPropertyName("name")]            public string? Name            { get; set; }
    [JsonPropertyName("email")]           public string? Email           { get; set; }
}

public sealed class IyzicoSubMerchantResponse : IyzicoBaseResponse
{
    [JsonPropertyName("subMerchantKey")]
    public string? SubMerchantKey { get; set; }

    [JsonPropertyName("subMerchantExternalId")]
    public string? SubMerchantExternalId { get; set; }
}

// ── Refund ────────────────────────────────────────────────────────────────────

public sealed class IyzicoRefundRequest : IyzicoBaseRequest
{
    // BE-P9-fix §8: item-level refund — paymentTransactionId is the CF-retrieve item id (NOT the payment-level paymentId).
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0.0";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    /// <summary>OTHER | FRAUD | BUYER_REQUEST | DOUBLE_PAYMENT (iyzico enum).</summary>
    [JsonPropertyName("reason"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = "85.34.78.112";
}

public sealed class IyzicoRefundResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [JsonPropertyName("paymentId")]
    public string? PaymentId { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    [JsonPropertyName("retryable")]
    public bool? Retryable { get; set; }
}

/// <summary>BE-P9-fix §8 — allowed iyzico refund reasons.</summary>
public static class IyzicoRefundReason
{
    public const string Other         = "OTHER";
    public const string Fraud         = "FRAUD";
    public const string BuyerRequest  = "BUYER_REQUEST";
    public const string DoublePayment = "DOUBLE_PAYMENT";
}

// ── BE-P9 item-level marketplace operations (§10, §21) ──────────────────────

/// <summary>POST /payment/iyzipos/item/approve — approve a single basket item's sub-merchant split (partial/native).</summary>
public sealed class IyzicoItemApproveRequest : IyzicoBaseRequest
{
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;
}

public sealed class IyzicoItemApproveResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }
}

/// <summary>POST /payment/iyzipos/item/disapprove — disapprove a single basket item's sub-merchant split.</summary>
public sealed class IyzicoItemDisapproveRequest : IyzicoBaseRequest
{
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;
}

public sealed class IyzicoItemDisapproveResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }
}

/// <summary>PUT /payment/item — update a sub-merchant's share on a basket item (change-order / partial per §20.13/§21).</summary>
public sealed class IyzicoUpdateItemRequest : IyzicoBaseRequest
{
    [JsonPropertyName("paymentTransactionId")]
    public string  PaymentTransactionId { get; set; } = string.Empty;

    [JsonPropertyName("subMerchantKey")]
    public string  SubMerchantKey       { get; set; } = string.Empty;

    [JsonPropertyName("subMerchantPrice")]
    public string  SubMerchantPrice     { get; set; } = "0.0";
}

public sealed class IyzicoUpdateItemResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }
}
