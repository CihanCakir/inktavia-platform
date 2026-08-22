using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.Common.Abstraction.ViewModel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Onboarding;

public sealed class AttachOnboardingDocumentCommandHandler
    : AizenCommandHandler<AttachOnboardingDocumentCommand, AttachDocumentBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<AttachOnboardingDocumentCommandHandler> _logger;

    public AttachOnboardingDocumentCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IIdentityRemoteCall identity,
        ILogger<AttachOnboardingDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _identity = identity;
        _logger = logger;
    }

    // FAZ13A #67: başarısızlık 200'de gizlenmiyor. Attach için Identity'nin kararlı belge-doğrulama kodları
    // (4307-4318: sahiplik, karantina, tip, boyut...) Refit zarfından AYNEN yukarı taşınıyor → frontend eşleyebilir.
    public override async Task<AttachDocumentBffResponse?> Handle(AttachOnboardingDocumentCommand request, CancellationToken ct)
    {
        // Resolve identity first → populates IProviderIdentityHolder → assertion headers on the module call.
        // Without this the holder stays empty, the module call carries no asserted user, and Identity sees
        // callerUserId = 0 → "You do not have permission to modify this profile."
        var resolution = await _resolver.ResolveAsync(ct);
        var profileId = resolution.ProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException((int)AizenErrorCode.ProviderProfileNotFound, "Provider profile not found.");

        // Fail closed: a missing user id is a bug, not a caller we can silently treat as user 0.
        var userId = _identityHolder.UserId ?? 0;
        if (userId <= 0)
        {
            _logger.LogError("Provider identity unresolved for profile {ProfileId}; refusing to attach document.", profileId);
            throw new AizenBusinessException((int)AizenErrorCode.ProviderOnboardingCallerIdentityInvalid, "Provider identity could not be resolved.");
        }

        AttachProviderDocumentResponse? data;
        try
        {
            var result = await _identity.AttachProviderDocument(profileId, new AttachProviderDocumentRequest
            {
                FileId = request.FileId,
                DocumentType = request.DocumentType,
                Issuer = request.Issuer,
                UserId = userId,
            });
            data = result.Body;
        }
        catch (Refit.ApiException ex)
        {
            // Identity maps AizenBusinessException → HTTP 400 + Aizen envelope. Kuralı ("dosya sana ait değil",
            // "zaten ekli", "karantinada", "tip/boyut"...) kodu + mesajıyla aynen yukarı taşı — 200'de kaybolmasın.
            _logger.LogWarning(ex, "Attach rejected for document {FileId}, profile {ProfileId}.", request.FileId, profileId);
            throw ModuleFailurePropagation.FromApiException(ex, "An error occurred while attaching the document.");
        }

        if (data is null || !data.Success)
            throw new AizenBusinessException("Failed to attach document.");

        return new AttachDocumentBffResponse
        {
            Success = true,
            Message = "Document attached successfully.",
            Document = data.Document,
        };
    }
}
