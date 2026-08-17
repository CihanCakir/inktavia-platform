using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayoutReceiptUrl;

public sealed class GetProviderPayoutReceiptUrlQueryHandler
    : AizenQueryHandler<GetProviderPayoutReceiptUrlQuery, ProviderFilePdfUrlDto>
{
    private readonly IPayoutRecordRepository _payoutRepo;
    private readonly IPayoutReceiptPdfService _receiptService;
    private readonly IPaymentFileStorageRemoteCall _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public GetProviderPayoutReceiptUrlQueryHandler(
        IPayoutRecordRepository payoutRepo,
        IPayoutReceiptPdfService receiptService,
        IPaymentFileStorageRemoteCall fileStorage,
        IAizenInfoAccessor info)
    {
        _payoutRepo     = payoutRepo;
        _receiptService = receiptService;
        _fileStorage    = fileStorage;
        _info           = info;
    }

    public override async Task<ProviderFilePdfUrlDto?> Handle(
        GetProviderPayoutReceiptUrlQuery request, CancellationToken ct)
    {
        var payout = await _payoutRepo.GetByIdAsync(request.PayoutId, ct);
        if (payout is null || payout.ProviderProfileId != request.ProviderProfileId)
            return null; // 404

        if (payout.Status != PayoutStatus.Completed)
            throw new AizenBusinessException("RECEIPT_NOT_READY");

        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        var fileIdStr = await _receiptService.EnsureReceiptAsync(request.PayoutId, accessToken, ct);

        if (!Guid.TryParse(fileIdStr, out var fileId))
            return null;

        const int expiresInSeconds = 300;
        var readUrlEnvelope = await _fileStorage.CreateReadUrl(
            fileId,
            new CreateReadUrlRequest { ExpiresIn = TimeSpan.FromSeconds(expiresInSeconds) },
            $"Bearer {accessToken}");

        var readUrlBody = readUrlEnvelope.Body;
        if (readUrlBody is null)
            return null;

        return new ProviderFilePdfUrlDto
        {
            Url              = readUrlBody.ReadUrl,
            ExpiresInSeconds = expiresInSeconds,
        };
    }
}
