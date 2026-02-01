using Aizen.Core.Api.Middleware;
using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.CheckOtp
{
    public class CheckOtpCommandValidator : AizenValidator<CheckOtpCommand>
    {
        public CheckOtpCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotNull()
                .WithMessage(((int)AizenErrorCode.phoneNumberCannotBeNull).ToString())
                .NotEmpty()
                .WithMessage(((int)AizenErrorCode.phoneNumberCannotBeNull).ToString());


            RuleFor(x => x.PhoneNumber)
                      .Length(10)
                      .WithMessage(((int)AizenErrorCode.phoneNumberMustBeTenDigits).ToString());

            RuleFor(x => x.Otp)
                .NotNull()
                .WithMessage(((int)AizenErrorCode.otpCannotBeNull).ToString());

            RuleFor(x => x.Otp)
                .LessThan(999999)
                .WithMessage(((int)AizenErrorCode.otpMustBe6Digits).ToString())
                .GreaterThan(100000)
                .WithMessage(((int)AizenErrorCode.otpMustBe6Digits).ToString());

            RuleFor(x => x.ValidationGuid)
                .NotNull()
                .WithMessage(((int)AizenErrorCode.validationGuidCannotBeNull).ToString())
                .NotEmpty()
                .WithMessage(((int)AizenErrorCode.validationGuidCannotBeNull).ToString());
        }
    }
}