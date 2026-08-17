using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class MediaValidatorTests
{
    private static ContentMediaValidator Validator()
        => new(new FakeFileStorage(), new FakeInfo { UserId = 1, AccessToken = "tok" });

    private static ContentMediaDto[] Media(string id) => new[] { new ContentMediaDto { FileStorageId = id } };

    [Fact]
    public async Task Rejects_non_guid_id()
        => await FluentActions.Awaiting(() => Validator().ValidateAsync(Media("not-a-guid")))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*not a valid*");

    [Fact]
    public async Task Rejects_missing_asset()
        => await FluentActions.Awaiting(() => Validator().ValidateAsync(Media(FakeFileStorage.MissingId.ToString())))
            .Should().ThrowAsync<AizenBusinessException>().WithMessage("*not found*");

    [Fact]
    public async Task Accepts_existing_asset()
        => await FluentActions.Awaiting(() => Validator().ValidateAsync(Media(Guid.NewGuid().ToString())))
            .Should().NotThrowAsync();

    [Fact]
    public async Task Empty_media_list_is_a_no_op()
        => await FluentActions.Awaiting(() => Validator().ValidateAsync(System.Array.Empty<ContentMediaDto>()))
            .Should().NotThrowAsync();
}
