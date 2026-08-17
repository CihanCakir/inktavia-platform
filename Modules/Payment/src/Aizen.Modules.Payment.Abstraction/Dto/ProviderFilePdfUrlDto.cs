namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderFilePdfUrlDto
{
    public string Url              { get; init; } = default!;
    public int    ExpiresInSeconds { get; init; }
}
