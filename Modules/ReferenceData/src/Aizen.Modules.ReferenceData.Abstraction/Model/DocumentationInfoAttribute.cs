namespace Aizen.Modules.ReferenceData.Abstraction.Model;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class DocumentationInfoAttribute : Attribute
{
    public string Summary { get; }
    public string? Remarks { get; }

    public DocumentationInfoAttribute(string summary, string? remarks = null)
    {
        Summary = summary;
        Remarks = remarks;
    }
}
