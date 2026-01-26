using Aizen.Core.Cache.Abstraction.Common;
using System.Text.Json;

namespace Aizen.Core.Cache.Abstraction;

public interface IAizenDistributedCache : IAizenCache
{
    Task<T> GetNoHash<T>(string key);
    Task<dynamic> GetDirectStringToDynamic(string key, CancellationToken token = default);

    Task<bool> RemoveNoHash(string key);
    Task<bool> ExistNoHash(string key);
    Task<bool> SetNoHash<T>(string key, T value, TimeSpan ttl);
    Task<AizenStringCacheItem<T>> GetNoHashWitTtl<T>(string key);
}