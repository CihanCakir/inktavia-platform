using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentComments;
using Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCountries;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W4 — envelope-binding discipline. Content PUBLIC endpoints return the raw DTO (bind <c>Task&lt;T&gt;</c>);
/// Content /me endpoints and ReferenceData endpoints return <see cref="AizenApiResponse{T}"/> (handlers unwrap
/// <c>.Body</c>). Verified both structurally (the Refit interface return types) and behaviourally (the handlers
/// return the unwrapped payload).
/// </summary>
public sealed class EnvelopeBindingTests
{
    [Fact]
    public void Content_public_methods_bind_raw_dto_not_envelope()
    {
        var bySlug = typeof(IContentRemoteCall).GetMethod(nameof(IContentRemoteCall.GetPublicBySlug))!;
        bySlug.ReturnType.Should().Be(typeof(Task<ContentItemDto>), "public reads bind the raw DTO");

        var feed = typeof(IContentRemoteCall).GetMethod(nameof(IContentRemoteCall.GetPublicFeed))!;
        feed.ReturnType.Should().Be(typeof(Task<ContentFeedResponse>));
    }

    [Fact]
    public void Content_me_and_reference_methods_bind_envelope()
    {
        var myComments = typeof(IContentRemoteCall).GetMethod(nameof(IContentRemoteCall.GetMyComments))!;
        myComments.ReturnType.Should().Be(typeof(Task<AizenApiResponse<List<ContentCommentDto>>>));

        var countries = typeof(IReferenceDataRemoteCall).GetMethod(nameof(IReferenceDataRemoteCall.GetCountries))!;
        countries.ReturnType.Should().Be(typeof(Task<AizenApiResponse<List<CountryDto>>>), "reference binds the envelope");
    }

    [Fact]
    public async Task BySlug_handler_maps_raw_dto()
    {
        var content = new FakeContentRemoteCall
        {
            BySlugResponse = new ContentItemDto
            {
                Id = "c1", Type = ContentType.Blog, Slug = "s", DefaultLanguage = "tr",
                Translations = new() { new() { Lang = "tr", Title = "Başlık" } },
            },
        };
        var handler = new GetWebContentBySlugQueryHandler(content, new FakeSeoIndexabilityPolicy(), NullLogger<GetWebContentBySlugQueryHandler>.Instance);

        var d = await handler.Handle(new GetWebContentBySlugQuery { Slug = "s", Lang = "tr" }, default);

        d!.Title.Should().Be("Başlık");
    }

    [Fact]
    public async Task MyComments_handler_unwraps_envelope_body()
    {
        var content = new FakeContentRemoteCall
        {
            MyCommentsResponse = Env.Ok(new List<ContentCommentDto>
            {
                new() { Id = "m1", ContentId = "c1", Body = "Benim", Status = ContentCommentStatus.Approved,
                        AuthorUserId = 500, CreatedAt = DateTimeOffset.UtcNow },
            }),
        };
        var handler = new GetWebMyContentCommentsQueryHandler(
            new FakeWebParticipantProfileResolver(userId: 500), content,
            NullLogger<GetWebMyContentCommentsQueryHandler>.Instance);

        var list = await handler.Handle(new GetWebMyContentCommentsQuery { ContentId = "c1" }, default);

        list.Should().ContainSingle();
        list![0].Status.Should().Be(ContentCommentStatus.Approved);
    }

    [Fact]
    public async Task Countries_handler_unwraps_envelope_body()
    {
        var reference = new FakeReferenceDataRemoteCall
        {
            CountriesResponse = Env.Ok(new List<CountryDto>
            {
                new() { CountryCode = "TR", Name = "Türkiye", PhoneCode = "90", DefaultCurrencyCode = "TRY" },
            }),
        };
        var handler = new GetWebCountriesQueryHandler(reference, NullLogger<GetWebCountriesQueryHandler>.Instance);

        var list = await handler.Handle(new GetWebCountriesQuery(), default);

        list.Should().ContainSingle().Which.CountryCode.Should().Be("TR");
    }
}
