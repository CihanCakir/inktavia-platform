using FluentValidation;

namespace Aizen.Modules.Messaging.Application.Command.RequestAttachmentUploadUrl;

public sealed class RequestAttachmentUploadUrlCommandValidator
    : AbstractValidator<RequestAttachmentUploadUrlCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/pdf",
        "video/mp4", "video/quicktime"
    ];

    private const long MaxImageBytes = 10L  * 1024 * 1024;  // 10 MB
    private const long MaxVideoBytes = 100L * 1024 * 1024;  // 100 MB
    private const long MaxDocBytes   = 25L  * 1024 * 1024;  // 25 MB

    public RequestAttachmentUploadUrlCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("File type not allowed. Allowed: jpg, png, webp, gif, pdf, mp4, mov.");
        RuleFor(x => x.SizeInBytes)
            .GreaterThan(0)
            .Must((cmd, size) => size <= GetMaxSize(cmd.ContentType))
            .WithMessage("File size exceeds the limit for this content type.");
    }

    private static long GetMaxSize(string contentType) => contentType switch
    {
        var ct when ct.StartsWith("video/") => MaxVideoBytes,
        var ct when ct.StartsWith("image/") => MaxImageBytes,
        _                                   => MaxDocBytes
    };
}
