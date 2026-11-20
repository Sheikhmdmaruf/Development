# Portfolio Admin

A business dashboard for your portfolio: collect inquiries from all your websites, manage leads, edit the portfolio's content and run your team. It's one place for the whole business.

**Stack:** ASP.NET Core 9 MVC (Razor) · Entity Framework Core 9 · SQL Server · ASP.NET Core Identity · Chart.js · Lucide icons. There's no Node build step, and the client libraries are vendored in `wwwroot/lib`.

## What's in it (Phase 1)

| Module | What you can do |
| --- | --- |
| **Overview** | KPIs, inquiries per month, lead pipeline, inquiries by website and type, latest inquiries, activity feed |
| **My Workspace** | Contacts assigned to you, your notifications, your recent actions |
| **Contacts (CRM)** | Search, filter (status, website, type, assignee, tag, unread) and sort. Profile page with full message history, internal notes, tags, lead status and assignment. Archive / restore / delete, bulk actions, CSV export (opens in Excel) |
| **Leads** | Drag-and-drop board across New → Contacted → Interested → In Discussion → Converted / Rejected |
| **Websites** | Connect any site. Each one gets a public site key (browser), a secret key (server, stored hashed, shown once), allowed origins, stats and copy-paste code |
| **Portfolio content** | Projects + case studies, services + detail pages, blog posts (with article editor), reviews, FAQ, site settings (email, links, availability, tools). Image upload, drag to reorder, publish/hide |
| **Notifications** | In-app bell with unread count, new inquiry / assigned / note alerts, per-user preferences |
| **Team** | Add members (temporary password, forced change), edit, deactivate, reset password, remove, member stats, team activity feed |
| **Roles & permissions** | Super Admin, Admin, Project Manager, Team Member + custom roles. A permission matrix per module, applied live within a minute |
| **Audit log** | Sign-ins, failed sign-ins and lockouts, key changes, role changes, deletions, exports |
| **Global search** | `Ctrl + K` across contacts, websites, content and team (respects permissions) |
| **Light / dark / system theme** | Collapsible sidebar, responsive down to phone width |

**Next phases:** 2 Projects & Tasks (Kanban, list, table, calendar, comments, attachments, review flow) · 3 Email (Gmail OAuth, IMAP/SMTP, Resend/SendGrid) · 4 Reports, real-time notifications (SignalR), webhooks. The permissions for these modules already exist, so roles don't need to be set up again.

## Run it locally

Requirements: .NET 9 SDK and SQL Server (Express is fine).

```powershell
cd "src/PortfolioAdmin.Web"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5180
```

On first start the app:
1. creates the `PortfolioAdmin` database (migrations run automatically),
2. creates the 4 built-in roles,
3. creates the first **Super Admin** from `Seed:AdminEmail` (in `appsettings.Development.json`). If `Seed:AdminPassword` isn't set, it generates one and **prints it in the console**. You must change it at first sign-in,
4. imports your existing portfolio content from `Data/Seed/portfolio-content.json` (only when the CMS is empty).

To set your own first password instead:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Seed:AdminPassword" "YourStrongPassword123"
```

### Demo data (optional)

Set `Seed:DemoData` to `true` to add 28 fake inquiries for trying things out. They are clearly separated: all of them are tagged **Demo** and come from a disabled website called **Demo data (seed)**. Use a separate database for this:

```powershell
$env:ConnectionStrings__Default = "Server=.\SQLEXPRESS;Database=PortfolioAdmin_Demo;Trusted_Connection=True;TrustServerCertificate=True"
$env:Seed__DemoData = "true"
```

## Connect a website

1. **Websites → Connect website.** Enter the URL and allowed origins (add `http://localhost:5173` while developing). For your portfolio, turn on **Can read portfolio content**.
2. Copy the **site key** (`pk_live_…`) and copy the **secret key** right away. It's shown only once.
3. Use the snippet on the website's page, or for the React portfolio, create `Portfolio Website/.env.local`:

```
VITE_ADMIN_API_URL=http://localhost:5180
VITE_SITE_KEY=pk_live_...
```

### Public API

| Endpoint | Auth | Limit |
| --- | --- | --- |
| `POST /api/v1/submissions` | `X-Site-Key: pk_…` from an allowed origin, **or** `Authorization: Bearer sk_…` from a server | 5 / minute / IP |
| `GET /api/v1/content?key=pk_…` | Site key with "Can read portfolio content" | 120 / minute / IP |
| `GET /api/v1/health?key=pk_…` | Site key | 120 / minute / IP |

Submission fields (JSON or form data): `name`*, `email`*, `message`*, `phone`, `company`, `subject`, `inquiryType` (or `service`), `pageUrl`, `extra` (object). Any other fields are kept as extras. `gotcha` is a honeypot: when it's filled, the request gets the normal "ok" reply but nothing is stored. Repeat messages from the same email within 10 minutes are ignored. Messages from the same email are grouped under one contact.

## Security

- ASP.NET Core Identity: hashed passwords, 10+ characters, lockout after 5 failures (15 min), forced change of temporary passwords
- Every page requires sign-in (fallback policy). Every action checks a permission policy (`[HasPermission]`), and the UI hides what you can't do
- CSRF tokens on every form and AJAX call; `SameSite` cookies; `HttpOnly` auth cookie; `Secure` outside development
- EF Core parameterised queries; Razor output encoding; service SVG icons sanitised server-side to an allow-list; CSV export protected against formula injection
- Uploads: images only, checked by file signature (not extension), 5 MB max, random file names
- Website secret keys stored as SHA-256 hashes; site keys limited to allowed origins
- Rate limits on login and the public API; security headers (CSP without inline scripts, `X-Frame-Options`, `nosniff`, `Referrer-Policy`)
- Audit log for sensitive actions; no secrets in the repo (use user-secrets or environment variables)

## Production checklist

- Set `ConnectionStrings__Default`, `Seed__AdminEmail` and `Seed__AdminPassword` as environment variables (first run only for the seed values)
- Set `App__PublicBaseUrl` to the dashboard's public URL (used for uploaded image links on the portfolio)
- Serve over HTTPS (HSTS and HTTPS redirection are on outside Development)
- Back up the database and `wwwroot/uploads`

## Project layout

```
src/PortfolioAdmin.Web/
  Authorization/   permissions, policies, [HasPermission]
  Controllers/     dashboard pages + Api/PublicApiController (website API)
  Data/            DbContext, entities, migrations, seeder, seed JSON
  Infrastructure/  security headers, claims, filters, view helpers
  Services/        submissions, content, notifications, activity, media
  ViewComponents/  sidebar, unread badge
  Views/           Razor views (one folder per module)
  wwwroot/         css/app.css (design system), js/app.js, lib/
tools/export-portfolio-content.mjs   re-export the portfolio's src/data to seed JSON
```
