using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File visibility", "Defines the access scope of a file.")]
public enum FileVisibility { Private = 1, Internal = 2, Public = 3 }
