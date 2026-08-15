using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class ContentStatusTransitionTests
{
    [Theory]
    [InlineData(ContentStatus.Draft)]
    [InlineData(ContentStatus.Scheduled)]
    public void Publish_allowed_from_draft_or_scheduled(ContentStatus s)
        => FluentActions.Invoking(() => ContentStatusTransition.EnsureCanPublish(s)).Should().NotThrow();

    [Theory]
    [InlineData(ContentStatus.Published)]
    [InlineData(ContentStatus.Archived)]
    public void Publish_rejected_from_published_or_archived(ContentStatus s)
        => FluentActions.Invoking(() => ContentStatusTransition.EnsureCanPublish(s)).Should().Throw<AizenBusinessException>();

    [Fact]
    public void Unpublish_only_from_published()
    {
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanUnpublish(ContentStatus.Published)).Should().NotThrow();
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanUnpublish(ContentStatus.Draft)).Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Schedule_from_draft_or_scheduled_only()
    {
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanSchedule(ContentStatus.Draft)).Should().NotThrow();
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanSchedule(ContentStatus.Published)).Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Archive_rejected_only_when_already_archived()
    {
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanArchive(ContentStatus.Published)).Should().NotThrow();
        FluentActions.Invoking(() => ContentStatusTransition.EnsureCanArchive(ContentStatus.Archived)).Should().Throw<AizenBusinessException>();
    }

    [Fact]
    public void Editable_rejected_only_when_archived()
    {
        FluentActions.Invoking(() => ContentStatusTransition.EnsureEditable(ContentStatus.Draft)).Should().NotThrow();
        FluentActions.Invoking(() => ContentStatusTransition.EnsureEditable(ContentStatus.Published)).Should().NotThrow();
        FluentActions.Invoking(() => ContentStatusTransition.EnsureEditable(ContentStatus.Archived)).Should().Throw<AizenBusinessException>();
    }
}
