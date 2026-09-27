# Orchard Core CMS

The Credit Plus website reads its shared site settings and navigation targets from Orchard Core.

## Open the CMS

Start Orchard with:

dotnet run --project orchard-cms/CreditPlus.Cms.Web/CreditPlus.Cms.Web.csproj

Open the admin dashboard at http://localhost:3001/Admin and choose **Credit Plus** in the left navigation.

- **Site settings — logo, email and phones**: logo images, logo click destinations, support email, contact phone numbers, address and map link, Contact Us introduction, footer description, and login/signup URLs.
- **Header links**: each navigation item shown in the website header.
- **Footer links**: footer links, grouped into Explore, Company, and Contact columns.
- **Social links**: LinkedIn and any other social destinations.
- **Page links by page**: each page has one CMS item containing all of that page's button and link destinations. Open **Home page** to edit the Home links together, or choose About, Buyers, Calculator, Article, or Custom pages. Publish the page item after editing its destination fields.
- **Page phone and extra links**: add a phone number, email link, or extra link to any route. Set its page route and choose whether it appears at the top or bottom of the page.

In Orchard's content editor, save and publish each change for it to appear on the website. Internal destinations can be local routes such as /buyers; external destinations can use a full URL. Email and phone destinations can use mailto:address@example.com and tel:+962....

## Manage the logos

The Header logo and Footer logo fields use Orchard's Media Picker. The current Credit Plus logo images are in Orchard's Media Library under the credit-plus folder. Upload a replacement in Media → Media Library, select it in Site Settings, and publish.

## Orchard Core tools

Media, Menu, Navigation, Content Preview, Queries, SEO, Sitemaps, Content Localization, Audit Trail, and Workflows are enabled.

The local database and tenant content are stored under orchard-cms/CreditPlus.Cms.Web/App_Data and are ignored by Git. Back up that folder to retain CMS content and the administrator account.
