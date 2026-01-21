using System.Collections.Concurrent;
using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.InfoAccessor;

public class AizenInfoContainerForSigleton
{
    private ConcurrentDictionary<Type, object> _infoDictionary;

    public AizenInfoContainerForSigleton()
    {
        _infoDictionary = new ConcurrentDictionary<Type, object>();
    }

    public bool TryGetValue<TAizenInfo>(out TAizenInfo result) where TAizenInfo : IAizenInfo
    {
        if (_infoDictionary.TryGetValue(typeof(TAizenInfo), out var r))
        {
            result = (TAizenInfo) r;
            return true;
        }

        result = default;
        return false;
    }

    public void Set<TAizenInfo>(TAizenInfo info) where TAizenInfo : IAizenInfo
    {
        _infoDictionary.AddOrUpdate(
            typeof(TAizenInfo),
            t => info,
            (t, o) => o
        );
    }
}