namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenInfoContainer
{
    public TAizenInfo Get<TAizenInfo>() where TAizenInfo : IAizenInfo;
    
    public void Set<TAizenInfo>(TAizenInfo info) where TAizenInfo : IAizenInfo;
}