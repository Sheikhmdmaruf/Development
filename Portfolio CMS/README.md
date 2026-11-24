# Portfolio CMS

Admin dashboard for the **Portfolio Website**, built with **ASP.NET Core 8 MVC** (LTS) and **Entity Framework Core 8**,
using **SQL Server** (database `PortfolioCms` on `.\SQLEXPRESS`).

From the dashboard you manage everything on the website:

| Dashboard page | What it changes on the website |
|---|---|
| Work | Projects (cards, Work page, home page) and their case studies: metrics, story steps, gallery |
| Services | Service cards and the service detail pages: metrics, what's included, how it works |
| Blog | Blog posts and the full article text (paragraphs, headings, quotes, lists) |
| Reviews | Client reviews |
| FAQ | Questions and answers |
| Tools | Tools and skills list |
| Media | Upload images and use them anywhere |
| Site settings | Contact email, availability text, reply time, location, Fiverr, Dribbble, GitHub and LinkedIn links |
| Inbox | Messages sent from the website's contact form |

You can hide or publish items, and change their order. Changes appear on the website the next time a page loads.

## Run it

**Visual Studio:** double-click `PortfolioCms.sln`, then press **F5** (or **Ctrl+F5**).

**Command line:**

You need the [.NET 8 SDK or newer](https://dotnet.microsoft.com/download) (with the .NET 8 runtime) and SQL Server (Express is fine).

```bash
dotnet run          # opens http://localhost:5180
```

On first start it:
1. creates the `PortfolioCms` database in SQL Server (EF Core migrations), and
2. fills it with the content from the website's `src/data` folder (`Data/seed.json`).

The first time you open the dashboard, it asks you to **create your admin account**.

## Connect the website

In the dashboard go to **Site settings → Connect your website**, then copy the two lines into
`Portfolio Website/.env.local` and restart the website (`npm run dev`):

```
VITE_ADMIN_API_URL=http://localhost:5180
VITE_SITE_KEY=pk_live_...
```

If the dashboard is not running, the website still works and shows its built-in content.

## API (used by the website)

| Method | URL | |
|---|---|---|
| GET | `/api/v1/content?key=<site key>` | All published content as JSON |
| POST | `/api/v1/submissions` (header `X-Site-Key`) | Contact form message → Inbox |

Only the websites listed in `appsettings.json` → `Cms:AllowedOrigins` can call the API from a browser.
When you put the website online, add its address there (for example `https://maruf.design`).

## Database

Open **SQL Server Management Studio**, connect to `.\SQLEXPRESS` (Windows Authentication) and open
the `PortfolioCms` database. Main tables: `Projects`, `ProjectMetrics`, `ProjectSteps`, `ProjectImages`,
`Services`, `ServiceMetrics`, `ServiceIncluded`, `ServiceSteps`, `Posts`, `PostBlocks`, `Reviews`, `Faqs`,
`Tools`, `SiteSettings`, `Submissions`, `AdminUsers`.

The connection string is in `appsettings.json` → `ConnectionStrings:Cms`.

After changing a model in `Models/Entities.cs`:

```bash
dotnet ef migrations add <Name> -o Data/Migrations
dotnet ef database update
```

## Project structure

```
Controllers/   ApiController (website API), content CRUD, account, inbox, media, settings
Data/          CmsDbContext, Seeder, migrations, seed.json
Models/        Entity classes (database tables)
Infrastructure/ form helpers for the edit pages
Views/         Razor pages of the dashboard
wwwroot/       CSS, JS, uploaded images (wwwroot/uploads)
```
