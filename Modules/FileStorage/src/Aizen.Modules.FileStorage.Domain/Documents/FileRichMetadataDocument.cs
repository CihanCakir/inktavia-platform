using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.FileStorage.Abstraction.Model;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.FileStorage.Domain.Documents;

[DocumentationInfo("File rich metadata document", "MongoDB document storing rich extracted metadata (EXIF, PDF properties, etc.) for a file.")]
public sealed class FileRichMetadataDocument : AizenDocumentBase
{
    [BsonElement("fileCode")]
    public string FileCode { get; set; } = default!;

    [BsonElement("fileId")]
    public Guid FileId { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [BsonElement("extractedAt")]
    public DateTime ExtractedAt { get; set; }
}
