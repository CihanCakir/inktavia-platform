namespace Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;

public sealed class AddWorkLogEntryRequest
{
    public string Type { get; set; } = "note";
    public string Content { get; set; } = string.Empty;
    public string? MediaFileId { get; set; }
    public string Author { get; set; } = string.Empty;
    public long? AuthorId { get; set; }
}
