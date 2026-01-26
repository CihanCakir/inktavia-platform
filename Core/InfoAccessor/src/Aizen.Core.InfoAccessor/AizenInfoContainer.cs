using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.InfoAccessor;

internal class AizenInfoContainer : IAizenInfoContainer
{
    private readonly AizenInfoContainerForSigleton _containerForSigleton;

    private readonly AizenInfoContainerForScoped _containerForScoped;

    public AizenInfoContainer(AizenInfoContainerForSigleton containerForSigleton,
        AizenInfoContainerForScoped containerForScoped
    )
    {
        _containerForSigleton = containerForSigleton;
        _containerForScoped = containerForScoped;
        
        Set(new AizenServerInfo());
        Set(new AizenNetworkInfo());
    }

    public TAizenInfo Get<TAizenInfo>() where TAizenInfo : IAizenInfo
    {
        if (_containerForScoped.TryGetValue<TAizenInfo>(out var result))
        {
            return result;
        }

        if (_containerForSigleton.TryGetValue<TAizenInfo>(out result))
        {
            return result;
        }

        return default;
    }

    public void Set<TAizenInfo>(TAizenInfo info) where TAizenInfo : IAizenInfo
    {
        switch (info.LifeCycle)
        {
            case InfoLifeCycle.Singleton:
                _containerForSigleton.Set(info);
                break;
            case InfoLifeCycle.Scoped:
                _containerForScoped.Set(info);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

  
}