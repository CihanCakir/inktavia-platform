using System.Reflection;
using Aizen.Bff.AdminPanel.Controllers.V1;
using Aizen.Modules.Content.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Aizen.Bff.AdminPanel.UnitTests.Content;

/// <summary>
/// Requirements #1 and #5: every ContentController action is [Authorize]d with a role set that is a
/// subset of one of the module's own canonical sets (ContentRoles.AdminAuthoring / .Moderation) — the
/// BFF must never be broader than the module — and comment-moderation actions require the Moderation
/// set specifically (not the authoring one). Asserted by reflection so a drift breaks the build's tests.
/// </summary>
public sealed class ContentControllerAuthorizationTests
{
    private static readonly HashSet<string> AuthoringSet =
        ContentRoles.AdminAuthoring.Split(',').Select(s => s.Trim()).ToHashSet();

    private static readonly HashSet<string> ModerationSet =
        ContentRoles.Moderation.Split(',').Select(s => s.Trim()).ToHashSet();

    private static IEnumerable<MethodInfo> ActionMethods() =>
        typeof(ContentController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(false).Any());

    private static HashSet<string> RolesOf(MethodInfo m) =>
        m.GetCustomAttributes<AuthorizeAttribute>(true)
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(','))
            .Select(s => s.Trim())
            .ToHashSet();

    private static bool IsCommentAction(MethodInfo m) =>
        m.GetCustomAttributes<HttpMethodAttribute>(false)
            .Any(a => (a.Template ?? string.Empty).Contains("comments", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Every_action_has_authorize_with_roles_subset_of_a_module_set()
    {
        var actions = ActionMethods().ToList();
        actions.Should().NotBeEmpty("the ContentController must expose actions");

        foreach (var action in actions)
        {
            var roles = RolesOf(action);
            roles.Should().NotBeEmpty($"{action.Name} must carry [Authorize(Roles=...)]");

            var subsetOfAuthoring = roles.IsSubsetOf(AuthoringSet);
            var subsetOfModeration = roles.IsSubsetOf(ModerationSet);

            (subsetOfAuthoring || subsetOfModeration).Should().BeTrue(
                $"{action.Name} roles [{string.Join(",", roles)}] must be a subset of the module's " +
                $"AdminAuthoring [{ContentRoles.AdminAuthoring}] or Moderation [{ContentRoles.Moderation}] set");
        }
    }

    [Fact]
    public void Moderation_actions_require_the_moderation_set_not_authoring()
    {
        var commentActions = ActionMethods().Where(IsCommentAction).ToList();
        commentActions.Should().NotBeEmpty("comment moderation actions must exist");

        foreach (var action in commentActions)
        {
            var roles = RolesOf(action);
            roles.SetEquals(ModerationSet).Should().BeTrue(
                $"{action.Name} must require exactly the Moderation set [{ContentRoles.Moderation}]");
            roles.SetEquals(AuthoringSet).Should().BeFalse(
                $"{action.Name} must NOT use the authoring set");
        }
    }

    [Fact]
    public void Authoring_actions_use_the_authoring_set()
    {
        var authoringActions = ActionMethods().Where(m => !IsCommentAction(m)).ToList();
        authoringActions.Should().NotBeEmpty();

        foreach (var action in authoringActions)
        {
            RolesOf(action).SetEquals(AuthoringSet).Should().BeTrue(
                $"{action.Name} must require the AdminAuthoring set [{ContentRoles.AdminAuthoring}]");
        }
    }
}
