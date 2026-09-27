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
        await EnsureDefaultLogosAsync();
        return 3;
    }

    public async Task<int> UpdateFrom3Async()
    {
        await EnsureDefaultLogosAsync();
        return 4;
    }

    public async Task<int> UpdateFrom4Async()
    {
        await EnsureDefaultLogosAsync();
        return 5;
    }

    public async Task<int> UpdateFrom5Async()
    {
        await EnsureSiteSettingsLinkFieldsAsync();
        await EnsureManagedLinkTypesAsync();
        await EnsurePageLinkContentTypeAsync();
        await MoveLegacySiteLinksAsync();
        await SeedFooterLinksAsync();
        await SeedSocialLinksAsync();
        await SeedPageLinksAsync();
        return 6;
    }

    public async Task<int> UpdateFrom6Async()
    {
        await EnsureSiteSettingsLinkFieldsAsync();
        await RemoveObsoletePageLinkAsync("buyers.final.calculator");
        return 7;
    }

    public async Task<int> UpdateFrom7Async()
    {
        await EnsureGroupedPageLinkTypesAsync();
        await MigratePageLinksIntoPageItemsAsync();
        return 8;
    }

    public async Task<int> UpdateFrom8Async()
    {
        await EnsureSiteSettingsLinkFieldsAsync();
        await EnsureDefaultLogosAsync();
        return 9;
    }

    private async Task EnsureGroupedPageLinkTypesAsync()
    {
        foreach (var group in PageLinkGroups)
        {
            await _definitions.AlterTypeDefinitionAsync(group.ContentType, type => type
                .WithDisplayName(group.DisplayName).Creatable().Listable()
                .WithPart("TitlePart").WithPart(group.PartName));

            await _definitions.AlterPartDefinitionAsync(group.PartName, part =>
            {
                foreach (var field in group.Fields)
                {
                    part.WithField(field.FieldName, definition => definition
                        .OfType("TextField").WithDisplayName(field.DisplayName));
                }
            });
        }
    }

    private async Task MigratePageLinksIntoPageItemsAsync()
    {
        var oldItems = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "PageLink" && x.Published).ListAsync();
        var destinations = PageLinkGroups.ToDictionary(
            group => group.ContentType,
            group => group.Fields.ToDictionary(field => field.LinkKey, field => field.DefaultUrl, StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (ContentItem oldItem in oldItems)
        {
            await _contentManager.LoadAsync(oldItem);
            var oldPart = oldItem.Content["PageLinkPart"];
            var key = oldPart?["LinkKey"]?["Text"]?.ToString();
            var url = oldPart?["Url"]?["Text"]?.ToString();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(url)) continue;

            var group = PageLinkGroups.FirstOrDefault(candidate => candidate.Fields.Any(field => field.LinkKey == key));
            if (group is not null) destinations[group.ContentType][key] = url;
        }

        foreach (var group in PageLinkGroups)
        {
            var item = await _session.Query<ContentItem, ContentItemIndex>(
                x => x.ContentType == group.ContentType && x.Published).FirstOrDefaultAsync();
            var isNew = item is null;
            item ??= await _contentManager.NewAsync(group.ContentType);
            if (isNew)
            {
                item.DisplayText = group.DisplayName;
                item.Content["TitlePart"] = new JsonObject { ["Title"] = group.DisplayName };
            }

            var part = item.Content[group.PartName] as JsonObject ?? new JsonObject();
            foreach (var field in group.Fields)
            {
                var existing = part[field.FieldName]?["Text"]?.ToString();
                if (string.IsNullOrWhiteSpace(existing))
                {
                    part[field.FieldName] = Field(destinations[group.ContentType][field.LinkKey]);
                }
            }
            item.Content[group.PartName] = part;

            if (isNew)
            {
                await _contentManager.CreateAsync(item, VersionOptions.Published);
            }
            else
            {
                await _contentManager.UpdateAsync(item);
            }
        }

        foreach (ContentItem oldItem in oldItems)
        {
            await _contentManager.RemoveAsync(oldItem);
        }
    }

    private async Task EnsureDefaultLogosAsync()
    {
        var settings = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteSettings" && x.Published).FirstOrDefaultAsync();
        if (settings is null) return;

        await _contentManager.LoadAsync(settings);
        var part = settings.Content["SiteSettingsPart"] as JsonObject;
        if (part is null) return;
        EnsureMediaField(part, "HeaderLogo", "credit-plus/header-logo.png", "Credit Plus header logo");
        EnsureMediaField(part, "FooterLogo", "credit-plus/footer-logo.png", "Credit Plus footer logo");
        await _contentManager.UpdateAsync(settings);
        await _contentManager.PublishAsync(settings);
    }
    private static void EnsureMediaField(JsonObject part, string fieldName, string path, string altText)
    {
        JsonArray? paths = part[fieldName]?["Paths"] as JsonArray;
        var selectedPath = paths?.FirstOrDefault()?.ToString();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            part[fieldName] = MediaField(path, altText);
        }
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

    private async Task EnsureSiteSettingsLinkFieldsAsync()
    {
        await _definitions.AlterPartDefinitionAsync("SiteSettingsPart", part => part
            .WithField("HeaderLogoUrl", field => field.OfType("TextField").WithDisplayName("Header logo destination"))
            .WithField("FooterLogoUrl", field => field.OfType("TextField").WithDisplayName("Footer logo destination"))
            .WithField("AddressMapUrl", field => field.OfType("TextField").WithDisplayName("Address map URL")));

        var settings = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteSettings" && x.Published).FirstOrDefaultAsync();
        if (settings is null) return;

        await _contentManager.LoadAsync(settings);
        var part = settings.Content["SiteSettingsPart"] as JsonObject;
        if (part is null) return;
        string? Text(string key) => part[key]?["Text"]?.ToString();
        if (string.IsNullOrWhiteSpace(Text("HeaderLogoUrl"))) part["HeaderLogoUrl"] = Field("/home");
        if (string.IsNullOrWhiteSpace(Text("FooterLogoUrl"))) part["FooterLogoUrl"] = Field("/home");
        if (string.IsNullOrWhiteSpace(Text("AddressMapUrl")))
        {
            part["AddressMapUrl"] = Field("https://www.google.com/maps/search/?api=1&query=King+Hussein+Business+Park+Amman+Jordan");
        }
        await _contentManager.UpdateAsync(settings);
        await _contentManager.PublishAsync(settings);
    }

    private async Task EnsureManagedLinkTypesAsync()
    {
        await _definitions.AlterTypeDefinitionAsync("HeaderLink", type => type
            .WithDisplayName("Header Link").Creatable().Listable()
            .WithPart("TitlePart").WithPart("HeaderLinkPart"));
        await _definitions.AlterPartDefinitionAsync("HeaderLinkPart", part => part
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic label"))
            .WithField("Url", field => field.OfType("TextField").WithDisplayName("Destination URL"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order")));

        await _definitions.AlterTypeDefinitionAsync("FooterLink", type => type
            .WithDisplayName("Footer Link").Creatable().Listable()
            .WithPart("TitlePart").WithPart("FooterLinkPart"));
        await _definitions.AlterPartDefinitionAsync("FooterLinkPart", part => part
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic label"))
            .WithField("Url", field => field.OfType("TextField").WithDisplayName("Destination URL"))
            .WithField("Section", field => field.OfType("TextField").WithDisplayName("Footer column (Explore, Company, Contact)"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order")));

        await _definitions.AlterTypeDefinitionAsync("SocialLink", type => type
            .WithDisplayName("Social Link").Creatable().Listable()
            .WithPart("TitlePart").WithPart("SocialLinkPart"));
        await _definitions.AlterPartDefinitionAsync("SocialLinkPart", part => part
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic label"))
            .WithField("Url", field => field.OfType("TextField").WithDisplayName("Destination URL"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order")));
    }

    private async Task EnsurePageLinkContentTypeAsync()
    {
        await _definitions.AlterTypeDefinitionAsync("PageLink", type => type
            .WithDisplayName("Page Link (Button or Link)").Creatable().Listable()
            .WithPart("TitlePart").WithPart("PageLinkPart"));

        await _definitions.AlterPartDefinitionAsync("PageLinkPart", part => part
            .WithField("PagePath", field => field.OfType("TextField").WithDisplayName("Page route (example: /home)"))
            .WithField("LinkKey", field => field.OfType("TextField").WithDisplayName("Link key (keep unchanged)"))
            .WithField("LabelAr", field => field.OfType("TextField").WithDisplayName("Arabic description"))
            .WithField("Url", field => field.OfType("TextField").WithDisplayName("Button or link destination"))
            .WithField("SortOrder", field => field.OfType("TextField").WithDisplayName("Display order")));
    }

    private async Task MoveLegacySiteLinksAsync()
    {
        var legacyLinks = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteLink").ListAsync();
        foreach (var legacy in legacyLinks)
        {
            await _contentManager.LoadAsync(legacy);
            var part = legacy.Content["SiteLinkPart"];
            string? Text(string key) => part?[key]?["Text"]?.ToString();

            var placement = Text("Placement") ?? "Header";
            var type = placement.Equals("Footer", StringComparison.OrdinalIgnoreCase) ? "FooterLink"
                : placement.Equals("Social", StringComparison.OrdinalIgnoreCase) ? "SocialLink"
                : "HeaderLink";
            var labelEn = legacy.Content["TitlePart"]?["Title"]?.ToString() ?? legacy.DisplayText;
            var labelAr = Text("LabelAr") ?? "";
            var url = Text("Url") ?? "";
            var sortOrder = Text("SortOrder") ?? "0";

            var item = await _contentManager.NewAsync(type);
            item.DisplayText = labelEn;
            item.Content["TitlePart"] = new JsonObject { ["Title"] = labelEn };
            var partName = type switch
            {
                "FooterLink" => "FooterLinkPart",
                "SocialLink" => "SocialLinkPart",
                _ => "HeaderLinkPart"
            };
            var newPart = new JsonObject
            {
                ["LabelAr"] = Field(labelAr),
                ["Url"] = Field(url),
                ["SortOrder"] = Field(sortOrder)
            };
            if (type == "FooterLink")
            {
                var section = labelEn.Equals("News", StringComparison.OrdinalIgnoreCase) ||
                              labelEn.Equals("Articles", StringComparison.OrdinalIgnoreCase)
                    ? "Company" : "Explore";
                newPart["Section"] = Field(section);
            }
            item.Content[partName] = newPart;
            await _contentManager.CreateAsync(item, VersionOptions.Published);
            await _contentManager.RemoveAsync(legacy);
        }
    }

    private async Task SeedFooterLinksAsync()
    {
        var existing = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "FooterLink" && x.Published).ListAsync();
        var existingTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ContentItem existingItem in existing)
        {
            var title = existingItem.Content["TitlePart"]?["Title"]?.ToString() ?? existingItem.DisplayText;
            if (!string.IsNullOrWhiteSpace(title)) existingTitles.Add(title);
        }
        var seeds = new[]
        {
            new LinkSeed("Home", "الرئيسية", "/home", "Explore", 1),
            new LinkSeed("About Us", "من نحن", "/about", "Explore", 2),
            new LinkSeed("Solutions for Suppliers", "حلول للموردين", "/suppliers", "Explore", 3),
            new LinkSeed("Advantages for Buyers", "مزايا للمشترين", "/buyers", "Explore", 4),
            new LinkSeed("Calculator", "الحاسبة", "/calculator", "Explore", 5),
            new LinkSeed("Contact Us", "تواصل معنا", "/contact", "Explore", 6),
            new LinkSeed("Privacy Policy", "سياسة الخصوصية", "/privacy-policy", "Explore", 7),
            new LinkSeed("News", "الأخبار", "/news", "Company", 1),
            new LinkSeed("Articles", "المقالات", "/articles", "Company", 2),
            new LinkSeed("Login", "تسجيل الدخول", "https://cp.credit-plus.me/Account/Login", "Company", 3),
            new LinkSeed("Sign Up", "إنشاء حساب", "https://cp.credit-plus.me/Account/OnBoarding", "Company", 4),
            new LinkSeed("marketing", "التسويق", "/pages/manage", "Contact", 1)
        };

        foreach (var seed in seeds)
        {
            if (existingTitles.Contains(seed.LabelEn)) continue;
            var item = await _contentManager.NewAsync("FooterLink");
            item.DisplayText = seed.LabelEn;
            item.Content["TitlePart"] = new JsonObject { ["Title"] = seed.LabelEn };
            item.Content["FooterLinkPart"] = new JsonObject
            {
                ["LabelAr"] = Field(seed.LabelAr),
                ["Url"] = Field(seed.Url),
                ["Section"] = Field(seed.Placement),
                ["SortOrder"] = Field(seed.SortOrder.ToString())
            };
            await _contentManager.CreateAsync(item, VersionOptions.Published);
        }
    }

    private async Task RemoveObsoletePageLinkAsync(string linkKey)
    {
        var links = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "PageLink" && x.Published).ListAsync();
        foreach (ContentItem link in links)
        {
            await _contentManager.LoadAsync(link);
            var key = link.Content["PageLinkPart"]?["LinkKey"]?["Text"]?.ToString();
            if (string.Equals(key, linkKey, StringComparison.Ordinal))
            {
                await _contentManager.RemoveAsync(link);
            }
        }
    }

    private async Task SeedSocialLinksAsync()
    {
        var exists = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SocialLink" && x.Published).FirstOrDefaultAsync();
        if (exists is not null) return;

        var item = await _contentManager.NewAsync("SocialLink");
        item.DisplayText = "LinkedIn";
        item.Content["TitlePart"] = new JsonObject { ["Title"] = "LinkedIn" };
        item.Content["SocialLinkPart"] = new JsonObject
        {
            ["LabelAr"] = Field("لينكدإن"),
            ["Url"] = Field("https://www.linkedin.com/company/credit-plus-me/"),
            ["SortOrder"] = Field("1")
        };
        await _contentManager.CreateAsync(item, VersionOptions.Published);
    }

    private async Task SeedPageLinksAsync()
    {
        var exists = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "PageLink").FirstOrDefaultAsync();
        if (exists is not null) return;

        var links = new[]
        {
            new PageLinkSeed("/home", "home.hero.suppliers", "Home — Hero: Suppliers button", "الرئيسية — زر الموردين", "/suppliers", 1),
            new PageLinkSeed("/home", "home.hero.buyers", "Home — Hero: Buyers button", "الرئيسية — زر المشترين", "/buyers", 2),
            new PageLinkSeed("/home", "home.hero.institutions", "Home — Hero: Financial institutions button", "الرئيسية — زر المؤسسات المالية", "/about", 3),
            new PageLinkSeed("/home", "home.solutions.buyers", "Home — Solutions: Buyers card", "الرئيسية — بطاقة المشترين", "/buyers", 4),
            new PageLinkSeed("/home", "home.solutions.suppliers", "Home — Solutions: Suppliers card", "الرئيسية — بطاقة الموردين", "/suppliers", 5),
            new PageLinkSeed("/home", "home.solutions.banks", "Home — Solutions: Financial institutions card", "الرئيسية — بطاقة المؤسسات المالية", "/about", 6),
            new PageLinkSeed("/home", "home.solutions.dynamicDiscounting", "Home — Solutions: Dynamic discounting card", "الرئيسية — بطاقة الخصم الديناميكي", "/buyers#dynamic-discounting", 7),
            new PageLinkSeed("/home", "home.suppliers.learnMore", "Home — Suppliers: Learn more button", "الرئيسية — زر اعرف المزيد للموردين", "/suppliers", 8),
            new PageLinkSeed("/home", "home.buyers.learnMore", "Home — Buyers: Learn more button", "الرئيسية — زر اعرف المزيد للمشترين", "/buyers", 9),
            new PageLinkSeed("/home", "home.cta.about", "Home — Final: About button", "الرئيسية — الزر الختامي لمن نحن", "/about", 10),
            new PageLinkSeed("/about", "about.companyProfile.download", "About — Download company profile", "من نحن — تنزيل ملف الشركة", "/Credit-Plus-Company-Profile-2025.pdf", 1),
            new PageLinkSeed("/buyers", "buyers.dynamic.registerInterest", "Buyers — Dynamic discounting: Register interest", "المشترون — الخصم الديناميكي: سجل اهتمامك", "/contact", 1),
            new PageLinkSeed("/buyers", "buyers.dynamic.calculator", "Buyers — Dynamic discounting: Calculator", "المشترون — الخصم الديناميكي: الحاسبة", "/calculator", 2),
            new PageLinkSeed("/buyers", "buyers.final.registerInterest", "Buyers — Final: Register interest", "المشترون — الزر الختامي للتسجيل", "/contact", 3),
            new PageLinkSeed("/calculator", "calculator.hero.jump", "Calculator — Jump to calculator", "الحاسبة — الانتقال إلى الحاسبة", "/calculator#calc-anchor", 1),
            new PageLinkSeed("/calculator", "calculator.final.contact", "Calculator — Ask us by email", "الحاسبة — تواصل معنا عبر البريد", "mailto:support@credit-plus.me?subject=Credit%20Plus%20Question", 2),
            new PageLinkSeed("/calculator", "calculator.regulatory.email", "Calculator — Regulatory notice email", "الحاسبة — بريد التواصل في التنويه", "mailto:support@credit-plus.me", 3),
            new PageLinkSeed("/article/:id", "article.backHome", "Article — Back to Home button", "المقال — زر العودة للرئيسية", "/home", 1),
            new PageLinkSeed("/page/:id", "customPage.backHome", "Custom page — Back to Home button", "صفحة مخصصة — زر العودة للرئيسية", "/home", 1)
        };

        foreach (var link in links)
        {
            var item = await _contentManager.NewAsync("PageLink");
            item.DisplayText = link.LabelEn;
            item.Content["TitlePart"] = new JsonObject { ["Title"] = link.LabelEn };
            item.Content["PageLinkPart"] = new JsonObject
            {
                ["PagePath"] = Field(link.PagePath),
                ["LinkKey"] = Field(link.LinkKey),
                ["LabelAr"] = Field(link.LabelAr),
                ["Url"] = Field(link.Url),
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

    private static readonly PageLinkGroup[] PageLinkGroups =
    [
        new("HomePageLinks", "Home page links", "HomePageLinksPart", "/home",
        [
            new("home.hero.suppliers", "Hero — Suppliers button", "SuppliersButtonUrl", "/suppliers"),
            new("home.hero.buyers", "Hero — Buyers button", "BuyersButtonUrl", "/buyers"),
            new("home.hero.institutions", "Hero — Financial institutions button", "InstitutionsButtonUrl", "/about"),
            new("home.solutions.buyers", "Solutions — Buyers card", "BuyersCardUrl", "/buyers"),
            new("home.solutions.suppliers", "Solutions — Suppliers card", "SuppliersCardUrl", "/suppliers"),
            new("home.solutions.banks", "Solutions — Financial institutions card", "InstitutionsCardUrl", "/about"),
            new("home.solutions.dynamicDiscounting", "Solutions — Dynamic discounting card", "DynamicDiscountingCardUrl", "/buyers#dynamic-discounting"),
            new("home.suppliers.learnMore", "Suppliers — Learn more button", "SuppliersLearnMoreUrl", "/suppliers"),
            new("home.buyers.learnMore", "Buyers — Learn more button", "BuyersLearnMoreUrl", "/buyers"),
            new("home.cta.about", "Closing section — About button", "AboutButtonUrl", "/about")
        ]),
        new("AboutPageLinks", "About page links", "AboutPageLinksPart", "/about",
        [
            new("about.companyProfile.download", "Download company profile", "CompanyProfileDownloadUrl", "/Credit-Plus-Company-Profile-2025.pdf")
        ]),
        new("BuyersPageLinks", "Buyers page links", "BuyersPageLinksPart", "/buyers",
        [
            new("buyers.dynamic.registerInterest", "Dynamic discounting — Register interest", "DynamicRegisterInterestUrl", "/contact"),
            new("buyers.dynamic.calculator", "Dynamic discounting — Calculator", "DynamicCalculatorUrl", "/calculator"),
            new("buyers.final.registerInterest", "Closing section — Register interest", "FinalRegisterInterestUrl", "/contact")
        ]),
        new("CalculatorPageLinks", "Calculator page links", "CalculatorPageLinksPart", "/calculator",
        [
            new("calculator.hero.jump", "Jump to calculator", "JumpToCalculatorUrl", "/calculator#calc-anchor"),
            new("calculator.final.contact", "Ask us by email", "ContactByEmailUrl", "mailto:support@credit-plus.me?subject=Credit%20Plus%20Question"),
            new("calculator.regulatory.email", "Regulatory notice email", "RegulatoryNoticeEmailUrl", "mailto:support@credit-plus.me")
        ]),
        new("ArticlePageLinks", "Article page links", "ArticlePageLinksPart", "/article/:id",
        [
            new("article.backHome", "Back to Home button", "BackHomeUrl", "/home")
        ]),
        new("CustomPageLinks", "Custom page links", "CustomPageLinksPart", "/page/:id",
        [
            new("customPage.backHome", "Back to Home button", "BackHomeUrl", "/home")
        ])
    ];

    private sealed record PageLinkGroup(string ContentType, string DisplayName, string PartName, string PagePath, PageLinkField[] Fields);
    private sealed record PageLinkField(string LinkKey, string DisplayName, string FieldName, string DefaultUrl);
    private sealed record LinkSeed(string LabelEn, string LabelAr, string Url, string Placement, int SortOrder);
    private sealed record PageLinkSeed(string PagePath, string LinkKey, string LabelEn, string LabelAr, string Url, int SortOrder);
}
