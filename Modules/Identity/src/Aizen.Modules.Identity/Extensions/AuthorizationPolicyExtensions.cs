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
            options.AddPolicy("IdentityRead", policy =>
                policy.RequireRole("identity_read", "admin_user"));

            options.AddPolicy("IdentityWrite", policy =>
                policy.RequireRole("identity_write", "admin_user"));

            options.AddPolicy("ProfileRead", policy =>
                policy.RequireRole("profile_read", "admin_user"));

            options.AddPolicy("ProfileWrite", policy =>
                policy.RequireRole("profile_write", "admin_user"));

            options.AddPolicy("PaymentRead", policy =>
                policy.RequireRole("payment_read", "admin_user"));

            options.AddPolicy("PaymentWrite", policy =>
                policy.RequireRole("payment_write", "admin_user"));

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin_user"));
        });

        return services;
    }
}
