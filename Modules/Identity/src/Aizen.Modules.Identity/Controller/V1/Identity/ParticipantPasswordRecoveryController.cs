using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestParticipantPasswordRecovery;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyParticipantPasswordRecoveryOtp;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResetParticipantPassword;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendParticipantPasswordRecoveryOtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

/// <summary>
/// Participant (mobile) password recovery (Identity-owned). Service-token authorized (IdentityWrite), called by the
/// mobile participant BFF. Endpoints bind the shared HTTP <c>Request</c> DTOs and map them to internal CQRS commands —
/// command classes are never exposed as HTTP contracts. Mirrors <see cref="ProviderPasswordRecoveryController"/>.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth/participant-password-recovery")]
[Tags("Identity - Participant Password Recovery")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ParticipantPasswordRecoveryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ParticipantPasswordRecoveryController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
    {
        _sender = cqrsProcessor;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(RequestProviderPasswordRecoveryResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RequestProviderPasswordRecoveryResponse>> Request(
        [FromBody] RequestProviderPasswordRecoveryRequest request, CancellationToken ct)
    {
        var command = new RequestParticipantPasswordRecoveryCommand
        {
            Channel = request.Channel,
            Identifier = request.Identifier,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(VerifyProviderPasswordRecoveryOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VerifyProviderPasswordRecoveryOtpResponse>> VerifyOtp(
        [FromBody] VerifyProviderPasswordRecoveryOtpRequest request, CancellationToken ct)
    {
        var command = new VerifyParticipantPasswordRecoveryOtpCommand
        {
            ResetRequestId = request.ResetRequestId,
            OtpCode = request.OtpCode,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetProviderPasswordResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResetProviderPasswordResponse>> Reset(
        [FromBody] ResetProviderPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetParticipantPasswordCommand
        {
            ResetToken = request.ResetToken,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ResendProviderPasswordRecoveryOtpResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ResendProviderPasswordRecoveryOtpResponse>> ResendOtp(
        [FromBody] ResendProviderPasswordRecoveryOtpRequest request, CancellationToken ct)
    {
        var command = new ResendParticipantPasswordRecoveryOtpCommand
        {
            ResetRequestId = request.ResetRequestId,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
