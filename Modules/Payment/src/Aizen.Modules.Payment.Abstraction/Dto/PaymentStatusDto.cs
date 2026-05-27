namespace Aizen.Modules.Payment.Abstraction.Dto
{
    public class PaymentStatusDto
    {
        public required string ProviderReference { get; init; }
        public required bool IsPaid { get; init; }
        public string? RawStatus { get; init; }
    }
}