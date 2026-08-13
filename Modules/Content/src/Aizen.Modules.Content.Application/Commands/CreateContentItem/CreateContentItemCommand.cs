using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Commands.CreateContentItem;

/// <summary>Creates a content item as a Draft (§8). AuthorUserId is set by the controller from the token.</summary>
public sealed class CreateContentItemCommand : AizenCommand<ContentItemDto>
{
    public ContentType Type { get; set; }
    public string? Slug { get; set; }
    public string DefaultLanguage { get; set; } = "tr";

    public List<ContentTranslationDto> Translations { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string? CategorySlug { get; set; }

    public List<ContentPlacementDto> Placements { get; set; } = new();
    public ContentAudienceDto? Audience { get; set; }
    public List<ContentMediaDto> Media { get; set; } = new();

    public ContentCampaignDto? Campaign { get; set; }
    public ContentReleaseNoteDto? ReleaseNote { get; set; }

    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }

    public long AuthorUserId { get; set; }
}
