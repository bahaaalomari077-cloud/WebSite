using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using CreditPlus.SiteContent.Migrations;

namespace CreditPlus.SiteContent;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IDataMigration, SiteContentMigrations>();
    }

    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute(
            name: "CreditPlusSiteSettings",
            areaName: "CreditPlus.SiteContent",
            pattern: "cms-api/site-settings",
            defaults: new { controller = "SiteSettings", action = "Get" });
    }
}
