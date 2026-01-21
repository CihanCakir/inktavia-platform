namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenRequestInfoAccessor
{
    public AizenRequestInfo RequestInfo { get; }
}

public class AizenRequestInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;
}