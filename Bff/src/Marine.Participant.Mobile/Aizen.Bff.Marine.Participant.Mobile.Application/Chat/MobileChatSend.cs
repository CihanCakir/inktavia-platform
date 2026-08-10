using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Chat;

/// <summary>The kind of chat message being sent (exactly one per message).</summary>
public enum MobileChatSendKind { Text, Image, Location }

/// <summary>
/// BE-MO10b — pure validation of an owner chat send: exactly one of { text, image, location }, coords in range, and
/// length caps. Kept static (no I/O) so it is unit-testable. The SR module does NO validation on this path, so the
/// BFF is the gate (mirrors the entity's Location-wins-over-Image-wins-over-Text branch precedence).
/// </summary>
public static class MobileChatSend
{
    public const int MaxContentLength = 4000;
    public const int MaxLabelLength = 200;

    /// <summary>Validates + normalizes the send. Returns the message kind + the trimmed text (empty for image/
    /// location). Throws <see cref="AizenBusinessException"/> on any rule violation.</summary>
    public static (MobileChatSendKind Kind, string Content) Validate(
        string? content, Guid? attachmentFileId, double? lat, double? lng, string? label)
    {
        var text = content?.Trim();
        var hasText = !string.IsNullOrWhiteSpace(text);
        var hasImage = attachmentFileId is { } id && id != Guid.Empty;
        var hasLocation = lat.HasValue && lng.HasValue;

        var count = (hasText ? 1 : 0) + (hasImage ? 1 : 0) + (hasLocation ? 1 : 0);
        if (count == 0)
            throw new AizenBusinessException("A message must have text, an image, or a location.");
        if (count > 1)
            throw new AizenBusinessException("A message can be only one of text, an image, or a location.");

        if (hasLocation)
        {
            if (lat is < -90 or > 90)
                throw new AizenBusinessException("Latitude must be between -90 and 90.");
            if (lng is < -180 or > 180)
                throw new AizenBusinessException("Longitude must be between -180 and 180.");
            if ((label?.Length ?? 0) > MaxLabelLength)
                throw new AizenBusinessException($"The location label cannot exceed {MaxLabelLength} characters.");
            return (MobileChatSendKind.Location, string.Empty);
        }

        if (hasImage)
            return (MobileChatSendKind.Image, string.Empty);

        if (text!.Length > MaxContentLength)
            throw new AizenBusinessException($"A message cannot exceed {MaxContentLength} characters.");
        return (MobileChatSendKind.Text, text);
    }
}
