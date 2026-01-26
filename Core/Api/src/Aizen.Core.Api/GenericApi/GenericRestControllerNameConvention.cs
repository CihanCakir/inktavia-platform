using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Aizen.Core.Infrastructure.Api.GenericApi;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class GenericRestControllerNameConvention : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() !=
            typeof(AizenGenericApi<>))
        {
            return;
        }

        var entityType = controller.ControllerType.GenericTypeArguments[0];
        controller.ControllerName = entityType.Name;
        controller.RouteValues["Controller"] = entityType.Name;
    }
}