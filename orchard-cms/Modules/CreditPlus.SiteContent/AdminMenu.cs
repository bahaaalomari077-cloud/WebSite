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
                .Url("/Admin/Contents/ContentItems?contentTypeId=SiteSettings"))
            .Add(_localizer["Header links"], "HeaderLinks", item => item
                .Url("/Admin/Contents/ContentItems?contentTypeId=HeaderLink"))
            .Add(_localizer["Footer links"], "FooterLinks", item => item
                .Url("/Admin/Contents/ContentItems?contentTypeId=FooterLink"))
            .Add(_localizer["Social links"], "SocialLinks", item => item
                .Url("/Admin/Contents/ContentItems?contentTypeId=SocialLink"))
            .Add(_localizer["Page links by page"], "PageLinks", pages => pages
                .Add(_localizer["Home page"], "HomePageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=HomePageLinks"))
                .Add(_localizer["About page"], "AboutPageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=AboutPageLinks"))
                .Add(_localizer["Buyers page"], "BuyersPageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=BuyersPageLinks"))
                .Add(_localizer["Calculator page"], "CalculatorPageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=CalculatorPageLinks"))
                .Add(_localizer["Article pages"], "ArticlePageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=ArticlePageLinks"))
                .Add(_localizer["Custom pages"], "CustomPageLinks", item => item
                    .Url("/Admin/Contents/ContentItems?contentTypeId=CustomPageLinks")))
            .Add(_localizer["Page phone and extra links"], "PagePlacements", item => item
                .Url("/Admin/Contents/ContentItems?contentTypeId=PagePlacement")));

        return ValueTask.CompletedTask;
    }
}
