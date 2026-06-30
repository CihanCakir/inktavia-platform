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

    [JsonPropertyName("paymentItems")]
    public List<IyzicoPaymentItem>? PaymentItems { get; set; }
}

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

public sealed class IyzicoSubMerchantRequest : IyzicoBaseRequest
{
    [JsonPropertyName("subMerchantExternalId")]
    public string SubMerchantExternalId { get; set; } = string.Empty;

    [JsonPropertyName("subMerchantType")]
    public string SubMerchantType { get; set; } = "PRIVATE_COMPANY";  // PERSONAL | PRIVATE_COMPANY | LIMITED_OR_JOINT_STOCK_COMPANY

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("contactSurname")]
    public string ContactSurname { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("gsmNumber")]
    public string? GsmNumber { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("iban")]
    public string Iban { get; set; } = string.Empty;

    [JsonPropertyName("identityNumber")]
    public string? IdentityNumber { get; set; }

    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    [JsonPropertyName("taxNumber")]
    public string? TaxNumber { get; set; }

    [JsonPropertyName("legalCompanyTitle")]
    public string? LegalCompanyTitle { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";
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
    [JsonPropertyName("paymentTransactionId")]
    public string PaymentTransactionId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0.0";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    [JsonPropertyName("ip")]
    public string Ip { get; set; } = "85.34.78.112";
}

public sealed class IyzicoRefundResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}
