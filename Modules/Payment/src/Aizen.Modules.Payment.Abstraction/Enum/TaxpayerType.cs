namespace Aizen.Modules.Payment.Abstraction
// <summary>
// Represents the type of taxpayer in the Inktavia Store domain.
// </summary>
{
    public enum TaxpayerType
    {
        Influencer = 1,
        Individual = 2,
        LLC = 3,
        Corporation = 4,
        SoleProprietorship = 5,
        Company = 6,
        SelfEmployed = 7,           // Serbest meslek
        IndividualNonTaxpayer = 8   // Vergi mükellefi değil → gider pusulası
    }
    public enum BillingMode
    {
        Reseller,               // Müşteriye satıcı: Biz
        AgencyPrincipalInvoices,// Müşteriye satıcı: Organizator (kendi faturası)
        AgencyOnBehalf          // Müşteriye satıcı: Organizator (faturayı biz onun adına keseriz)
    }

}
