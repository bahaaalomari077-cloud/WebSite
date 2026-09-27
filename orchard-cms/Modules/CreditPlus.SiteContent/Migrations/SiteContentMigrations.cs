using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace CreditPlus.SiteContent.Migrations;

public sealed class SiteContentMigrations : DataMigration
{
    private readonly IContentDefinitionManager _definitions;
    private readonly IContentManager _contentManager;
    private readonly ISession _session;

    public SiteContentMigrations(IContentDefinitionManager definitions, IContentManager contentManager, ISession session)
    {
        _definitions = definitions;
        _contentManager = contentManager;
        _session = session;
    }

    public async Task<int> CreateAsync()
    {
        await _definitions.AlterTypeDefinitionAsync("SiteSettings", type => type
            .WithDisplayName("Site Settings")
            .Creatable()
            .Listable()
            .WithPart("TitlePart")
            .WithPart("AliasPart")
            .WithPart("SiteSettingsPart"));

        await _definitions.AlterPartDefinitionAsync("SiteSettingsPart", part => part
            .WithField("ContactEmail", field => field.OfType("TextField").WithDisplayName("Contact email"))
            .WithField("PhonePrimary", field => field.OfType("TextField").WithDisplayName("Primary phone"))
            .WithField("PhoneSecondary", field => field.OfType("TextField").WithDisplayName("Secondary phone"))
            .WithField("AddressEn", field => field.OfType("TextField").WithDisplayName("Address (English)"))
            .WithField("AddressAr", field => field.OfType("TextField").WithDisplayName("Address (Arabic)"))
            .WithField("ContactIntroEn", field => field.OfType("TextField").WithDisplayName("Contact introduction (English)"))
            .WithField("ContactIntroAr", field => field.OfType("TextField").WithDisplayName("Contact introduction (Arabic)"))
            .WithField("FooterDescriptionEn", field => field.OfType("TextField").WithDisplayName("Footer description (English)"))
            .WithField("FooterDescriptionAr", field => field.OfType("TextField").WithDisplayName("Footer description (Arabic)"))
            .WithField("LoginUrl", field => field.OfType("TextField").WithDisplayName("Login URL"))
            .WithField("SignupUrl", field => field.OfType("TextField").WithDisplayName("Signup URL"))
            .WithField("LinkedInUrl", field => field.OfType("TextField").WithDisplayName("LinkedIn URL"))
            .WithField("HeaderLinks", field => field.OfType("TextField").WithDisplayName("Header links JSON (legacy)"))
            .WithField("FooterLinks", field => field.OfType("TextField").WithDisplayName("Footer links JSON (legacy)"))
            .WithField("SocialLinks", field => field.OfType("TextField").WithDisplayName("Social links JSON (legacy)"))
        );

        await EnsureLinkContentTypeAsync();

        var existing = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteSettings").FirstOrDefaultAsync();
        if (existing is null)
        {
            var item = await _contentManager.NewAsync("SiteSettings");
            item.DisplayText = "Credit Plus Site Settings";
            item.Content["TitlePart"] = new JsonObject { ["Title"] = "Credit Plus Site Settings" };
            item.Content["AliasPart"] = new JsonObject { ["Alias"] = "global-site-settings" };
            item.Content["SiteSettingsPart"] = new JsonObject
            {
                ["ContactEmail"] = Field("support@credit-plus.me"),
                ["PhonePrimary"] = Field("+962 79 600 8900"),
                ["PhoneSecondary"] = Field("+962 79 816 2208"),
                ["AddressEn"] = Field("King Hussein Business Park (KHBP), Core Building, First Floor, Amman, Jordan"),
                ["AddressAr"] = Field("مجمع الملك الحسين للأعمال، مبنى Core، الطابق الأول، عمّان، الأردن"),
                ["ContactIntroEn"] = Field("We'd love to hear from you. For inquiries, partnerships, or support, feel free to reach out using the details below."),
                ["ContactIntroAr"] = Field("يسعدنا التواصل معكم. للاستفسارات أو الشراكات أو الدعم، تواصلوا معنا عبر التفاصيل أدناه."),
                ["FooterDescriptionEn"] = Field("Credit Plus helps businesses manage and grow their cash flow."),
                ["FooterDescriptionAr"] = Field("كريديت بلس تساعد الشركات على إدارة وتنمية تدفقاتها النقدية."),
                ["LoginUrl"] = Field("https://cp.credit-plus.me/Account/Login"),
                ["SignupUrl"] = Field("https://cp.credit-plus.me/Account/OnBoarding"),
                ["LinkedInUrl"] = Field("https://www.linkedin.com/company/credit-plus-me/"),
                ["HeaderLinks"] = Field("[]"),
                ["FooterLinks"] = Field("[]"),
                ["SocialLinks"] = Field("[]")
            };
            await _contentManager.CreateAsync(item, VersionOptions.Published);
        }

        await SeedLinksAsync();
        return 1;
    }

    public async Task<int> UpdateFrom1Async()
    {
        await EnsureLinkContentTypeAsync();
        await SeedLinksAsync();

        await _definitions.AlterPartDefinitionAsync("SiteSettingsPart", part => part
            .RemoveField("HeaderLinks")
            .RemoveField("FooterLinks")
            .RemoveField("SocialLinks"));

        return 2;
    }

    public async Task<int> UpdateFrom2Async()
    {
        await _definitions.AlterPartDefinitionAsync("SiteSettingsPart", part => part
            .WithField("HeaderLogo", field => field.OfType("MediaField").WithDisplayName("Header logo"))
            .WithField("FooterLogo", field => field.OfType("MediaField").WithDisplayName("Footer logo"))
        );

        await EnsurePagePlacementContentTypeAsync();

        var settings = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteSettings" && x.Published).FirstOrDefaultAsync();
        if (settings is not null)
        {
            await _contentManager.LoadAsync(settings);
            var part = settings.Content["SiteSettingsPart"] as JsonObject;
            if (part is not null)
            {
                part["HeaderLogo"] ??= MediaField("credit-plus/header-logo.png", "Credit Plus header logo");
                part["FooterLogo"] ??= MediaField("credit-plus/footer-logo.png", "Credit Plus footer logo");
                _session.Save(settings);
            }
        }

        return 3;
    }

    private async Task EnsureLinkContentTypeAsync()
    {
        await _definitions.AlterTypeDefinitionAsync("SiteLink", type => type
            .WithDisplayName("Site Link")
            .Creatable()
            .Listable()
            .WithPart("TitlePart")
            .WithPart("SiteLinkPart"));

        await _definitions.AlterPartDefinitionAsync("SiteLinkPart", part => part
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic label"))
            .WithField("Url", field => field.OfType("TextField").WithDisplayName("Link URL"))
            .WithField("Placement", field => field.OfType("TextField").WithDisplayName("Location (Header, Footer, Social)"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order"))
        );
    }

    private async Task EnsurePagePlacementContentTypeAsync()
    {
        await _definitions.AlterTypeDefinitionAsync("PagePlacement", type => type
            .WithDisplayName("Page Phone or Link")
            .Creatable()
            .Listable()
            .WithPart("TitlePart")
            .WithPart("PagePlacementPart"));

        await _definitions.AlterPartDefinitionAsync("PagePlacementPart", part => part
            .WithField("PagePath", field => field.OfType("TextField").WithDisplayName("Page path (example: /about)"))
            .WithField("ElementType", field => field.OfType("TextField").WithDisplayName("Type (Phone or Link)"))
            .WithField("LabelEn", field => field.OfType("TextField").WithDisplayName("English label"))
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic label"))
            .WithField("Value", field => field.OfType("TextField").WithDisplayName("Phone number or URL"))
            .WithField("Placement", field => field.OfType("TextField").WithDisplayName("Position (Top or Bottom)"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order"))
        );
    }

    private async Task SeedLinksAsync()
    {
        var existingLinks = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteLink").FirstOrDefaultAsync();
        if (existingLinks is not null) return;

        var links = new[]
        {
            new LinkSeed("Home", "الرئيسية", "/home", "Header", 1),
            new LinkSeed("About Us", "من نحن", "/about", "Header", 2),
            new LinkSeed("Solutions for Suppliers", "حلول للموردين", "/suppliers", "Header", 3),
            new LinkSeed("Advantages for Buyers", "مزايا للمشترين", "/buyers", "Header", 4),
            new LinkSeed("Calculator", "الحاسبة", "/calculator", "Header", 5),
            new LinkSeed("Contact Us", "تواصل معنا", "/contact", "Header", 6),
            new LinkSeed("Privacy Policy", "سياسة الخصوصية", "/privacy-policy", "Footer", 1),
            new LinkSeed("News", "الأخبار", "/news", "Footer", 2),
            new LinkSeed("Articles", "المقالات", "/articles", "Footer", 3),
            new LinkSeed("LinkedIn", "لينكدإن", "https://www.linkedin.com/company/credit-plus-me/", "Social", 1)
        };

        foreach (var link in links)
        {
            var item = await _contentManager.NewAsync("SiteLink");
            item.DisplayText = link.LabelEn;
            item.Content["TitlePart"] = new JsonObject { ["Title"] = link.LabelEn };
            item.Content["SiteLinkPart"] = new JsonObject
            {
                ["LabelAr"] = Field(link.LabelAr),
                ["Url"] = Field(link.Url),
                ["Placement"] = Field(link.Placement),
                ["SortOrder"] = Field(link.SortOrder.ToString())
            };
            await _contentManager.CreateAsync(item, VersionOptions.Published);
        }
    }

    private static JsonObject Field(string? value) => new() { ["Text"] = value };

    private static JsonObject MediaField(string path, string altText) => new()
    {
        ["Paths"] = new JsonArray { JsonValue.Create(path) },
        ["MediaTexts"] = new JsonArray { JsonValue.Create(altText) }
    };

    private sealed record LinkSeed(string LabelEn, string LabelAr, string Url, string Placement, int SortOrder);
}
