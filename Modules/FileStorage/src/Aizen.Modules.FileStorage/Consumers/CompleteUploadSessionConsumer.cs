using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Message;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.FileStorage.Application.Commands.CompleteUploadSession;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.FileStorage.Consumers;

[DocumentationInfo("Complete upload session consumer", "Request/response consumer that finalizes an S3 upload session via CQRS.")]
public sealed class CompleteUploadSessionConsumer
    : AizenBaseMessageConsumer<CompleteUploadSessionProcessMessage, CompleteUploadSessionProcessMessageResult>
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public CompleteUploadSessionConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = serviceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CompleteUploadSessionProcessMessage message, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrWhiteSpace(message.UploadSessionCode));

    public override async Task<CompleteUploadSessionProcessMessageResult> ExecuteCommitMessage(
        CompleteUploadSessionProcessMessage message, CancellationToken cancellationToken)
    {
        var result = await _cqrsProcessor.ProcessAsync<FileDto>(
            new CompleteUploadSessionCommand
            {
                UploadSessionCode = message.UploadSessionCode,
                Request = new CompleteUploadSessionRequest { Checksum = message.Checksum }
            }, cancellationToken);

        return new CompleteUploadSessionProcessMessageResult
        {
            FileId = result?.FileId ?? Guid.Empty,
            Status = result?.Status ?? default
        };
    }

    public override Task ExecuteRollbackMessage(
        CompleteUploadSessionProcessMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
