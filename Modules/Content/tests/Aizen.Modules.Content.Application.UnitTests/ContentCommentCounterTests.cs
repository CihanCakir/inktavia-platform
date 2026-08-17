using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Content.Application.UnitTests;

public sealed class ContentCommentCounterTests
{
    [Theory]
    [InlineData(ContentCommentStatus.Pending,  ContentCommentStatus.Approved, +1)]
    [InlineData(ContentCommentStatus.Rejected, ContentCommentStatus.Approved, +1)]
    [InlineData(ContentCommentStatus.Hidden,   ContentCommentStatus.Approved, +1)]
    [InlineData(ContentCommentStatus.Approved, ContentCommentStatus.Hidden,   -1)]
    [InlineData(ContentCommentStatus.Approved, ContentCommentStatus.Rejected, -1)]
    [InlineData(ContentCommentStatus.Approved, ContentCommentStatus.Approved,  0)]
    [InlineData(ContentCommentStatus.Pending,  ContentCommentStatus.Hidden,    0)]
    [InlineData(ContentCommentStatus.Rejected, ContentCommentStatus.Hidden,    0)]
    public void TransitionDelta_matches_matrix(ContentCommentStatus from, ContentCommentStatus to, int expected)
        => ContentCommentCounter.TransitionDelta(from, to).Should().Be(expected);

    [Theory]
    [InlineData(ContentCommentStatus.Approved, -1)]
    [InlineData(ContentCommentStatus.Pending,   0)]
    [InlineData(ContentCommentStatus.Hidden,    0)]
    [InlineData(ContentCommentStatus.Rejected,  0)]
    public void DeleteDelta_only_decrements_approved(ContentCommentStatus current, int expected)
        => ContentCommentCounter.DeleteDelta(current).Should().Be(expected);
}
