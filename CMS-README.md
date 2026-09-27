# Orchard Core CMS

The Credit Plus Angular site reads published contact details and links from Orchard Core.

## Run locally

From the site folder, start Orchard Core:

\`\`\`powershell
dotnet run --project orchard-cms/CreditPlus.Cms.Web/CreditPlus.Cms.Web.csproj
\`\`\`

In another terminal, start Angular:

\`\`\`powershell
npm start
\`\`\`

Open the site at \`http://localhost:4200\` and Orchard admin at \`http://localhost:3001/Admin\`. The Angular development proxy forwards \`/cms-api\` to Orchard's read-only endpoint at \`http://localhost:3001/cms-api/site-settings\`.

## Edit site details

In Orchard admin, open **Content → Content Items** and edit **Credit Plus Site Settings**. This item contains the email, contact page phone numbers, Arabic and English address and contact intro, footer description, and login and signup URLs. Publish the item after changes.

## Edit header, footer, and social links

In **Content → Content Items**, choose **Site Link** in the content type filter. The existing links are individual content items, so each can be edited, unpublished, or deleted. Use **New → Site Link** to add another.

Each item uses:

- **Title**: English label
- **Arabic label**: Arabic label
- **Link URL**: internal route such as \`/contact\`, or a full external URL
- **Location**: \`Header\`, \`Footer\`, or \`Social\`
- **Display order**: numeric position within that location

The CMS is prefilled with the current Credit Plus navigation and footer links, LinkedIn, support email, two contact page phone numbers, Amman address, and login/signup links. The phone numbers remain on the Contact Us page and are not shown in the footer.

## Orchard Core tools

Orchard's built-in Media, Menu, Navigation, Content Preview, and Queries features are enabled. SEO, Sitemaps, Content Localization, Audit Trail, and Workflows are also enabled. They are available from the standard Orchard admin menus; SEO adds metadata editing, Sitemaps generates a sitemap, Content Localization supports localized content items, Audit Trail records admin changes, and Workflows provides visual automation.

The local database and tenant data are stored under \`orchard-cms/App_Data\` and are ignored by Git. Back up that folder to preserve CMS content and its administrator account. The local administrator is \`siteadmin\`; change its password from the Orchard user menu after signing in.

For deployment, forward \`/cms-api\` to the Orchard service over HTTPS and keep the Orchard admin behind HTTPS.
