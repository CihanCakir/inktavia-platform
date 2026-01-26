using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Infrastructure.Api;

public class AizenResponseHeader
{
    public bool IsSuccess { get; set; }

    public int ErrorCode { get; set; }

    public string ErrorMessage { get; set; }

    public string? ErrorDetails { get; set; }

    public AizenResponseHeader()
    {
    }

    public static AizenResponseHeader Success()
    {
        var header = new AizenResponseHeader();
        header.IsSuccess = true;

        return header;
    }

    public static AizenResponseHeader Fail(System.Exception ex)
    {
        var header = new AizenResponseHeader();
        header.IsSuccess = false;
        header.ErrorCode = ex is AizenException ? ((AizenException)ex).ErrorCode : 911;
        header.ErrorCode = ex is AizenException ? ((AizenException)ex).ErrorCode : 911;
        header.ErrorMessage = ex.Message;
        header.ErrorDetails = ex.StackTrace;

        return header;
    }
}

public class AizenApiResponse<T> where T : class
{
    public AizenApiResponse()
    {
    }

    public AizenApiResponse(AizenResponseHeader header, T body)
    {
        Header = header;
        Body = body;
    }

    public AizenResponseHeader Header { get; set; }

    public T Body { get; set; }
}

