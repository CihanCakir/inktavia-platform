namespace Aizen.Modules.Identity.Abstraction.Response
{
    public class UserLoginResponse
    {
        public required TokenInfo Token { get; set; }
        public required UserInfo Profile { get; set; }
        public required AgreementInfo Agreement { get; set; }
    }

    public record TokenInfo(string? AccessToken, DateTime AccessTokenExpiredDate, string? RefreshToken, DateTime RefreshTokenExpiredDate);

    public record UserInfo(long UserId, string? Email, string? NationalityId, string? Name, string? Surname, string? PhoneNumber);

    public class AgreementInfo
    {
        public int AgreementId { get; set; } = 0;
        public bool AgreementApproved { get; set; } = true;
    }

}