using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Alfanar.MarketIntel.Api.Services;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        if (env.IsDevelopment())
        {
            return true;
        }

        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        return configuration.GetValue("Hangfire:DashboardEnabled", false);
    }
}
