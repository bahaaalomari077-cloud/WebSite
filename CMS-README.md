# Orchard Core CMS

The Credit Plus Angular site reads its shared settings, logos, navigation links, and page-specific phone/link items from Orchard Core.

## Run locally

From the site folder, run **dotnet run --project orchard-cms/CreditPlus.Cms.Web/CreditPlus.Cms.Web.csproj** to start Orchard, then run **npm start** in another terminal for Angular.

Open the website at http://localhost:4200 and Orchard admin at http://localhost:3001/Admin. The Angular proxy forwards /cms-api and /cms-media to Orchard on port 3001.

## Manage the logos

Open Content → Content Items → Credit Plus Site Settings. The Header logo and Footer logo fields use Orchard's Media Picker. The current Credit Plus logo images are already in Orchard's Media Library under the credit-plus folder. Upload replacement files in Media → Media Library, then select them in the settings item and publish.

## Add a phone number or link to any page

Open Content → Content Items → New → Page Phone or Link. Create one item for each phone number or link:

- Title: English label shown on the page
- Page path: the route to show it on, for example /about or /contact
- Type: Phone or Link
- English label and Arabic label: optional display text
- Phone number or URL: a phone number for Phone, or a local route/full URL for Link
- Position: Top or Bottom
- Display order: numeric ordering when a page has multiple items

Dynamic routes can use a parameter, for example /article/:id. The website displays each published item only on matching routes.

## Manage shared site content

In Content → Content Items, edit Credit Plus Site Settings for the support email, Contact Us phone numbers, bilingual address and intro, footer description, and login/signup URLs. Publish after changes.

To edit the header, footer, or social links, filter Content Items by Site Link. Each link is an individual item. Location is Header, Footer, or Social; the title is its English label and Arabic label is its Arabic label.

The existing two phone numbers are kept on the Contact Us page and do not appear in the footer.

## Orchard Core tools

Media, Menu, Navigation, Content Preview, Queries, SEO, Sitemaps, Content Localization, Audit Trail, and Workflows are enabled.

The local database and tenant content are stored under orchard-cms/CreditPlus.Cms.Web/App_Data and are ignored by Git. Back up that folder to retain CMS content and its administrator account.
