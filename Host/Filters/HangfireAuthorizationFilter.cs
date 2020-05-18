using Hangfire.Dashboard;

namespace Host.Filters
{
    public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContextUser = context.GetHttpContext().User;
            return httpContextUser.Identity.IsAuthenticated;
        }
    }
}