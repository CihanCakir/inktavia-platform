using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserExternalLoginEntity : AizenEntityWithAudit
    {
        public long UserId { get; private set; }
        public LoginType Provider { get; private set; }
        public string? ProviderUserId { get; private set; } // id_token.sub
        public string? EmailAtLinkTime { get; private set; }
        public string? Scope { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; private set; }

        public virtual UserEntity User { get; private set; } = default!;

        // EF Core lazy-loading proxies (Castle DynamicProxy) subclass the entity, so the parameterless ctor must be
        // at least protected. A private one makes every query that materializes this type fail at runtime.
        protected UserExternalLoginEntity() { }

        public static UserExternalLoginEntity Create(long userId, LoginType provider, string providerUserId, string? email, string? scope)
            => new()
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                EmailAtLinkTime = email,
                Scope = scope
            };

        public void Update(string providerUserId, string? email, string? scope)
        {
            ProviderUserId = providerUserId;
            EmailAtLinkTime = email;
            Scope = scope;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}