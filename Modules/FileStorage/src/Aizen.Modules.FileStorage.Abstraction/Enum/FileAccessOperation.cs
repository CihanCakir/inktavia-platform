using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File access operation", "Defines the type of access operation permitted on a file.")]
public enum FileAccessOperation { Read = 1, Write = 2, Delete = 3 }
