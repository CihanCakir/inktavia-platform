using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Enum;

[DocumentationInfo("File category", "Classifies the type of file content.")]
public enum FileCategory { Image = 1, Document = 2, Video = 3, Invoice = 4, Certificate = 5, Report = 6, Other = 99 }
