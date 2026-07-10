using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.PasswordRecovery;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.RequestProviderPasswordRecovery;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.VerifyProviderPasswordRecoveryOtp;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResetProviderPassword;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.PasswordRecovery.ResendProviderPasswordRecoveryOtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity;

/// <summary>
/// Provider password recovery (Identity-owned). Service-token authorized (IdentityWrite), called by the
/// MarineProvider BFF. Endpoints bind HTTP <c>Request</c> DTOs and map them to internal CQRS commands — command
/// classes are never exposed as HTTP contracts.
/// </summary>
[ApiController]
[Route("api/v1/identity/auth/provider-password-recovery")]
[Tags("Identity - Provider Password Recovery")]
[Authorize(Policy = "IdentityWrite")]
public sealed class ProviderPasswordRecoveryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _sender;

    public ProviderPasswordRecoveryController(
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
        var command = new RequestProviderPasswordRecoveryCommand
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
        var command = new VerifyProviderPasswordRecoveryOtpCommand
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
        var command = new ResetProviderPasswordCommand
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
        var command = new ResendProviderPasswordRecoveryOtpCommand
        {
            ResetRequestId = request.ResetRequestId,
        };

        var result = await _sender.ProcessAsync(command, ct);
        return SetResponse(result);
    }
}
