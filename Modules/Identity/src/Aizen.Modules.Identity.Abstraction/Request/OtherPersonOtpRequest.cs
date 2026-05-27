using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Abstraction.Enum;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public class OtherPersonOtpRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        /// <summary>
        /// OTP gönderim bağlamı (diğer kişi girişi, login, vb.)
        /// </summary>
        public OtpRequestContextType ContextType { get; set; }
    }
}