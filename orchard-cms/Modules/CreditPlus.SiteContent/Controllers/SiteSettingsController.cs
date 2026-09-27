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
            var paths = part?[key]?["Paths"] as JsonArray;
            var firstPath = paths?.FirstOrDefault();
            var path = firstPath is JsonValue pathValue ? pathValue.GetValue<string>() : null;
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
        var items = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "PageLink" && x.Published).ListAsync();

        return items.Select(item =>
        {
            var part = item.Content["PageLinkPart"];
            string? Text(string key) => part?[key]?["Text"]?.ToString();
            return new
            {
                pagePath = Text("PagePath") ?? "",
                linkKey = Text("LinkKey") ?? "",
                labelEn = item.Content["TitlePart"]?["Title"]?.ToString() ?? item.DisplayText,
                labelAr = Text("LabelAr") ?? "",
                url = Text("Url") ?? "",
                sortOrder = int.TryParse(Text("SortOrder"), out var order) ? order : 0
            };
        })
        .Where(link => !string.IsNullOrWhiteSpace(link.pagePath) &&
                       !string.IsNullOrWhiteSpace(link.linkKey) &&
                       !string.IsNullOrWhiteSpace(link.url))
        .OrderBy(link => link.sortOrder)
        .Cast<object>()
        .ToArray();
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

    private sealed record ManagedLink(string LabelEn, string LabelAr, string Url, string Section, int SortOrder);
}
