namespace Aizen.Modules.FileStorage.Abstraction.Request.File;

[DocumentationInfo("Admin file list request", "Admin dosya listeleme için arama, içerik tipi filtresi ve sayfalama parametreleri.")]
public sealed class AdminFileListRequest
{
    /// <summary>Dosya adında (OriginalFileName) geçen metne göre filtre. Boş ise filtre uygulanmaz.</summary>
    public string? Search { get; set; }

    /// <summary>MIME içerik tipine göre birebir filtre (ör. image/png). Boş ise filtre uygulanmaz.</summary>
    public string? ContentType { get; set; }

    /// <summary>1 tabanlı sayfa numarası.</summary>
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
