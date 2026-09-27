using Microsoft.AspNetCore.Mvc;
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
        var item = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteSettings" && x.Published).FirstOrDefaultAsync();
        if (item is null) return NotFound(new { message = "Create and publish a Site Settings item in Orchard Core." });
        await _contentManager.LoadAsync(item);
        var part = item.Content["SiteSettingsPart"];
        string? Text(string key) => part?[key]?["Text"]?.ToString();
        string? MediaUrl(string key)
        {
            var path = part?[key]?["Paths"]?.AsArray().FirstOrDefault()?.ToString();
            if (string.IsNullOrWhiteSpace(path)) return null;
            var encodedPath = string.Join("/", path.Trim('/').Split('/').Select(Uri.EscapeDataString));
            return $"/media/{encodedPath}";
        }

        var linkItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "SiteLink" && x.Published).ListAsync();
        var links = linkItems
            .Select(link =>
            {
                var linkPart = link.Content["SiteLinkPart"];
                string? LinkText(string key) => linkPart?[key]?["Text"]?.ToString();
                return new
                {
                    labelEn = link.Content["TitlePart"]?["Title"]?.ToString() ?? link.DisplayText,
                    labelAr = LinkText("LabelAr") ?? "",
                    url = LinkText("Url") ?? "",
                    placement = LinkText("Placement") ?? "",
                    sortOrder = int.TryParse(LinkText("SortOrder"), out var order) ? order : 0
                };
            })
            .Where(link => !string.IsNullOrWhiteSpace(link.url))
            .OrderBy(link => link.sortOrder)
            .ToArray();

        var pagePlacementItems = await _session.Query<ContentItem, ContentItemIndex>(x => x.ContentType == "PagePlacement" && x.Published).ListAsync();
        var pagePlacements = pagePlacementItems
            .Select(pageItem =>
            {
                var pagePart = pageItem.Content["PagePlacementPart"];
                string? PageText(string key) => pagePart?[key]?["Text"]?.ToString();
                return new
                {
                    id = pageItem.ContentItemId,
                    pagePath = PageText("PagePath") ?? "",
                    elementType = PageText("ElementType") ?? "",
                    labelEn = PageText("LabelEn") ?? "",
                    labelAr = PageText("LabelAr") ?? "",
                    value = PageText("Value") ?? "",
                    placement = PageText("Placement") ?? "Bottom",
                    sortOrder = int.TryParse(PageText("SortOrder"), out var order) ? order : 0
                };
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.pagePath) &&
                            !string.IsNullOrWhiteSpace(entry.value) &&
                            (entry.elementType.Equals("Phone", StringComparison.OrdinalIgnoreCase) ||
                             entry.elementType.Equals("Link", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(entry => entry.sortOrder)
            .ToArray();

        return Ok(new
        {
            contactEmail = Text("ContactEmail"),
            phones = new[] { Text("PhonePrimary"), Text("PhoneSecondary") }.Where(x => !string.IsNullOrWhiteSpace(x)),
            address = new { en = Text("AddressEn"), ar = Text("AddressAr") },
            contactIntro = new { en = Text("ContactIntroEn"), ar = Text("ContactIntroAr") },
            footerDescription = new { en = Text("FooterDescriptionEn"), ar = Text("FooterDescriptionAr") },
            headerLogo = MediaUrl("HeaderLogo"),
            footerLogo = MediaUrl("FooterLogo"),
            loginUrl = Text("LoginUrl"), signupUrl = Text("SignupUrl"), linkedInUrl = Text("LinkedInUrl"),
            headerLinks = links.Where(link => link.placement.Equals("Header", StringComparison.OrdinalIgnoreCase)).Select(link => new { link.labelEn, link.labelAr, link.url }),
            footerLinks = links.Where(link => link.placement.Equals("Footer", StringComparison.OrdinalIgnoreCase)).Select(link => new { link.labelEn, link.labelAr, link.url }),
            socialLinks = links.Where(link => link.placement.Equals("Social", StringComparison.OrdinalIgnoreCase)).Select(link => new { link.labelEn, link.labelAr, link.url }),
            pagePlacements
        });
    }
}
