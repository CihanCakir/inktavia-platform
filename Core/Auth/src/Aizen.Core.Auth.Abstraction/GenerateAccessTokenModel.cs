namespace Aizen.Core.Auth.Abstraction
{
    public class GenerateAccessTokenModel
    {
        /// <summary>
        /// Giriş yapan kullanıcının ID'si
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Kullanıcı adı veya sistemdeki benzersiz login bilgisi
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının sahip olduğu rol(ler) — örn. "Admin", "Organizer", "Member"
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// (İsteğe bağlı) Kullanıcının bulunduğu platform (örn: Web, Mobile, OrganizerApp)
        /// </summary>
        public string? Platform { get; set; }

        /// <summary>
        /// (İsteğe bağlı) Uygulama versiyonu (loglama, versiyon kontrol vs. için)
        /// </summary>
        public string? AppVersion { get; set; }


        public GenerateAccessTokenModel(
            long userId,
            string userName,
            List<string> roles,
            string? platform = null,
            string? appVersion = null)
        {
            UserId = userId;
            UserName = userName;
            Roles = roles;
            Platform = platform;
            AppVersion = appVersion;
        }
    }

}