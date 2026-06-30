namespace Aizen.Modules.Payment.Abstraction.Model.Result;

public sealed class ProcessIyzicoWebhookResult
{
    public bool    Processed     { get; init; }
    public string? FailureReason { get; init; }
}
