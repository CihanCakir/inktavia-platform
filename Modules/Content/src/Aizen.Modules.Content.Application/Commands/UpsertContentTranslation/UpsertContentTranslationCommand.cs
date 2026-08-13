using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.UpsertContentTranslation;

/// <summary>Adds or replaces a single-language translation of a content item (§8), keyed by Lang.</summary>
public sealed class UpsertContentTranslationCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;

    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Body { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}
