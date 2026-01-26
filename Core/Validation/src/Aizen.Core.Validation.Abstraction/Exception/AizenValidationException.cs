using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Validation.Abstraction.Exception;

public class AizenValidationException : AizenException
{
    public AizenValidationException(int errorCode) : base(errorCode)
    {
    }

    public AizenValidationException(string message) : base(message)
    {
    }
}