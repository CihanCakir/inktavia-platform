using Aizen.Core.Data.Mongo.Document;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using MongoDB.Bson.Serialization.Attributes;

namespace Aizen.Modules.FileStorage.Domain.Documents;

[DocumentationInfo("File processing result document", "MongoDB document storing detailed processing results for a file.")]
public sealed class FileProcessingResultDocument : AizenDocumentBase
{
    [BsonElement("fileId")]
    public Guid FileId { get; set; }

    [BsonElement("processingType")]
    public FileProcessingType ProcessingType { get; set; }

    [BsonElement("isSuccess")]
    public bool IsSuccess { get; set; }

    [BsonElement("resultData")]
    public Dictionary<string, string> ResultData { get; set; } = new();

    [BsonElement("errorCode")]
    public string? ErrorCode { get; set; }

    [BsonElement("completedAt")]
    public DateTime CompletedAt { get; set; }
}
