using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace CreditPlus.SiteContent.Controllers;

[ApiController]
[Route("cms-api/site-settings")]
public sealed class SiteSettingsController : ControllerBase
{
    private readonly IContentManager _contentManager;
    private readonly ISession _session;

    public SiteSettingsController(IContentManager contentManager, ISession session)
    {
        _contentManager = contentManager;
        _session = session;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var item = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "SiteSettings" && x.Published).FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound(new { message = "Create and publish a Site Settings item in Orchard Core." });
        }

        await _contentManager.LoadAsync(item);
        var part = item.Content["SiteSettingsPart"];
        string? Text(string key) => part?[key]?["Text"]?.ToString();

        string? MediaUrl(string key)
        {
            var mediaField = part?[key] ?? part?[char.ToLowerInvariant(key[0]) + key[1..]];
            var pathsNode = mediaField?["Paths"] ?? mediaField?["paths"];
            var paths = pathsNode as JsonArray;
            if (paths is null && pathsNode is JsonValue pathsValue && pathsValue.TryGetValue<string>(out var serializedPaths))
            {
                paths = JsonNode.Parse(serializedPaths) as JsonArray;
            }

            var firstPath = paths?.FirstOrDefault();
            var path = firstPath is JsonObject mediaItem
                ? mediaItem["path"]?.ToString() ?? mediaItem["Path"]?.ToString()
                : firstPath is JsonValue pathValue && pathValue.TryGetValue<string>(out var selectedPath)
                    ? selectedPath
                    : null;
            if (string.IsNullOrWhiteSpace(path)) return null;

            var encodedPath = string.Join("/", path.Trim('/').Split('/').Select(Uri.EscapeDataString));
            return $"/cms-media/{encodedPath}";
        }

        var headerLinks = await ReadLinksAsync("HeaderLink", "HeaderLinkPart");
        var footerLinks = await ReadLinksAsync("FooterLink", "FooterLinkPart");
        var socialLinks = await ReadLinksAsync("SocialLink", "SocialLinkPart");
        var pageLinks = await ReadPageLinksAsync();
        var pagePlacements = await ReadPagePlacementsAsync();

        // Keep older published SiteLink items readable if a deployment has not run the latest migration yet.
        if (headerLinks.Count == 0 || footerLinks.Count == 0 || socialLinks.Count == 0)
        {
            var legacy = await ReadLegacyLinksAsync();
            if (headerLinks.Count == 0) headerLinks = legacy.Where(x => x.Section == "Header").ToList();
            if (footerLinks.Count == 0) footerLinks = legacy.Where(x => x.Section == "Footer").ToList();
            if (socialLinks.Count == 0) socialLinks = legacy.Where(x => x.Section == "Social").ToList();
        }

        return Ok(new
        {
            contactEmail = Text("ContactEmail"),
            phones = new[] { Text("PhonePrimary"), Text("PhoneSecondary") }
                .Where(x => !string.IsNullOrWhiteSpace(x)),
            address = new { en = Text("AddressEn"), ar = Text("AddressAr") },
            addressMapUrl = Text("AddressMapUrl"),
            contactIntro = new { en = Text("ContactIntroEn"), ar = Text("ContactIntroAr") },
            footerDescription = new { en = Text("FooterDescriptionEn"), ar = Text("FooterDescriptionAr") },
            headerLogo = MediaUrl("HeaderLogo"),
            footerLogo = MediaUrl("FooterLogo"),
            headerLogoUrl = Text("HeaderLogoUrl"),
            footerLogoUrl = Text("FooterLogoUrl"),
            loginUrl = Text("LoginUrl"),
            signupUrl = Text("SignupUrl"),
            linkedInUrl = socialLinks.FirstOrDefault()?.Url ?? Text("LinkedInUrl"),
            headerLinks = headerLinks.Select(x => new { labelEn = x.LabelEn, labelAr = x.LabelAr, url = x.Url }),
            footerLinks = footerLinks.Select(x => new { labelEn = x.LabelEn, labelAr = x.LabelAr, url = x.Url, section = x.Section }),
            socialLinks = socialLinks.Select(x => new { labelEn = x.LabelEn, labelAr = x.LabelAr, url = x.Url }),
            pageLinks,
            pagePlacements
        });
    }

    private async Task<List<ManagedLink>> ReadLinksAsync(string contentType, string partName)
    {
        var items = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == contentType && x.Published).ListAsync();

        return items.Select(item =>
        {
            var part = item.Content[partName];
            string? Text(string key) => part?[key]?["Text"]?.ToString();
            var title = item.Content["TitlePart"]?["Title"]?.ToString() ?? item.DisplayText;
            return new ManagedLink(
                title,
                Text("LabelAr") ?? "",
                Text("Url") ?? "",
                Text("Section") ?? "",
                int.TryParse(Text("SortOrder"), out var order) ? order : 0);
        })
        .Where(link => !string.IsNullOrWhiteSpace(link.Url))
        .OrderBy(link => link.SortOrder)
        .ToList();
    }

    private async Task<List<ManagedLink>> ReadLegacyLinksAsync()
    {
        var items = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "SiteLink" && x.Published).ListAsync();

        return items.Select(item =>
        {
            var part = item.Content["SiteLinkPart"];
            string? Text(string key) => part?[key]?["Text"]?.ToString();
            var title = item.Content["TitlePart"]?["Title"]?.ToString() ?? item.DisplayText;
            return new ManagedLink(
                title,
                Text("LabelAr") ?? "",
                Text("Url") ?? "",
                Text("Placement") ?? "",
                int.TryParse(Text("SortOrder"), out var order) ? order : 0);
        })
        .Where(link => !string.IsNullOrWhiteSpace(link.Url))
        .OrderBy(link => link.SortOrder)
        .ToList();
    }

    private async Task<object[]> ReadPageLinksAsync()
    {
        var links = new List<object>();
        foreach (var definition in GroupedPageLinks)
        {
            var item = await _session.Query<ContentItem, ContentItemIndex>(
                x => x.ContentType == definition.ContentType && x.Published).FirstOrDefaultAsync();
            if (item is null) continue;

            await _contentManager.LoadAsync(item);
            var part = item.Content[definition.PartName];
            string? url = part?[definition.FieldName]?["Text"]?.ToString();
            if (string.IsNullOrWhiteSpace(url)) continue;

            links.Add(new
            {
                pagePath = definition.PagePath,
                linkKey = definition.LinkKey,
                labelEn = definition.LabelEn,
                labelAr = definition.LabelAr,
                url,
                sortOrder = definition.SortOrder
            });
        }

        return links.ToArray();
    }

    private async Task<object[]> ReadPagePlacementsAsync()
    {
        var items = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "PagePlacement" && x.Published).ListAsync();

        return items.Select(item =>
        {
            var part = item.Content["PagePlacementPart"];
            string? Text(string key) => part?[key]?["Text"]?.ToString();
            return new
            {
                id = item.ContentItemId,
                pagePath = Text("PagePath") ?? "",
                elementType = Text("ElementType") ?? "",
                labelEn = Text("LabelEn") ?? item.Content["TitlePart"]?["Title"]?.ToString() ?? item.DisplayText,
                labelAr = Text("LabelAr") ?? "",
                value = Text("Value") ?? "",
                placement = Text("Placement") ?? "Bottom",
                sortOrder = int.TryParse(Text("SortOrder"), out var order) ? order : 0
            };
        })
        .Where(entry => !string.IsNullOrWhiteSpace(entry.pagePath) &&
                        !string.IsNullOrWhiteSpace(entry.value) &&
                        (entry.elementType.Equals("Phone", StringComparison.OrdinalIgnoreCase) ||
                         entry.elementType.Equals("Link", StringComparison.OrdinalIgnoreCase)))
        .OrderBy(entry => entry.sortOrder)
        .Cast<object>()
        .ToArray();
    }

    private static readonly PageLinkDefinition[] GroupedPageLinks =
    [
        new("HomePageLinks", "HomePageLinksPart", "/home", "SuppliersButtonUrl", "home.hero.suppliers", "Home — Hero: Suppliers button", "الرئيسية — زر الموردين", 1),
        new("HomePageLinks", "HomePageLinksPart", "/home", "BuyersButtonUrl", "home.hero.buyers", "Home — Hero: Buyers button", "الرئيسية — زر المشترين", 2),
        new("HomePageLinks", "HomePageLinksPart", "/home", "InstitutionsButtonUrl", "home.hero.institutions", "Home — Hero: Financial institutions button", "الرئيسية — زر المؤسسات المالية", 3),
        new("HomePageLinks", "HomePageLinksPart", "/home", "BuyersCardUrl", "home.solutions.buyers", "Home — Solutions: Buyers card", "الرئيسية — بطاقة المشترين", 4),
        new("HomePageLinks", "HomePageLinksPart", "/home", "SuppliersCardUrl", "home.solutions.suppliers", "Home — Solutions: Suppliers card", "الرئيسية — بطاقة الموردين", 5),
        new("HomePageLinks", "HomePageLinksPart", "/home", "InstitutionsCardUrl", "home.solutions.banks", "Home — Solutions: Financial institutions card", "الرئيسية — بطاقة المؤسسات المالية", 6),
        new("HomePageLinks", "HomePageLinksPart", "/home", "DynamicDiscountingCardUrl", "home.solutions.dynamicDiscounting", "Home — Solutions: Dynamic discounting card", "الرئيسية — بطاقة الخصم الديناميكي", 7),
        new("HomePageLinks", "HomePageLinksPart", "/home", "SuppliersLearnMoreUrl", "home.suppliers.learnMore", "Home — Suppliers: Learn more button", "الرئيسية — زر اعرف المزيد للموردين", 8),
        new("HomePageLinks", "HomePageLinksPart", "/home", "BuyersLearnMoreUrl", "home.buyers.learnMore", "Home — Buyers: Learn more button", "الرئيسية — زر اعرف المزيد للمشترين", 9),
        new("HomePageLinks", "HomePageLinksPart", "/home", "AboutButtonUrl", "home.cta.about", "Home — Final: About button", "الرئيسية — الزر الختامي لمن نحن", 10),
        new("AboutPageLinks", "AboutPageLinksPart", "/about", "CompanyProfileDownloadUrl", "about.companyProfile.download", "About — Download company profile", "من نحن — تنزيل ملف الشركة", 1),
        new("BuyersPageLinks", "BuyersPageLinksPart", "/buyers", "DynamicRegisterInterestUrl", "buyers.dynamic.registerInterest", "Buyers — Dynamic discounting: Register interest", "المشترون — الخصم الديناميكي: سجل اهتمامك", 1),
        new("BuyersPageLinks", "BuyersPageLinksPart", "/buyers", "DynamicCalculatorUrl", "buyers.dynamic.calculator", "Buyers — Dynamic discounting: Calculator", "المشترون — الخصم الديناميكي: الحاسبة", 2),
        new("BuyersPageLinks", "BuyersPageLinksPart", "/buyers", "FinalRegisterInterestUrl", "buyers.final.registerInterest", "Buyers — Final: Register interest", "المشترون — الزر الختامي للتسجيل", 3),
        new("CalculatorPageLinks", "CalculatorPageLinksPart", "/calculator", "JumpToCalculatorUrl", "calculator.hero.jump", "Calculator — Jump to calculator", "الحاسبة — الانتقال إلى الحاسبة", 1),
        new("CalculatorPageLinks", "CalculatorPageLinksPart", "/calculator", "ContactByEmailUrl", "calculator.final.contact", "Calculator — Ask us by email", "الحاسبة — تواصل معنا عبر البريد", 2),
        new("CalculatorPageLinks", "CalculatorPageLinksPart", "/calculator", "RegulatoryNoticeEmailUrl", "calculator.regulatory.email", "Calculator — Regulatory notice email", "الحاسبة — بريد التواصل في التنويه", 3),
        new("ArticlePageLinks", "ArticlePageLinksPart", "/article/:id", "BackHomeUrl", "article.backHome", "Article — Back to Home button", "المقال — زر العودة للرئيسية", 1),
        new("CustomPageLinks", "CustomPageLinksPart", "/page/:id", "BackHomeUrl", "customPage.backHome", "Custom page — Back to Home button", "صفحة مخصصة — زر العودة للرئيسية", 1)
    ];

    private sealed record PageLinkDefinition(
        string ContentType, string PartName, string PagePath, string FieldName,
        string LinkKey, string LabelEn, string LabelAr, int SortOrder);
    private sealed record ManagedLink(string LabelEn, string LabelAr, string Url, string Section, int SortOrder);
}
