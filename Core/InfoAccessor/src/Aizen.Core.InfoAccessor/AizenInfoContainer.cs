using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.InfoAccessor;

internal class AizenInfoContainer : IAizenInfoContainer
{
    private readonly AizenInfoContainerForSigleton _containerForSigleton;

    private readonly AizenInfoContainerForScoped _containerForScoped;

    private readonly AizenServerInfoProvider _serverInfoProvider;

    public AizenInfoContainer(AizenInfoContainerForSigleton containerForSigleton,
        AizenInfoContainerForScoped containerForScoped,
        AizenServerInfoProvider serverInfoProvider
    )
    {
        _containerForSigleton = containerForSigleton;
        _containerForScoped = containerForScoped;
        _serverInfoProvider = serverInfoProvider;

        // #111: reuse the process-wide singleton — the host probes ran once at startup, not per scope.
        Set(_serverInfoProvider.ServerInfo);
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