using System.Text.Json;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.MarineProvider.Application.Common;

/// <summary>
/// FAZ13A #67 — modül hata zarfını 200 gövdesinde GİZLEMEK yerine YUKARI TAŞI.
///
/// Modüller (Identity, Faz 12B'den beri) iş hatalarını kararlı kodla (4300-4318) fırlatıyor → HTTP 400 +
/// Aizen envelope. Refit non-2xx'te ApiException fırlatır; BFF handler'ları bunu YAKALAYIP { Success=false }
/// olan bir HTTP 200'e çeviriyordu → frontend'in resolveApiErrorMessage'ı bunu HİÇ görmüyor, İngilizce gövde
/// mesajı doğrudan kullanıcıya gidiyordu (#67). Bu yardımcı, modülün KODUNU ve (Task A ile artık çağıranın
/// diline yerelleştirilmiş) MESAJINI koruyarak yeniden fırlatır → BFF middleware düzgün bir 400 failure
/// envelope üretir, frontend kodu eşleyebilir.
/// </summary>
public static class ModuleFailurePropagation
{
    /// <summary>Refit ApiException içindeki Aizen envelope'undan (header.errorCode + errorMessage) bir
    /// AizenBusinessException üretir. Kod yoksa yalnız mesajla (kodsuz) döner.</summary>
    public static AizenBusinessException FromApiException(Refit.ApiException ex, string fallbackMessage)
    {
        var (code, message) = Parse(ex.Content);
        var finalMessage = message ?? fallbackMessage;
        return code.HasValue
            ? new AizenBusinessException(code.Value, finalMessage)
            : new AizenBusinessException(finalMessage);
    }

    private static (int? code, string? message) Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("header", out var header))
                return (null, null);

            int? code = header.TryGetProperty("errorCode", out var c) && c.ValueKind == JsonValueKind.Number
                ? c.GetInt32()
                : null;
            if (code == 0) code = null; // 0 = success sentinel → kod yok say

            string? message = header.TryGetProperty("errorMessage", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(message)) message = null;

            return (code, message);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}
