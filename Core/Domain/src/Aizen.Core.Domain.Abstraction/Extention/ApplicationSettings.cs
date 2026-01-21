namespace Aizen.Core.Domain.Abstraction.Extention
{
    public class ApplicationSettings
    {
        public string ApiVersion { get; set; }
        public string AcceptLanguage { get; set; }
        public string DealerId { get; set; }
        public string DealerPassword { get; set; }
        public List<Integration> Integrations { get; set; }
        public BioTekno BioTekno { get; set; }
    }
    
    public class Integration
    {
        public string Key { get; set; }
        public string ProviderUrl { get; set; }
        public string ProviderExternalUrl { get; set; }

        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        public string Value4 { get; set; }
        public string Value5{ get; set; }

    }
    public class  BioTekno
    {
        public string GrantType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Scope { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}