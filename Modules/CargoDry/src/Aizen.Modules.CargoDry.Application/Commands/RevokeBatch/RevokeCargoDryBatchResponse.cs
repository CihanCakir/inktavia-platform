
namespace Aizen.Modules.CargoDry.Application.Commands.RevokeBatch;

[DocumentationInfo("Revoke CargoDry batch response",
    "Returned after a successful batch revoke operation.")]
public sealed class RevokeCargoDryBatchResponse
{
    public long   BatchId            { get; init; }
    public string BatchCode          { get; init; } = default!;
    public string Reason             { get; init; } = default!;
    public int    AvailableKitsAlsoRevoked { get; init; }
    public string RevokedAt          { get; init; } = default!;
}
