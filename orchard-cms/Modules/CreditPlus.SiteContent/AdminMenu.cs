using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace CreditPlus.SiteContent;

public sealed class AdminMenu : INavigationProvider
{
    private readonly IStringLocalizer<AdminMenu> _localizer;

    public AdminMenu(IStringLocalizer<AdminMenu> localizer) => _localizer = localizer;

    public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.CompletedTask;
        }

        builder.Add(_localizer["Credit Plus"], "CreditPlusSite", site => site
            .Add(_localizer["Site settings — logo, email and phones"], "SiteSettings", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "SiteSettings" }))
            .Add(_localizer["Header links"], "HeaderLinks", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "HeaderLink" }))
            .Add(_localizer["Footer links"], "FooterLinks", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "FooterLink" }))
            .Add(_localizer["Social links"], "SocialLinks", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "SocialLink" }))
            .Add(_localizer["Page links — buttons and destinations"], "PageLinks", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "PageLink" }))
            .Add(_localizer["Page phone and extra links"], "PagePlacements", item => item
                .Action("ContentItems", "Admin", new { area = "OrchardCore.Contents", contentTypeId = "PagePlacement" })));

        return ValueTask.CompletedTask;
    }
}
