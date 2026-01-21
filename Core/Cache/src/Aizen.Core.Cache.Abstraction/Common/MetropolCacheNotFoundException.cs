using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Cache.Abstraction.Common;

public class AizenCacheNotFoundException : AizenException
{
    public AizenCacheNotFoundException(string cacheKey) : base($"Cache key {cacheKey} not found")
    {
    }
}