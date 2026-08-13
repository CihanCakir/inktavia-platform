using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetContentByIdAdmin;

/// <summary>Admin get-by-id (§8). Returns the item in any status (null when not found / soft-deleted).</summary>
public sealed class GetContentByIdAdminQuery : AizenQuery<ContentItemDto?>
{
    public string ContentId { get; set; } = default!;
}
