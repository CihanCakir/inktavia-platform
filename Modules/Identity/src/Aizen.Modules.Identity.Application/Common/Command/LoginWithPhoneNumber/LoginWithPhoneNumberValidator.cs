using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class LoginWithPhoneNumberValidator : AizenValidator<LoginWithPhoneNumberCommand>
    {
        public LoginWithPhoneNumberValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("PhoneNumber cannot be empty")
                .Matches(@"^\+90\d{10}$").WithMessage("PhoneNumber must start with +90 and be 13 digits");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            RuleFor(x => x.DeviceId)
                .NotEmpty().WithMessage("DeviceId is required");

            RuleFor(x => x.NotificationToken)
                .NotNull(); // boş olabilir ama null olmasın
        }
    }
}