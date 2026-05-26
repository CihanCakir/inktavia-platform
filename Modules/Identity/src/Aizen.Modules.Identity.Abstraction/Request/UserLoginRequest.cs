namespace Aizen.Modules.Identity.Abstraction.Request
{
    public class UserLoginRequest
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = default!; // Username → UserName
        public string DeviceId { get; set; } = default!;
        public string? Email { get; set; }
        public string? NationalityId { get; set; }
        public string? FirstName { get; set; }   // Name → FirstName
        public string? LastName { get; set; }    // Surname → LastName
        public string? PhoneNumber { get; set; }
        public string NotificationToken { get; set; } = default!;
        public WorkshopRoleContext RoleContext { get; set; }
        public ConsumerDeviceType DeviceType { get; set; }
        public long? ActiveProfileId { get; set; }
        public List<string>? Roles { get; set; }
    }

}