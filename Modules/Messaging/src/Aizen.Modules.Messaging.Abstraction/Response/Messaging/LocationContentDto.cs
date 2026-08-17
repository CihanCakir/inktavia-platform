namespace Aizen.Modules.Messaging.Abstraction.Response.Messaging;

[DocumentationInfo("Location content DTO", "Parsed location payload for MessageType.Location messages.")]
public sealed record LocationContentDto(
    double Lat,
    double Lng,
    string Label,
    double? Accuracy
)
{
    /// <summary>Google Maps URL — opened by web clients and as fallback.</summary>
    public string GoogleMapsUrl
        => $"https://www.google.com/maps?q={Lat},{Lng}";

    /// <summary>Yandex Maps URL — preferred for TR/RU clients.</summary>
    public string YandexMapsUrl
        => $"https://yandex.com/maps/?pt={Lng},{Lat}&z=16&l=map";

    /// <summary>Universal geo URI — iOS/Android opens native map app.</summary>
    public string GeoUri
        => $"geo:{Lat},{Lng}?q={Lat},{Lng}({Uri.EscapeDataString(Label)})";
}
