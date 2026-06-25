namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class GenerateBatchResultDto
{
    public string BatchCode       { get; init; } = default!;
    public int    GeneratedCount  { get; init; }
    public string QrZipFileUrl    { get; init; } = default!;
    public string ExcelFileUrl    { get; init; } = default!;
}
