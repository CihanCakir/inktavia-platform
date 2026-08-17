namespace Aizen.Modules.Messaging.Domain.Interface;

[DocumentationInfo("LLM content analyzer interface",
    "Asynchronous AI-powered content analysis — runs after message delivery, non-blocking.")]
public interface ILlmContentAnalyzer
{
    /// <summary>
    /// Analyzes message content using LLM. Fire-and-forget from the send pipeline.
    /// Mutates message moderation status if risk is detected.
    /// </summary>
    Task AnalyzeAsync(long messageId, string content, long conversationId, CancellationToken ct = default);
}
