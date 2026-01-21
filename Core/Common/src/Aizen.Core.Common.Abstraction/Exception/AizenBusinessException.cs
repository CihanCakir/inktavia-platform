using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Infrastructure.Exception;

public class AizenBusinessException : AizenException
{

    public AizenBusinessException(int errorCode) : base(errorCode)
    {
    }
    public AizenBusinessException(int errorCode, string message) : base(errorCode, message)
    {
    }
    public AizenBusinessException(string message) : base(message)
    {
    }
}