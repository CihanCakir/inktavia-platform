using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Tests;

public class FileEntityTests
{
    [Fact]
    public void Create_assigns_a_non_empty_PublicId()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);

        file.PublicId.Should().NotBeNull();
        file.PublicId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_sets_status_to_Created()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);

        file.Status.Should().Be(FileStatus.Created);
    }

    [Fact]
    public void MarkRejected_sets_status_to_Rejected()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);

        file.MarkRejected("bad content");

        file.Status.Should().Be(FileStatus.Rejected);
    }

    [Fact]
    public void PromoteToReady_sets_status_to_Ready()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);
        file.MarkUploadUrlGenerated();
        file.MarkUploaded();

        file.PromoteToReady();

        file.Status.Should().Be(FileStatus.Ready);
    }

    [Fact]
    public void MarkQuarantined_sets_status_to_Quarantined()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);
        file.MarkUploadUrlGenerated();
        file.MarkUploaded();

        file.MarkQuarantined();

        file.Status.Should().Be(FileStatus.Quarantined);
    }

    [Fact]
    public void UpdateActualSize_persists_the_real_size()
    {
        var file = FileEntity.Create("CODE", "test.pdf", "stored.pdf", "bucket", "key",
            "application/pdf", "pdf", 1024, StorageProviderType.AwsS3,
            FileVisibility.Private, FileCategory.Document, 42);

        file.UpdateActualSize(2048);

        file.SizeInBytes.Should().Be(2048);
    }
}
