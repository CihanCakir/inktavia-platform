using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.InfoAccessor;

/// <summary>
/// Process-wide single source of <see cref="AizenServerInfo"/>.
///
/// Debt #111: <see cref="AizenInfoContainer"/> is registered SCOPED, and its constructor used to run
/// <c>new AizenServerInfo()</c> on every scope (i.e. every HTTP request). That constructor probes the
/// host — on Linux it shelled out ~6 processes (cpuinfo/meminfo/lspci/xdpyinfo/dmidecode/df), most of
/// which only printed "command not found" (log flood) and burned CPU (CFS throttling).
///
/// The host does not change during a process's lifetime, so the probes run EXACTLY ONCE here, guarded
/// by a <see cref="Lazy{T}"/> in <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> mode so a
/// concurrent cold start still constructs a single instance. Registered as a DI singleton; the scoped
/// container just reads <see cref="ServerInfo"/> (the same reference every request).
/// </summary>
internal sealed class AizenServerInfoProvider
{
    private readonly Lazy<AizenServerInfo> _serverInfo =
        new(() => new AizenServerInfo(), LazyThreadSafetyMode.ExecutionAndPublication);

    public AizenServerInfo ServerInfo => _serverInfo.Value;
}
