namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenInfo
{
    public InfoLifeCycle LifeCycle { get; }
}

public enum InfoLifeCycle
{
    Singleton,
    Scoped
}