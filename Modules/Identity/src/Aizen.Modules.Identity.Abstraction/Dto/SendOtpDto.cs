using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Dto
{
    public class SendOtpDto
    {
        public string? ValidationGuid { get; set; }
        public required string PhoneNumber { get; set; }
        public DateTime ExpiredDateTime { get; set; }
        public int ExpireCounter { get; set; }
    }
}