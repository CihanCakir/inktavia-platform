namespace Aizen.Core.Starter.Abstraction.Configuration;

/// <summary>
/// Uygulama AÇILIRKEN (host henüz ayağa kalkmadan) yapılandırmanın güvenle çalışmaya elverişli olmadığını
/// bildiren HATA. İş-hatası DEĞİLDİR: bilerek <c>AizenException</c>'dan türetilmez — o tip, istek anındaki
/// hata-kodu/lokalizasyon sözlüğüne bağlıdır ve "iş hatası" olarak sunulur. Doldurulmamış konfig ise saatler
/// sonra bir iş hatasına dönüşmeden, sade ve gürültülü bir başlangıç çökmesi olmalıdır (borç #30'un özü).
/// </summary>
public sealed class AizenConfigurationException : System.Exception
{
    public AizenConfigurationException(string message) : base(message) { }
}
