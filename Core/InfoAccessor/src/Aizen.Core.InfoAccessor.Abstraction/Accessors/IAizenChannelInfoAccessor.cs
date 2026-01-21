namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenChannelInfoAccessor
{
    public AizenChannelInfo ChannelInfo { get; }
}

public class AizenChannelInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;
}