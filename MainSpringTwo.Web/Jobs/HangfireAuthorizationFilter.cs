using Hangfire.Dashboard;

namespace MainSpringTwo.Web.Jobs
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
