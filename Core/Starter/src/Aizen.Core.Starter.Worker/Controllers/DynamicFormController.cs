using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.Starter.Worker.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Aizen.Core.Starter.Worker.Controllers;

public class DynamicFormController : Controller
{
    private readonly IAizenMessagePublisher _publisher;
    private readonly IAizenInfoAccessor _infoAccessor;
    private Dictionary<string, MessageTypeContainer> _dictionary;

    public DynamicFormController(IAizenMessagePublisher publisher, IAizenInfoAccessor infoAccessor,
        IEnumerable<IAizenMessageConsumer> consumers)
    {
        _publisher = publisher;
        _infoAccessor = infoAccessor;
        _dictionary = new Dictionary<string, MessageTypeContainer>();
        foreach (var type in consumers.Select(x => x.GetType())
                     .Where(x => IsDerivedFromGenericType(x, typeof(AizenBaseMessageConsumer<>)) ||
                                 IsDerivedFromGenericType(x, typeof(AizenBaseMessageConsumer<,>))))
        {
            var messageTypes = type.BaseType.GetGenericArguments();
            _dictionary.Add($"{type.Name}-{messageTypes[0].FullName}",
                new MessageTypeContainer
                {
                    RequestType = messageTypes[0],
                    ResponseType = messageTypes.Length == 2 ? messageTypes[1] : null
                });
        }
    }

    public IActionResult Index()
    {
        var model = new DynamicFormModel
        {
            AppInfo = _infoAccessor.AppInfoAccessor.AppInfo,
            Events = _dictionary.Keys.ToList(),
            FormFields = new List<FormField>()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromForm] Dictionary<string, string> formData)
    {
        var selectedEvent = formData["selectedEvent"];
        if (string.IsNullOrEmpty(selectedEvent))
        {
            return View("Index", new DynamicFormModel
            {
                AppInfo = _infoAccessor.AppInfoAccessor.AppInfo,
                Events = _dictionary.Keys.ToList(),
                SelectedEvent = selectedEvent,
                FormFields = new List<FormField>()
            });
        }

        var selectedType = _dictionary[selectedEvent];
        var message = Activator.CreateInstance(selectedType.RequestType);
        FormDataBinder.BindFormData(formData, message);

        var genericSendAsyncMethod = selectedType.ResponseType == null
            ? _publisher.GetType().GetMethod("PublishAsync").MakeGenericMethod(selectedType.RequestType)
            : _publisher.GetType().GetMethod("SendAsync")
                .MakeGenericMethod(selectedType.RequestType, selectedType.ResponseType);
        var resultTask =
            (Task) genericSendAsyncMethod.Invoke(_publisher, new object[] {message, CancellationToken.None});
        await resultTask;
        var resultProperty = resultTask.GetType().GetProperty("Result");
        var response = resultProperty.GetValue(resultTask);

        return View("Index", new DynamicFormModel
        {
            AppInfo = _infoAccessor.AppInfoAccessor.AppInfo,
            Events = _dictionary.Keys.ToList(),
            SelectedEvent = selectedEvent,
            Result = JObject.FromObject(response).ToString(),
            FormFields = FormFieldGenerator.GetFormFields(selectedType.RequestType)
        });
    }

    public IActionResult ReRender([FromQuery] string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            return PartialView("_DynamicForm", new DynamicFormModel
            {
                AppInfo = _infoAccessor.AppInfoAccessor.AppInfo,
                Events = _dictionary.Keys.ToList(),
                SelectedEvent = eventType,
                FormFields = new List<FormField>()
            });
        }

        var selectedType = _dictionary[eventType];
        return PartialView("_DynamicForm", new DynamicFormModel
        {
            AppInfo = _infoAccessor.AppInfoAccessor.AppInfo,
            Events = _dictionary.Keys.ToList(),
            SelectedEvent = eventType,
            FormFields = FormFieldGenerator.GetFormFields(selectedType.RequestType)
        });
    }

    bool IsDerivedFromGenericType(Type derivedType, Type genericType)
    {
        while (derivedType != null && derivedType != typeof(object))
        {
            var currentType = derivedType.IsGenericType ? derivedType.GetGenericTypeDefinition() : derivedType;
            if (genericType == currentType)
            {
                return true;
            }

            derivedType = derivedType.BaseType;
        }

        return false;
    }
}

public class MessageTypeContainer
{
    public Type RequestType { get; set; }

    public Type ResponseType { get; set; }
}