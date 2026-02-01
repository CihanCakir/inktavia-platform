using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Dto
{
    public class TransferStatusDto
    {
        public required string ReferenceId { get; set; }
        public TransferStatusEnum Status { get; set; }
        public string? Message { get; set; }
    }
}