using System.ComponentModel.DataAnnotations;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public sealed class SendOtpRequest
    {
        [Required, Phone, RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Phone must be E.164")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public sealed class CheckOtpRequest
    {
        [Required, Phone, RegularExpression(@"^\+?[1-9]\d{7,14}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, Range(0, 999999)]
        public int Otp { get; set; }

        [Required, StringLength(64, MinimumLength = 8)]
        public string ValidationGuid { get; set; } = string.Empty;
    }

    public sealed class LoginWithOtpRequest
    {
        [Required, Phone, RegularExpression(@"^\+?[1-9]\d{7,14}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, Range(0, 999999)]
        public int Otp { get; set; }

        [Required, StringLength(64, MinimumLength = 8)]
        public string ValidationGuid { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 6)]
        public string DeviceId { get; set; } = string.Empty;

        public string NotificationToken { get; set; } = string.Empty;
    }

    public sealed class LoginWithPhoneRequest
    {
        [Required, Phone, RegularExpression(@"^\+?[1-9]\d{7,14}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 4)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 6)]
        public string DeviceId { get; set; } = string.Empty;

        public string NotificationToken { get; set; } = string.Empty;
    }

    public sealed class LoginWithUsernameRequest
    {
        [Required, StringLength(128, MinimumLength = 2)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(16, MinimumLength = 4)]
        public string Pin { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 6)]
        public string DeviceId { get; set; } = string.Empty;

        public string NotificationToken { get; set; } = string.Empty;
    }

    public sealed class ChangePasswordRequest
    {
        [Required, StringLength(128, MinimumLength = 4)]
        public string OldPassword { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string NewPasswordComfirm { get; set; } = string.Empty; // (aynı isimle tuttum)
    }

    /// <summary>
    /// HTTP body modeli (refresh için).
    /// </summary>
    public sealed class RefreshLoginHttpRequest
    {
        [Required]
        public string RefreshToken { get; set; } = default!;

        /// <summary> İstek gelen cihaz kimliği. Boşsa handler InfoAccessor’dan alır. </summary>
        public string? DeviceId { get; set; }

        /// <summary> (Opsiyonel) Push bildirimi için token. </summary>
        public string? AccessToken { get; set; }
    }

}