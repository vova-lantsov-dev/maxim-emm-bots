using Microsoft.AspNetCore.Authorization;

namespace Host.Authentication
{
    internal static class Roles
    {
        public const string Admin = "admin";
        public static AuthorizationPolicy AdminPolicy() =>
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Admin).Build();

        public const string ReadOnly = "readonly";
        public static AuthorizationPolicy ReadOnlyPolicy() =>
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(ReadOnly).Build();
    }
}