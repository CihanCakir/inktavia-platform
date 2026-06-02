using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.FileStorage.Application.Queries.FilterFiles;

[DocumentationInfo("Filter files query", "Returns a filtered list of files by status, category, visibility and/or owner.")]
public sealed class FilterFilesQuery : AizenQuery<IReadOnlyList<FileDto>>
{
    public FilterFilesRequest Request { get; }

    public FilterFilesQuery(FilterFilesRequest request)
    {
        Request = request;
    }
}
