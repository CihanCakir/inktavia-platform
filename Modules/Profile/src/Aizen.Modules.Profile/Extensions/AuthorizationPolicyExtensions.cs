namespace Aizen.Modules.Profile.Extensions;

public static class AuthorizationPolicyExtensions
{
    /// <summary>
    /// Registers Inktavia authorization policies.
    /// Each policy allows the specific realm role, admin_user, or the equivalent API client role.
    ///
    /// Two role schemes coexist: underscored realm roles (identity_write, admin_user) are held by
    /// human users, while dotted client roles (identity.write, profile.admin) are what BFF
    /// service-account tokens carry — those accounts have no realm roles at all. A policy that
    /// accepts only realm roles 403s every server-to-server call, so both are listed.
    /// </summary>
    public static IServiceCollection AddInktaviaAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("IdentityRead", policy =>
                policy.RequireRole("identity_read", "admin_user", "identity.read", "identity.admin"));

            options.AddPolicy("IdentityWrite", policy =>
                policy.RequireRole("identity_write", "admin_user", "identity.write", "identity.admin"));

            options.AddPolicy("ProfileRead", policy =>
                policy.RequireRole("profile_read", "admin_user", "profile.read", "profile.admin"));

            options.AddPolicy("ProfileWrite", policy =>
                policy.RequireRole("profile_write", "admin_user", "profile.write", "profile.admin"));

            options.AddPolicy("PaymentRead", policy =>
                policy.RequireRole("payment_read", "admin_user", "payment.read", "payment.admin"));

            options.AddPolicy("PaymentWrite", policy =>
                policy.RequireRole("payment_write", "admin_user", "payment.write", "payment.admin"));

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin_user", "profile.admin"));
        });

        return services;
    }
}
