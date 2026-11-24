using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

/// <summary>Full authoring detail (incl. all translations) of a single content item.</summary>
public sealed class GetContentItemBffQuery : AizenQuery<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
}
