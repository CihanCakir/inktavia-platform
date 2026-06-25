namespace Aizen.Bff.AdminPanel.Application.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
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
