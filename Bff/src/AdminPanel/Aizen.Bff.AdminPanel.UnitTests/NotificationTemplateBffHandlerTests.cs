using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Notifications.Command;
using Aizen.Bff.AdminPanel.Application.Notifications.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using FluentAssertions;
using NSubstitute;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// Phase-2 template admin BFF handler'ları: her biri Notification modülüne ince bir proxy'dir (result?.Body döner;
/// sayfalı liste gövde null gelirse boş sayfaya düşer). Refit arayüzü NSubstitute ile taklit edilir.
/// </summary>
public sealed class NotificationTemplateBffHandlerTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    private static INotificationTemplateRemoteCall Remote() => Substitute.For<INotificationTemplateRemoteCall>();

    // ── Paged list ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Paged_maps_module_body()
    {
        var remote = Remote();
        remote.GetTemplatesPaged(Arg.Any<NotificationChannel?>(), Arg.Any<string?>(), Arg.Any<TemplateContentStatus?>(),
                Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Ok(new NotificationTemplateListResult
            {
                Items = new List<NotificationTemplateListItemDto>
                {
                    new() { Id = 1, TemplateCode = "SR_CREATED_INAPP", Name = "SR Created", Type = NotificationType.ServiceRequestCreated, Channel = NotificationChannel.InApp, IsActive = true },
                },
                TotalCount = 42, Page = 2, PageSize = 20,
            }));

        var handler = new GetNotificationTemplatesPagedBffQueryHandler(remote);
        var result = await handler.Handle(
            new GetNotificationTemplatesPagedBffQuery { Page = 2, PageSize = 20 }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(42);
        result.Page.Should().Be(2);
        result.Items[0].TemplateCode.Should().Be("SR_CREATED_INAPP");
    }

    [Fact]
    public async Task Paged_null_body_falls_back_to_empty_page_preserving_paging()
    {
        var remote = Remote();
        remote.GetTemplatesPaged(Arg.Any<NotificationChannel?>(), Arg.Any<string?>(), Arg.Any<TemplateContentStatus?>(),
                Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((AizenApiResponse<NotificationTemplateListResult>)null!);

        var handler = new GetNotificationTemplatesPagedBffQueryHandler(remote);
        var result = await handler.Handle(
            new GetNotificationTemplatesPagedBffQuery { Page = 3, PageSize = 15 }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(15);
    }

    // ── Detail matrix ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Detail_returns_module_body_with_content_matrix()
    {
        var remote = Remote();
        remote.GetTemplateDetail("SR_CREATED_INAPP").Returns(Ok(new NotificationTemplateDetailDto
        {
            Id = 1, TemplateCode = "SR_CREATED_INAPP", Name = "SR Created", Channel = NotificationChannel.InApp, IsActive = true,
            Contents = new List<NotificationTemplateContentCellDto>
            {
                new() { Channel = NotificationChannel.InApp, Locale = "en", PublishedVersion = 1, DraftVersion = 2, Status = TemplateContentStatus.Published },
            },
        }));

        var handler = new GetNotificationTemplateDetailBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationTemplateDetailBffQuery("SR_CREATED_INAPP"), CancellationToken.None);

        result!.TemplateCode.Should().Be("SR_CREATED_INAPP");
        result.Contents.Should().ContainSingle();
        result.Contents[0].PublishedVersion.Should().Be(1);
        result.Contents[0].DraftVersion.Should().Be(2);
    }

    // ── Content-for-edit (passthrough) ────────────────────────────────────────
    [Fact]
    public async Task ContentForEdit_returns_module_body_with_editing_source()
    {
        var remote = Remote();
        remote.GetTemplateContentForEdit("SR_CREATED_INAPP", NotificationChannel.InApp, "en")
            .Returns(Ok(new NotificationTemplateContentEditResult
            {
                Found = true, EditingSource = TemplateEditingSource.Draft,
                Content = new NotificationTemplateContentDto { Id = 7, Channel = NotificationChannel.InApp, Locale = "en", Version = 4, Status = TemplateContentStatus.Draft },
            }));

        var handler = new GetNotificationTemplateContentForEditBffQueryHandler(remote);
        var result = await handler.Handle(new GetNotificationTemplateContentForEditBffQuery
        {
            Code = "SR_CREATED_INAPP", Channel = NotificationChannel.InApp, Locale = "en",
        }, CancellationToken.None);

        result!.Found.Should().BeTrue();
        result.EditingSource.Should().Be(TemplateEditingSource.Draft);
        result.Content!.Version.Should().Be(4);
    }

    // ── Save draft (passthrough) ──────────────────────────────────────────────
    [Fact]
    public async Task SaveDraft_passes_body_through_and_returns_content()
    {
        var remote = Remote();
        remote.SaveTemplateContentDraft("C", NotificationChannel.Email, "tr", Arg.Any<SaveNotificationTemplateContentRequest>())
            .Returns(Ok(new NotificationTemplateContentDto { Id = 9, TemplateId = 1, Channel = NotificationChannel.Email, Locale = "tr", Version = 3, Status = TemplateContentStatus.Draft }));

        var handler = new SaveTemplateContentDraftBffCommandHandler(remote);
        var result = await handler.Handle(new SaveTemplateContentDraftBffCommand
        {
            Code = "C", Channel = NotificationChannel.Email, Locale = "tr",
            SubjectTemplate = "S {{x}}", HtmlTemplate = "<p>{{x}}</p>",
        }, CancellationToken.None);

        result!.Version.Should().Be(3);
        result.Status.Should().Be(TemplateContentStatus.Draft);
        await remote.Received(1).SaveTemplateContentDraft("C", NotificationChannel.Email, "tr",
            Arg.Is<SaveNotificationTemplateContentRequest>(r => r.SubjectTemplate == "S {{x}}" && r.HtmlTemplate == "<p>{{x}}</p>"));
    }

    // ── Publish ───────────────────────────────────────────────────────────────
    [Fact]
    public async Task Publish_returns_published_content()
    {
        var remote = Remote();
        remote.PublishTemplateContent("C", NotificationChannel.InApp, "en")
            .Returns(Ok(new NotificationTemplateContentDto { Id = 5, Channel = NotificationChannel.InApp, Locale = "en", Version = 2, Status = TemplateContentStatus.Published }));

        var handler = new PublishTemplateContentBffCommandHandler(remote);
        var result = await handler.Handle(new PublishTemplateContentBffCommand { Code = "C", Channel = NotificationChannel.InApp, Locale = "en" }, CancellationToken.None);

        result!.Status.Should().Be(TemplateContentStatus.Published);
        result.Version.Should().Be(2);
    }

    // ── Versions ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task Versions_returns_module_list()
    {
        var remote = Remote();
        remote.GetTemplateVersions("C", NotificationChannel.InApp, "en").Returns(Ok(new List<NotificationTemplateVersionDto>
        {
            new() { Id = 3, Version = 2, Status = TemplateContentStatus.Draft },
            new() { Id = 1, Version = 1, Status = TemplateContentStatus.Published },
        }));

        var handler = new GetTemplateContentVersionsBffQueryHandler(remote);
        var result = await handler.Handle(new GetTemplateContentVersionsBffQuery { Code = "C", Channel = NotificationChannel.InApp, Locale = "en" }, CancellationToken.None);

        result.Should().HaveCount(2);
        result![0].Version.Should().Be(2);
    }

    // ── Preview: rendered ─────────────────────────────────────────────────────
    [Fact]
    public async Task Preview_rendered_returns_content()
    {
        var remote = Remote();
        remote.PreviewTemplateContent("C", Arg.Any<NotificationTemplatePreviewRequest>())
            .Returns(Ok(new NotificationTemplatePreviewResultDto { Rendered = true, Title = "New Request: SR-1", Body = "b" }));

        var handler = new PreviewTemplateContentBffQueryHandler(remote);
        var result = await handler.Handle(new PreviewTemplateContentBffQuery
        {
            Code = "C", Channel = NotificationChannel.InApp, Locale = "en",
            Variables = new Dictionary<string, string> { ["requestCode"] = "SR-1" },
        }, CancellationToken.None);

        result!.Rendered.Should().BeTrue();
        result.Title.Should().Be("New Request: SR-1");
        result.MissingKeys.Should().BeEmpty();
    }

    // ── Preview: missing placeholders (BFF carries the flag; controller emits 400) ─
    [Fact]
    public async Task Preview_missing_placeholders_carries_missing_keys()
    {
        var remote = Remote();
        remote.PreviewTemplateContent("C", Arg.Any<NotificationTemplatePreviewRequest>())
            .Returns(Ok(new NotificationTemplatePreviewResultDto { Rendered = false, MissingKeys = new[] { "requestCode", "serviceName" } }));

        var handler = new PreviewTemplateContentBffQueryHandler(remote);
        var result = await handler.Handle(new PreviewTemplateContentBffQuery
        {
            Code = "C", Channel = NotificationChannel.InApp, Locale = "en",
            Variables = new Dictionary<string, string>(),
        }, CancellationToken.None);

        result!.Rendered.Should().BeFalse();
        result.MissingKeys.Should().BeEquivalentTo(new[] { "requestCode", "serviceName" });
    }

    // ── Variables catalog ─────────────────────────────────────────────────────
    [Fact]
    public async Task Variables_returns_catalog_body()
    {
        var remote = Remote();
        remote.GetTemplateVariables("C").Returns(Ok(new NotificationTemplateVariablesDto
        {
            TemplateCode = "C", Type = NotificationType.ServiceRequestPublished,
            Placeholders = new[] { "requestCode", "serviceName" },
        }));

        var handler = new GetTemplateVariablesBffQueryHandler(remote);
        var result = await handler.Handle(new GetTemplateVariablesBffQuery("C"), CancellationToken.None);

        result!.Placeholders.Should().BeEquivalentTo(new[] { "requestCode", "serviceName" });
    }

    // ── Meta update (command → Ok, forwards body) ─────────────────────────────
    [Fact]
    public async Task UpdateMeta_forwards_body_and_returns_ok()
    {
        var remote = Remote();
        remote.UpdateTemplateMeta("C", Arg.Any<UpdateNotificationTemplateMetaRequest>())
            .Returns(Ok(new NotificationTemplateMutationResponse { TemplateCode = "C", Success = true }));

        var handler = new UpdateNotificationTemplateMetaBffCommandHandler(remote);
        var result = await handler.Handle(new UpdateNotificationTemplateMetaBffCommand
        {
            Code = "C", Name = "New Name", Description = "desc", IsActive = false,
        }, CancellationToken.None);

        result!.Success.Should().BeTrue();
        await remote.Received(1).UpdateTemplateMeta("C",
            Arg.Is<UpdateNotificationTemplateMetaRequest>(r => r.Name == "New Name" && r.Description == "desc" && r.IsActive == false));
    }
}
