using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

public interface IPayoutReceiptPdfService
{
    Task<string> EnsureReceiptAsync(long payoutId, string accessToken, CancellationToken ct = default);
}

public sealed class PayoutReceiptPdfService : IPayoutReceiptPdfService
{
    private readonly IPayoutRecordRepository _payoutRepo;
    private readonly IPayoutReceiptPdfRenderer _renderer;
    private readonly IPaymentFileStorageRemoteCall _fileStorage;
    private readonly ILogger<PayoutReceiptPdfService> _logger;

    public PayoutReceiptPdfService(
        IPayoutRecordRepository payoutRepo,
        IPayoutReceiptPdfRenderer renderer,
        IPaymentFileStorageRemoteCall fileStorage,
        ILogger<PayoutReceiptPdfService> logger)
    {
        _payoutRepo  = payoutRepo;
        _renderer    = renderer;
        _fileStorage = fileStorage;
        _logger      = logger;
    }

    public async Task<string> EnsureReceiptAsync(long payoutId, string accessToken, CancellationToken ct = default)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutId, ct)
            ?? throw new KeyNotFoundException($"Payout {payoutId} not found.");

        if (!string.IsNullOrEmpty(payout.ReceiptFileRef))
            return payout.ReceiptFileRef;

        if (payout.Status != PayoutStatus.Completed)
            throw new InvalidOperationException("RECEIPT_NOT_READY");

        var pdfBytes = _renderer.Render(payout);
        var fileName = $"payout-receipt-{payoutId}.pdf";
        var bearer   = $"Bearer {accessToken}";

        var sessionResponse = await _fileStorage.CreateUploadSession(
            new CreateUploadSessionRequest
            {
                OriginalFileName = fileName,
                ContentType      = "application/pdf",
                SizeInBytes      = pdfBytes.Length,
                Category         = FileCategory.Document,
                Visibility       = FileVisibility.Private,
                OwnerModule      = "Payment",
                OwnerEntityType  = "PayoutReceipt",
                ServerSideUpload = true,
            }, bearer);

        var session = sessionResponse.Body
            ?? throw new InvalidOperationException($"FileStorage CreateUploadSession returned null body for payout {payoutId}.");

        _logger.LogInformation(
            "Payout {Id} upload session: FileId={FileId}, UploadUrl={Url}, Code={Code}",
            payoutId, session.FileId, session.UploadUrl, session.UploadSessionCode);

        var uploadUrl = session.UploadUrl;
        if (!Uri.IsWellFormedUriString(uploadUrl, UriKind.Absolute))
            throw new InvalidOperationException($"Presigned upload URL is not absolute: '{uploadUrl}'.");

        using var http = new HttpClient();
        using var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        var putResponse = await http.PutAsync(new Uri(uploadUrl), content, ct);
        putResponse.EnsureSuccessStatusCode();

        await _fileStorage.CompleteUploadSession(
            session.UploadSessionCode,
            new CompleteUploadSessionRequest(),
            bearer);

        var fileId = session.FileId.ToString();
        payout.SetReceiptRef(fileId);
        _payoutRepo.Update(payout);
        await _payoutRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Payout {Id} receipt generated, fileId={FileId}.", payoutId, fileId);
        return fileId;
    }
}
