namespace Aizen.Modules.Identity.Abstraction.Enum
{
    public enum OtpRequestContextType
    {
        /// <summary>
        /// Standart kullanıcı girişi
        /// </summary>
        StandardLogin = 0,

        /// <summary>
        /// Diğer kişi doğrulama (örn: paylaşılan kullanıcı hesabı)
        /// </summary>
        OtherPersonLogin = 1
    }

}