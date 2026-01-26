using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Core.CQRS.Message
{
    public class AizenCommandResult
    {
        public AizenCommandResult(bool isSuccess)
        {
            this.IsSuccess = isSuccess;
        }
        public AizenCommandResult(bool isSuccess,
            string errorMessage)
        {
            this.IsSuccess = isSuccess;
            this.ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }
    }
    
    
}