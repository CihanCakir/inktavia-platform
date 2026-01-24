using Aizen.Core.InfoAccessor.Abstraction;

namespace Aizen.Core.Starter.Operation.Controllers;

public class DynamicFormModel
{
    public List<string> Events { get; set; }
    
    public string? SelectedEvent { get; set; }
    
    public List<FormField> FormFields { get; set; }
    
    public string Result { get; set; }
    public AizenAppInfo AppInfo { get; set; }
}

public class FormField
{
    public string Name { get; set; }
    
    public string Type { get; set; }
    
    public object Value { get; set; }

    public List<FormField> NestedFields { get; set; }
}