using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Security;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.SubmitProviderPaymentProfile;

/// <summary>
/// Provider self-service KYC submit. Captures the full iyzico KYC (IBAN + legal + the encrypted KYC blob), advances the
/// profile to DataSubmitted, and ENQUEUES async provisioning (no inline gateway call in the request path). Idempotent
/// re-submit updates the captured data and re-enqueues.
/// </summary>
public sealed class SubmitProviderPaymentProfileCommand : AizenCommand<ProviderPaymentProfileDto>
{
    public long    ProviderProfileId { get; init; }
    public string  Iban              { get; init; } = default!;
    public string? LegalName         { get; init; }
    public string? TaxNumber         { get; init; }
    public string? SubMerchantType   { get; init; }
    public string? Email             { get; init; }
    public string? TaxOffice         { get; init; }
    public string? GsmNumber         { get; init; }
    public string? ContactName       { get; init; }
    public string? ContactSurname    { get; init; }
    public string? IdentityNumber    { get; init; }
}

[DocumentationInfo("Submit provider payment profile (KYC) + enqueue provisioning",
    "Captures KYC, persists DataSubmitted with an encrypted KYC blob, and publishes ProviderSubMerchantProvisioningRequested. Does NOT call the gateway inline.")]
public sealed class SubmitProviderPaymentProfileCommandHandler
    : AizenCommandHandler<SubmitProviderPaymentProfileCommand, ProviderPaymentProfileDto>
{
    /// <summary>
    /// NOT transactional: this handler must COMMIT the DataSubmitted state BEFORE it publishes the provisioning message,
    /// otherwise the consumer could run against an uncommitted profile. With IsTransactional=false the explicit
    /// SaveChanges commits immediately, then we publish (fire-and-forget). At-least-once + the consumer's idempotency +
    /// the hourly sweep make the enqueue self-healing even if the publish is lost.
    /// </summary>
    public override bool IsTransactional => false;

    private readonly IProviderPaymentProfileRepository _repo;
    private readonly IAizenMessagePublisher            _publisher;
    private readonly string                            _encryptionKey;
    private readonly ILogger<SubmitProviderPaymentProfileCommandHandler> _logger;

    public SubmitProviderPaymentProfileCommandHandler(
        IProviderPaymentProfileRepository repo,
        IAizenMessagePublisher            publisher,
        IConfiguration                    configuration,
        ILogger<SubmitProviderPaymentProfileCommandHandler> logger)
    {
        _repo          = repo;
        _publisher     = publisher;
        _encryptionKey = configuration["Payment:IbanEncryptionKey"] ?? "default-dev-key-change-in-prod!!";
        _logger        = logger;
    }

    public override async Task<ProviderPaymentProfileDto?> Handle(
        SubmitProviderPaymentProfileCommand request, CancellationToken ct)
    {
        var iban = request.Iban?.Trim().ToUpperInvariant().Replace(" ", "") ?? "";
        if (iban.Length != 26 || !iban.StartsWith("TR") || !iban[2..].All(char.IsDigit))
            throw new AizenBusinessException("Invalid IBAN format. Turkish IBAN must be 26 characters: TR + 24 digits.");

        var ibanLast4     = iban[^4..];
        var ibanEncrypted = AizenSecurityHelper.EncryptByAES(iban, _encryptionKey);
        var kycEncrypted  = new SubMerchantKycPayload(
            request.Email, request.SubMerchantType, request.TaxOffice, request.GsmNumber,
            request.ContactName, request.ContactSurname, request.IdentityNumber).Encrypt(_encryptionKey);

        var entity = await _repo.GetByProviderProfileIdAsync(request.ProviderProfileId, ct);
        if (entity is null)
        {
            entity = ProviderPaymentProfileEntity.Create(
                request.ProviderProfileId, "manual", request.LegalName, request.TaxNumber);
            entity.UpdateProfileAndResetVerification(ibanEncrypted, ibanLast4, request.LegalName, request.TaxNumber); // → DataSubmitted
            entity.SetKycPayload(kycEncrypted);
            await _repo.AddAsync(entity, ct);
        }
        else
        {
            entity.UpdateProfileAndResetVerification(ibanEncrypted, ibanLast4, request.LegalName, request.TaxNumber); // → DataSubmitted
            entity.SetKycPayload(kycEncrypted);
            _repo.Update(entity);
        }

        await _repo.SaveChangesAsync(ct);   // committed before publish (IsTransactional=false)

        _ = _publisher.PublishAsync(new ProviderSubMerchantProvisioningRequested
        {
            ProviderProfileId = entity.ProviderProfileId,
            AttemptNumber     = entity.AttemptCount,
            Source            = "submit",
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogWarning(t.Exception,
                    "Failed to publish sub-merchant provisioning for provider {Id}; the hourly sweep will retry.",
                    entity.ProviderProfileId);
        }, TaskContinuationOptions.OnlyOnFaulted);

        return ProviderPaymentProfileMapper.ToDto(entity, ibanLast4);
    }
}
