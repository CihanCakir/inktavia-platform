namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public sealed class UserLoginHistoryItemDto
{
    public long Id { get; set; }
    public string? RoleContext { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? LoginAt { get; set; }
}
