namespace Aizen.Modules.Identity.Abstraction.Request
{
    public sealed class AddVerificationDocumentRequest
    {
        public Guid FileId { get; set; }
        public string DocumentType { get; set; } = default!;
        public string? Issuer { get; set; }
    }
}
