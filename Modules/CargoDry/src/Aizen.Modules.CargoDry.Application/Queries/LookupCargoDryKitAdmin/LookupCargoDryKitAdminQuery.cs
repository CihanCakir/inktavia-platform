using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.LookupCargoDryKitAdmin;

public sealed class LookupCargoDryKitAdminQuery : AizenQuery<LookupCargoDryKitAdminResponse>
{
    /// <summary>
    /// Raw lookup string from the admin. Accepted formats:
    /// <list type="bullet">
    ///   <item>Numeric string → interpreted as kit Id (e.g. "1042")</item>
    ///   <item>Kit code format (e.g. "CD-2024-000123") → exact KitCode match</item>
    ///   <item>Serial number format (e.g. "SN-ABC-456") → exact SerialNumber match</item>
    /// </list>
    /// </summary>
    public string Query { get; init; } = default!;
}

public sealed class LookupCargoDryKitAdminResponse
{
    public CargoDryKitLookupResultDto Result { get; init; } = default!;
}
