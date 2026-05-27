namespace Aizen.Modules.Payment.Abstraction.Request
{
    public sealed record ProviderWebhookInput(
        string ProviderReference,           // Iyzico: checkoutToken (controller’dan form/body’den çekip ver)
        string RawBody,                     // log/kanıt amaçlı ham payload (opsiyonel ama faydalı)
        string? Signature,                  // Iyzico imza header’ı (varsa)
        IReadOnlyDictionary<string, string>? Headers = null,
        DateTime? PaidAtUtc = null          // provider ödeme zamanı bilmiyorsak null bırak (UTC now kullanırız)
    );
}