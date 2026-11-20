using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Authorization;
using PortfolioAdmin.Web.Data.Entities;
using PortfolioAdmin.Web.Services;

namespace PortfolioAdmin.Web.Data;

/// <summary>
/// Runs at startup:
///  1. applies migrations,
///  2. creates the 4 built-in roles (permissions are only set when a role is first created, so your edits are kept),
///  3. creates the first Super Admin from config (Seed:AdminEmail / Seed:AdminPassword) when no users exist,
///  4. imports the portfolio's existing content when the CMS is empty,
///  5. adds DEMO contacts only when Seed:DemoData = true. Demo rows are tagged "Demo" and come from the "demo-seed" website.
/// </summary>
public static class Seeder
{
    static readonly (string Name, string Description)[] Roles =
    [
        (Permissions.SuperAdminRole, "Full access to everything, including roles and permissions."),
        ("Admin", "Manages contacts, content, team, projects and tasks."),
        ("Project Manager", "Runs projects and tasks, works with leads and the team."),
        ("Team Member", "Works on tasks assigned to them."),
    ];

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();
        for (var i = 0; i < Roles.Length; i++)
        {
            var (name, description) = Roles[i];
            if (await roleManager.RoleExistsAsync(name)) continue;
            var role = new AppRole(name) { Description = description, IsSystem = true, SortOrder = i };
            await roleManager.CreateAsync(role);
            if (name == Permissions.SuperAdminRole) continue; // has everything implicitly
            foreach (var p in Permissions.Defaults[name])
                await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, p));
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        if (!await db.Users.AnyAsync())
        {
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];
            var generated = false;
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Set Seed:AdminEmail (user-secrets or environment variable) to create the first Super Admin.");
            if (string.IsNullOrWhiteSpace(password))
            {
                if (!env.IsDevelopment())
                    throw new InvalidOperationException("Set Seed:AdminPassword to create the first Super Admin.");
                password = "Dev-" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace('/', 'x').Replace('+', 'y') + "9!";
                generated = true;
            }

            var admin = new AppUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                FullName = config["Seed:AdminName"] ?? "Sheikh Md Maruf", JobTitle = "Founder", MustChangePassword = generated,
            };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not create the Super Admin: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, Permissions.SuperAdminRole);

            if (generated)
                log.LogWarning("Created Super Admin {Email} with a generated password: {Password}  (you'll be asked to change it on first login)", email, password);
            else
                log.LogInformation("Created Super Admin {Email}", email);
        }

        var content = sp.GetRequiredService<ContentService>();
        await content.ImportIfEmptyAsync(Path.Combine(env.ContentRootPath, "Data", "Seed", "portfolio-content.json"), log);

        if (config.GetValue<bool>("Seed:DemoData"))
            await DemoData.AddAsync(db, log);
    }
}

/// <summary>Fake inquiries for trying the dashboard. Never runs unless Seed:DemoData = true.</summary>
static class DemoData
{
    public const string WebsiteIdentifier = "demo-seed";

    public static async Task AddAsync(AppDbContext db, ILogger log)
    {
        if (await db.Websites.AnyAsync(w => w.Identifier == WebsiteIdentifier)) return;

        var rnd = new Random(42);
        var demoSite = new Website
        {
            Name = "Demo data (seed)", Url = "https://demo.invalid", Identifier = WebsiteIdentifier,
            PublicKey = Keys.New("pk_demo_"), IsActive = false, AllowedOrigins = "",
        };
        db.Websites.Add(demoSite);
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == "Demo") ?? new Tag { Name = "Demo", Color = "slate" };

        string[] first = ["Ayesha", "Rahim", "Emma", "Lucas", "Sofia", "Noah", "Mia", "Arif", "Lena", "Omar", "Chloe", "Daniel", "Nadia", "Ethan", "Priya", "Jonas"];
        string[] last = ["Khan", "Ahmed", "Schmidt", "Rossi", "Garcia", "Smith", "Haque", "Nguyen", "Müller", "Islam", "Brown", "Lee"];
        string[] types = ["Website design", "Mobile app design", "Dashboard & SaaS UI", "Brand & graphic design", "Something else"];
        string[] msgs =
        [
            "Hi! We need a new landing page for our SaaS launch next month. Can you share your availability and rough pricing?",
            "Looking for a designer for a fintech mobile app (about 30 screens). Do you also make a clickable prototype?",
            "Our admin dashboard is hard to use. Could you redesign the main analytics screens?",
            "We're a small bakery and need a logo and simple brand guide.",
            "Saw your Dribbble shots — love the style. Are you open to a long-term retainer?",
        ];

        for (var i = 0; i < 28; i++)
        {
            var name = $"{first[rnd.Next(first.Length)]} {last[rnd.Next(last.Length)]}";
            var created = DateTime.UtcNow.AddDays(-rnd.Next(0, 180)).AddMinutes(-rnd.Next(0, 1440));
            var type = types[rnd.Next(types.Length)];
            var contact = new Contact
            {
                Name = name, Email = $"{name.ToLowerInvariant().Replace(' ', '.').Replace("ü", "u")}{i}@example.com",
                InquiryType = type, Status = (LeadStatus)rnd.Next(0, 6), Source = demoSite.Name, FirstWebsite = demoSite,
                CreatedAt = created, UpdatedAt = created, LastActivityAt = created, IsUnread = rnd.Next(3) == 0,
            };
            contact.Tags.Add(new ContactTag { Tag = tag });
            contact.Submissions.Add(new ContactSubmission
            {
                Website = demoSite, Name = name, Email = contact.Email, InquiryType = type, Subject = "New project: " + type,
                Message = msgs[rnd.Next(msgs.Length)], CreatedAt = created,
            });
            db.Contacts.Add(contact);
        }
        demoSite.TotalSubmissions = 28;
        await db.SaveChangesAsync();
        log.LogWarning("Added 28 DEMO contacts (tag \"Demo\", website \"demo-seed\"). Turn off Seed:DemoData for real use.");
    }
}
