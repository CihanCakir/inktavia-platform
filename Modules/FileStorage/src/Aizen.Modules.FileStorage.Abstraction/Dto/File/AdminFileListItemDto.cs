namespace Aizen.Modules.FileStorage.Abstraction.Dto.File;

[DocumentationInfo("Admin file list item DTO", "Tek bir dosyanın admin listeleme ekranında gösterilecek özet alanları.")]
public sealed class AdminFileListItemDto
{
    /// <summary>Dosyanın dışa açık kimliği (FileEntity.PublicId).</summary>
    public Guid Id { get; set; }

    public string FileName { get; set; } = default!;

    public string ContentType { get; set; } = default!;

    public long SizeBytes { get; set; }

    // Visibility bilinçli olarak string: BFF Newtonsoft serileştiricisinde StringEnumConverter YOK,
    // bu yüzden enum tipi FE'ye int olarak giderdi. String ile hem modül hem BFF tarafında
    // tutarlı, okunabilir değer ("Private"/"Internal"/"Public") döner. (Payment BFF DTO'ları ile aynı yaklaşım.)
    public string Visibility { get; set; } = default!;

    // Sahiplik FileOwnerReferenceEntity üzerinde tutulur; dosyanın hiç sahibi olmayabilir,
    // bu yüzden owner alanları nullable. Birden çok aktif sahip varsa ilk aktif referans alınır.
    public string? OwnerType { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
