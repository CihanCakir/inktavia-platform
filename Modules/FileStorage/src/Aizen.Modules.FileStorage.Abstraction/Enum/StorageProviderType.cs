using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("Storage provider type", "Identifies the backend object storage provider.")]
public enum StorageProviderType { AwsS3 = 1, Minio = 2, Local = 3 }
