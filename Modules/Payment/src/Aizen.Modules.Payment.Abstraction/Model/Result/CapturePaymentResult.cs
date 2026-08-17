namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed record CapturePaymentResult(
    long   TransactionId,
    string TransactionCode,
    string GatewayReference,
    bool   WasAlreadyCaptured
);
