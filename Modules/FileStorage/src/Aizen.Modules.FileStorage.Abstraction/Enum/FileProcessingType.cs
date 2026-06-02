using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File processing type", "Categorizes the background processing job applied to a file.")]
public enum FileProcessingType { VirusScan = 1, ThumbnailGeneration = 2, MetadataExtraction = 3, ContentTypeValidation = 4, Other = 99 }
