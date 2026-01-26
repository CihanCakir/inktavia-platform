namespace Aizen.Core.InfoAccessor.Abstraction;

public interface IAizenUserInfoAccessor
{
    public AizenUserInfo UserInfo { get; }
}

public class AizenUserInfo : IAizenInfo
{
    public InfoLifeCycle LifeCycle => InfoLifeCycle.Scoped;
    public  string AccessToken { get; set; }
    public long UserId { get; set; }
    public string PhoneNumber { get; set; }
    public IEnumerable<string>  Roles { get; set; }
}