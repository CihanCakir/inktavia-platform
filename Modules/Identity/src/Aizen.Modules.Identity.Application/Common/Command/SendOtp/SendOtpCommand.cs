using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.SendOtp
{
  public class SendOtpCommand : AizenCommand<SendOtpDto>
    { 
        public string PhoneNumber { get; set; }

        public SendOtpCommand(string phoneNumber)
        {
            PhoneNumber = phoneNumber;
        }
    }
}