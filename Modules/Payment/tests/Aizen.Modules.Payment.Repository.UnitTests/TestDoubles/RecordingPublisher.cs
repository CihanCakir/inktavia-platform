using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;

namespace Aizen.Modules.Payment.Repository.UnitTests;

/// <summary>
/// Hand-rolled test double for <see cref="IAizenMessagePublisher"/> (the repo has no mocking framework). Records every
/// published message so tests can assert what was emitted (e.g. N3-B PaymentChargebackRecordedMessage).
/// </summary>
public sealed class RecordingPublisher : IAizenMessagePublisher
{
    public List<AizenBaseMessage> Published { get; } = new();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task PublishRollbackAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage => Task.CompletedTask;

    public Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage
        where TResponse : class => Task.FromResult<TResponse>(null!);
}
