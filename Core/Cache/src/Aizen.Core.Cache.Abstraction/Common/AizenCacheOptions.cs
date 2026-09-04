namespace Aizen.Core.Cache.Abstraction.Common;

public class AizenCacheOptions
{
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    public TimeSpan? SlidingExpiration { get; set; }

    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
}