using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Tests;

public class ReadGateTests
{
    private static readonly FileStatus[] ReadableStatuses = { FileStatus.Uploaded, FileStatus.Ready };
    private static readonly FileStatus[] BlockedStatuses = { FileStatus.Quarantined, FileStatus.Rejected, FileStatus.Deleted, FileStatus.Created, FileStatus.UploadUrlGenerated };

    [Theory]
    [MemberData(nameof(GetReadableStatuses))]
    public void Readable_statuses_should_be_allowed(FileStatus status)
    {
        IsReadable(status).Should().BeTrue($"status {status} should be readable");
    }

    [Theory]
    [MemberData(nameof(GetBlockedStatuses))]
    public void Blocked_statuses_should_be_refused(FileStatus status)
    {
        IsReadable(status).Should().BeFalse($"status {status} should be blocked");
    }

    private static bool IsReadable(FileStatus status) =>
        status == FileStatus.Uploaded || status == FileStatus.Ready;

    public static IEnumerable<object[]> GetReadableStatuses() => ReadableStatuses.Select(s => new object[] { s });
    public static IEnumerable<object[]> GetBlockedStatuses() => BlockedStatuses.Select(s => new object[] { s });
}
