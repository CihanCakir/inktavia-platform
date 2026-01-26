using Aizen.Core.Infrastructure.Api.GenericApi;
using Aizen.Core.Starter.Api.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Aizen.Core.Starter.Bff.Generic;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class GenericRestControllerNameConvention : Attribute, IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() !=
            typeof(AizenGenericBffApi<>))
        {
            return;
        }

        var entityType = controller.ControllerType.GenericTypeArguments[0];
        controller.ControllerName = entityType.Name;
        controller.RouteValues["Controller"] = entityType.Name;
    }
}