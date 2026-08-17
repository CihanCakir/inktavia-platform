namespace Aizen.Modules.Identity.Extensions;

public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Registers Inktavia authorization policies based on realm roles.
    /// Each policy allows the specific role OR admin_user.
    /// </summary>
    public static IServiceCollection AddInktaviaAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Realm roles (underscored) are held by human users; the dotted variants are
            // identity-api client roles carried by BFF service-account tokens, which have no
            // realm roles at all. Both must satisfy the policy or server-to-server calls 403.
            options.AddPolicy("IdentityRead", policy =>
                policy.RequireRole("identity_read", "admin_user", "identity.read", "identity.admin"));

            options.AddPolicy("IdentityWrite", policy =>
                policy.RequireRole("identity_write", "admin_user", "identity.write", "identity.admin"));

            options.AddPolicy("ProfileRead", policy =>
                policy.RequireRole("profile_read", "admin_user"));

            options.AddPolicy("ProfileWrite", policy =>
                policy.RequireRole("profile_write", "admin_user"));

            options.AddPolicy("PaymentRead", policy =>
                policy.RequireRole("payment_read", "admin_user"));

            options.AddPolicy("PaymentWrite", policy =>
                policy.RequireRole("payment_write", "admin_user"));

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin_user", "identity.admin"));
        });

        return services;
    }
}
