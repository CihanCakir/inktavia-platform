namespace Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;

public sealed class AdminProvisionRequest
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? EmailVerified { get; set; }
}

public sealed class AdminProvisionResult
{
    public bool Provisioned { get; set; } = true;
    public long UserId { get; set; }
    public long? AdminProfileId { get; set; }
    public bool CreatedUser { get; set; }
    public bool CreatedProfile { get; set; }
    public bool AlreadyLinked { get; set; }
    public List<string> Warnings { get; set; } = new();
}
