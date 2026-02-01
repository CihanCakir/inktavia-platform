using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.CheckOtp
{
    public class CheckOtpCommand: AizenCommand<CheckOtpDto>
    {
        public string PhoneNumber { get; set; }
        public int Otp { get; set; }
        public string ValidationGuid { get; set; }

        public CheckOtpCommand(string phoneNumber, int otp, string validationGuid)
        {
            PhoneNumber = phoneNumber;
            Otp = otp;
            ValidationGuid = validationGuid;
        }
    }
}