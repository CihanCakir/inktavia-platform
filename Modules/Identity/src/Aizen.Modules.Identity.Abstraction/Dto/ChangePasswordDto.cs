namespace Aizen.Modules.Identity.Abstraction.Dto
{
    public class ChangePasswordDto
    {
        public long UserId { get; set; }

        public ChangePasswordDto(long userId)
        {
            UserId = userId;
        }
    }
}