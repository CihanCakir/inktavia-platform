using System.Net.Http.Json;
using System.Text.Json;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Application.Services;

[DocumentationInfo("Anthropic LLM content analyzer",
    "Calls Claude API to detect nuanced policy violations: tacit off-platform solicitation, threats, competitor referrals.")]
public sealed class AnthropicLlmContentAnalyzer : ILlmContentAnalyzer
{
    private readonly HttpClient _http;
    private readonly IConversationMessageRepository _messages;
    private readonly IConversationRepository _conversations;
    private readonly ILogger<AnthropicLlmContentAnalyzer> _logger;
    private readonly string _apiKey;

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";

    public AnthropicLlmContentAnalyzer(
        IHttpClientFactory httpFactory,
        IConversationMessageRepository messages,
        IConversationRepository conversations,
        IConfiguration config,
        ILogger<AnthropicLlmContentAnalyzer> logger)
    {
        _http          = httpFactory.CreateClient("anthropic");
        _messages      = messages;
        _conversations = conversations;
        _logger        = logger;
        _apiKey        = config["Messaging:LlmModeration:AnthropicApiKey"]
                         ?? throw new InvalidOperationException("Anthropic API key not configured.");
    }

    public async Task AnalyzeAsync(long messageId, string content, long conversationId, CancellationToken ct = default)
    {
        try
        {
            var conversation = await _conversations.GetByIdAsync(conversationId, ct);
            var contextLabel = conversation?.ContextType.ToString() ?? "Unknown";

            var prompt  = BuildPrompt(content, contextLabel);
            var verdict = await CallAnthropicAsync(prompt, ct);

            if (verdict is null || verdict.Risk == "none" || verdict.Risk == "low")
                return;

            var message = await _messages.GetByIdAsync(messageId, ct);
            if (message is null) return;

            var newStatus = verdict.Risk == "high"
                ? MessageModerationStatus.Flagged
                : MessageModerationStatus.PendingReview;

            message.SetModerationStatus(newStatus, $"LLM: {verdict.Reason}");
            _messages.Update(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM content analysis failed for message {MessageId}. Continuing.", messageId);
        }
    }

    private static string BuildPrompt(string content, string contextType) => $$"""
        You are a content moderator for a professional marine marketplace platform.
        Analyze the following message and return a JSON verdict.

        Context: {{contextType}} conversation between marine service professionals.

        Message: "{{content}}"

        Check for:
        - Off-platform payment or communication solicitation (tacit or obfuscated)
        - Harassment, threats, or abusive language
        - Competitor platform referrals or price undercutting schemes
        - Fraud signals (fake identity, false credentials)

        Respond ONLY with valid JSON in this format:
        { "risk": "none|low|medium|high", "reason": "brief explanation or empty string" }
        """;

    private async Task<LlmVerdict?> CallAnthropicAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model      = Model,
            max_tokens = 128,
            messages   = new[] { new { role = "user", content = prompt } }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _http.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
        var text = body?.Content?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text)) return null;

        text = text.Trim().TrimStart('`').TrimEnd('`').Trim();
        if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            text = text[4..].Trim();

        return JsonSerializer.Deserialize<LlmVerdict>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private sealed record LlmVerdict(string Risk, string Reason);
    private sealed record AnthropicResponse(List<AnthropicContentBlock>? Content);
    private sealed record AnthropicContentBlock(string Type, string Text);
}
