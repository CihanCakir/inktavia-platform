using System.Reflection;
using System.Runtime.Loader;
using Aizen.Core.Common.Abstraction.Patterns;

namespace Aizen.Core.Common.Abstraction.Helpers;

public class AizenModuleAssemblyDiscovery: SingletonBase<AizenModuleAssemblyDiscovery>
{
    public Assembly EntryAssembly { get; }

    public IEnumerable<Assembly> AbstractionAssemblies { get; }

    public IEnumerable<Assembly> ApplicationAssemblies { get; }

    public IEnumerable<Assembly> CoreAssemblies { get; }

    public IEnumerable<Assembly> DomainAssemblies { get; }

    public IEnumerable<Assembly> RepositoryAssemblies { get; }
   
    public IEnumerable<Assembly> ModuleAssemblies { get; }

    private AizenModuleAssemblyDiscovery()
    {
        this.EntryAssembly = Assembly.GetEntryAssembly();

        var path = Path.GetDirectoryName(this.EntryAssembly.Location);
        var moduleAssemblies = Directory
            .GetFiles(path, "Aizen.*.dll", SearchOption.TopDirectoryOnly)
            .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
            .ToList();

        this.AbstractionAssemblies = moduleAssemblies.Where(x => x.FullName.Contains("Abstraction"));
        this.ApplicationAssemblies = moduleAssemblies.Where(x => x.FullName.Contains("Application"));
        this.CoreAssemblies = moduleAssemblies.Where(x => x.FullName.Contains("Core"));
        this.DomainAssemblies = moduleAssemblies.Where(x => x.FullName.Contains("Domain"));
        this.RepositoryAssemblies = moduleAssemblies.Where(x => x.FullName.Contains("Repository"));

        this.ModuleAssemblies = moduleAssemblies.Where(x => x.FullName.StartsWith("Aizen.Modules.") &&
                                                           !x.FullName.Contains("Abstraction") &&
                                                           !x.FullName.Contains("Application") &&
                                                           !x.FullName.Contains("Core") &&
                                                           !x.FullName.Contains("Domain") &&
                                                           !x.FullName.Contains("Repository"));
    }
}