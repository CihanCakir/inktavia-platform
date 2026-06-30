using Aizen.Core.Data.Mongo.Document;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.FileStorage.Domain.Documents;

[DocumentationInfo("File thumbnail metadata document", "MongoDB document storing thumbnail generation results for an image file.")]
public sealed class FileThumbnailMetadataDocument : AizenDocumentBase
{
    [BsonElement("fileId")]
    public Guid FileId { get; set; }

    [BsonElement("originalObjectKey")]
    public string OriginalObjectKey { get; set; } = default!;

    [BsonElement("thumbnails")]
    public List<ThumbnailEntry> Thumbnails { get; set; } = new();

    [BsonElement("generatedAt")]
    public DateTime GeneratedAt { get; set; }
}

[DocumentationInfo("Thumbnail entry", "Represents a single generated thumbnail variant.")]
public sealed class ThumbnailEntry
{
    [BsonElement("size")]
    public string Size { get; set; } = default!;

    [BsonElement("objectKey")]
    public string ObjectKey { get; set; } = default!;

    [BsonElement("widthPx")]
    public int WidthPx { get; set; }

    [BsonElement("heightPx")]
    public int HeightPx { get; set; }

    [BsonElement("sizeInBytes")]
    public long SizeInBytes { get; set; }
}
