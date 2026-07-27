using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

public interface IPaymentInvoicePdfService
{
    Task<string> EnsurePdfAsync(long invoiceId, string accessToken, CancellationToken ct = default);
}

public sealed class PaymentInvoicePdfService : IPaymentInvoicePdfService
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IInvoicePdfRenderer _renderer;
    private readonly IPaymentFileStorageRemoteCall _fileStorage;
    private readonly ILogger<PaymentInvoicePdfService> _logger;

    public PaymentInvoicePdfService(
        IInvoiceRepository invoiceRepo,
        IInvoicePdfRenderer renderer,
        IPaymentFileStorageRemoteCall fileStorage,
        ILogger<PaymentInvoicePdfService> logger)
    {
        _invoiceRepo = invoiceRepo;
        _renderer    = renderer;
        _fileStorage = fileStorage;
        _logger      = logger;
    }

    public async Task<string> EnsurePdfAsync(long invoiceId, string accessToken, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdFullAsync(invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (!string.IsNullOrEmpty(invoice.PdfFileRef))
            return invoice.PdfFileRef;

        var pdfBytes = _renderer.Render(invoice);
        var fileName = $"invoice-{invoice.InvoiceNumber ?? invoice.Id.ToString()}.pdf";
        var bearer   = $"Bearer {accessToken}";

        // Create upload session with ServerSideUpload=true so FileStorage signs the presigned URL
        // with the internal S3 endpoint (http://minio:9000), not the browser-facing PublicServiceUrl.
        var sessionResponse = await _fileStorage.CreateUploadSession(
            new CreateUploadSessionRequest
            {
                OriginalFileName = fileName,
                ContentType      = "application/pdf",
                SizeInBytes      = pdfBytes.Length,
                Category         = FileCategory.Document,
                Visibility       = FileVisibility.Private,
                OwnerModule      = "Payment",
                OwnerEntityType  = "Invoice",
                ServerSideUpload = true,
            }, bearer);

        var session = sessionResponse.Body
            ?? throw new InvalidOperationException($"FileStorage CreateUploadSession returned null body for invoice {invoiceId}.");

        _logger.LogInformation(
            "Invoice {Id} upload session created: FileId={FileId}, ObjectKey={ObjectKey}, UploadUrl={UploadUrl}, SessionCode={Code}",
            invoiceId, session.FileId, session.ObjectKey, session.UploadUrl, session.UploadSessionCode);

        var uploadUrl = session.UploadUrl;
        if (!Uri.IsWellFormedUriString(uploadUrl, UriKind.Absolute))
            throw new InvalidOperationException(
                $"Presigned upload URL is not absolute: '{uploadUrl}'. Check FileStorage S3 config.");

        // PUT the PDF bytes to the presigned URL UNCHANGED (signed for internal endpoint, SigV4 intact)
        using var http = new HttpClient();
        using var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        var putResponse = await http.PutAsync(new Uri(uploadUrl), content, ct);
        putResponse.EnsureSuccessStatusCode();

        // Complete upload session
        await _fileStorage.CompleteUploadSession(
            session.UploadSessionCode,
            new CompleteUploadSessionRequest(),
            bearer);

        // Persist the file reference
        var fileId = session.FileId.ToString();
        invoice.SetPdfRef(fileId);
        _invoiceRepo.Update(invoice);
        await _invoiceRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Invoice {Id} PDF generated and stored as fileId={FileId}.", invoiceId, fileId);
        return fileId;
    }
}
