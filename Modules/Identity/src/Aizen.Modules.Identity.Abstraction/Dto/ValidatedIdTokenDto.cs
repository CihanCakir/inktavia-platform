namespace Aizen.Modules.Identity.Abstraction.Dto
{
    public sealed record ValidatedIdTokenDto(string Sub, string? Email, bool EmailVerified, string? Name, string Issuer, string Nonce);

}