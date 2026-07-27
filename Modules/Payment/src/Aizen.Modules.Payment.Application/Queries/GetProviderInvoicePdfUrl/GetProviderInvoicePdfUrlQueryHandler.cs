using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderInvoicePdfUrl;

public sealed class GetProviderInvoicePdfUrlQueryHandler
    : AizenQueryHandler<GetProviderInvoicePdfUrlQuery, ProviderFilePdfUrlDto>
{
    private static readonly InvoiceType[] ProviderTypes =
    {
        InvoiceType.CommissionInvoice,
        InvoiceType.SubscriptionInvoice,
        InvoiceType.ProviderSettlementStatement,
    };

    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IPaymentInvoicePdfService _pdfService;
    private readonly IPaymentFileStorageRemoteCall _fileStorage;
    private readonly IAizenInfoAccessor _info;

    public GetProviderInvoicePdfUrlQueryHandler(
        IInvoiceRepository invoiceRepo,
        IPaymentInvoicePdfService pdfService,
        IPaymentFileStorageRemoteCall fileStorage,
        IAizenInfoAccessor info)
    {
        _invoiceRepo = invoiceRepo;
        _pdfService  = pdfService;
        _fileStorage = fileStorage;
        _info        = info;
    }

    public override async Task<ProviderFilePdfUrlDto?> Handle(
        GetProviderInvoicePdfUrlQuery request, CancellationToken ct)
    {
        var invoice = await _invoiceRepo.GetByIdFullAsync(request.InvoiceId, ct);
        if (invoice is null
            || invoice.BuyerUserId != request.ProviderProfileId
            || !ProviderTypes.Contains(invoice.InvoiceType))
            return null;

        var accessToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        var fileIdStr   = await _pdfService.EnsurePdfAsync(request.InvoiceId, accessToken, ct);

        if (!Guid.TryParse(fileIdStr, out var fileId))
            return null;

        const int expiresInSeconds = 300; // 5 minutes
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
