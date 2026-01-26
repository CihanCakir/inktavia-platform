namespace Aizen.Core.Realtime.Abstraction.Interfaces;
/// <summary>
/// Uygulama başlatılırken domain event setlerini register etmek için kullanabileceğiniz soyutlama.
/// Module'ler kendi domain registrar'larını implement edip DI içinde çalıştırabilir veya extensions üzerinden register edebilirsiniz.
/// </summary>
public interface IRealtimeDomainRegistrar
{
    void Register();
}