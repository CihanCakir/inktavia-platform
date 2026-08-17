using System.Reflection;
using Aizen.Core.Domain;

namespace Aizen.Modules.FileStorage.Tests;

public class EntityConstructorGuardTests
{
    /// <summary>
    /// EF Core's lazy-loading proxies (Castle DynamicProxy) subclass the entity. A private
    /// parameterless constructor prevents subclassing -> throws on materialisation, but ONLY
    /// when the first row exists. This test catches it at build time.
    /// </summary>
    [Fact]
    public void All_AizenEntity_subclasses_must_have_a_non_private_parameterless_constructor()
    {
        var assemblies = new[]
        {
            typeof(Aizen.Modules.FileStorage.Domain.Entities.File.FileEntity).Assembly,
            typeof(Aizen.Modules.Identity.Domain.Entities.UserProfileEntity).Assembly,
        };

        var violations = new List<string>();

        foreach (var assembly in assemblies)
        {
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(AizenEntity).IsAssignableFrom(t));

            foreach (var type in entityTypes)
            {
                var parameterlessCtor = type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null, types: Type.EmptyTypes, modifiers: null);

                if (parameterlessCtor is null)
                {
                    violations.Add($"{type.FullName} has no parameterless constructor at all");
                }
                else if (parameterlessCtor.IsPrivate)
                {
                    violations.Add($"{type.FullName} has a PRIVATE parameterless constructor — must be protected or public for EF Core proxy materialisation");
                }
            }
        }

        violations.Should().BeEmpty(
            "EF Core's lazy-loading proxies subclass the entity via Castle DynamicProxy. " +
            "A private parameterless constructor prevents subclassing and throws at materialisation " +
            "— but only when the first row exists, making it invisible until production.");
    }
}
