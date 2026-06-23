namespace Aizen.Modules.Identity.Abstraction.Request
{
    public sealed class AddVerificationDocumentRequest
    {
        public long FileId { get; set; }
        public string Name { get; set; } = default!;
        public string DocumentType { get; set; } = default!;
        public string? Format { get; set; }
        public string? FileSizeDisplay { get; set; }
        public string? Issuer { get; set; }
    }
}
