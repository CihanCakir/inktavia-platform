using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.InktaviaStore.Application.Identity;
using Aizen.Modules.InktaviaStore.Application.Identity.Command;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.CheckOtp;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithOtp;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithUsername;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.SendOtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    [ApiController]
    [Route("api/v1/auth")]
    [Tags("Auth")]
    public sealed class AuthController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public AuthController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrsProcessor)
            : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // ===========================
        // REFRESH
        // POST /api/v1/auth/refresh
        // ===========================
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<UserLoginResponse>> Refresh([FromBody] RefreshLoginHttpRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RefreshLoginCommand(
                    refreshToken: req.RefreshToken,
                    deviceId: req.DeviceId,
                    accessToken: req.AccessToken
                ),
                ct
            );

            return SetResponse(result);
        }

        // ===========================
        // OTP GÖNDER
        // POST /api/v1/auth/otp/send
        // ===========================
        [HttpPost("otp/send")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SendOtpDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<SendOtpDto>> SendOtp([FromBody] SendOtpRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(new SendOtpCommand(req.PhoneNumber), ct);
            return SetResponse(result);
        }

        // ===========================
        // OTP KONTROL
        // POST /api/v1/auth/otp/check
        // ===========================
        [HttpPost("otp/check")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CheckOtpDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<CheckOtpDto>> CheckOtp([FromBody] CheckOtpRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new CheckOtpCommand(req.PhoneNumber, req.Otp, req.ValidationGuid), ct);
            return SetResponse(result);
        }

        // ===========================
        // OTP İLE GİRİŞ
        // POST /api/v1/auth/login/otp
        // ===========================
        [HttpPost("login/otp")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<UserLoginResponse>> LoginWithOtp([FromBody] LoginWithOtpRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new LoginWithOtpCommand(req.PhoneNumber, req.Otp, req.ValidationGuid, req.DeviceId, req.NotificationToken), ct);
            return SetResponse(result);
        }

        // =================================
        // TELEFON + ŞİFRE İLE GİRİŞ
        // POST /api/v1/auth/login/phone
        // =================================
        [HttpPost("login/phone")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VerifyProviderOtpLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> LoginWithPhone([FromBody] LoginWithPhoneRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new LoginWithPhoneNumberCommand(req.PhoneNumber, req.Password, req.DeviceId, req.NotificationToken), ct);
            return SetResponse(result);
        }

        // =================================
        // KULLANICI ADI + PIN İLE GİRİŞ
        // POST /api/v1/auth/login/username
        // =================================
        [HttpPost("login/username")]
        [ProducesResponseType(typeof(VerifyProviderOtpLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<VerifyProviderOtpLoginResponse>> LoginWithUsername([FromBody] LoginWithUsernameRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new LoginWithUsernameCommand(req.Username, req.Pin, req.DeviceId, req.NotificationToken), ct);
            return SetResponse(result);
        }

        // ===========================
        // PAROLA DEĞİŞTİR (AUTH)
        // POST /api/v1/auth/password/change
        // ===========================
        [HttpPost("password/change")]
        [Authorize]
        [ProducesResponseType(typeof(ChangePasswordDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<ChangePasswordDto>> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new ChangePasswordCommand(req.OldPassword, req.NewPassword, req.NewPasswordComfirm), ct);
            return SetResponse(result);
        }
    }
}