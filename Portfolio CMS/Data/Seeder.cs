using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Models;

namespace PortfolioCms.Data;

/// <summary>
/// Fills an empty database with the content that was in the portfolio's src/data folder
/// (exported to Data/seed.json), and makes sure every site setting exists.
/// </summary>
public static class Seeder
{
    public const string SiteKeySetting = "__site_key";

    /// <summary>Text and links the website reads with site(key, fallback). Empty value = the website's built-in text.</summary>
    public static readonly (string Key, string Label)[] SettingKeys =
    [
        ("contact_email", "Contact email (shown on the site, used for mailto)"),
        ("availability", "Availability badge — default \"Available for new projects\""),
        ("reply_time", "Reply time — default \"Replies within 24 hours\""),
        ("location_note", "Location note — default \"Working worldwide · GMT+6\""),
        ("fiverr_url", "Fiverr link"),
        ("dribbble_url", "Dribbble link"),
        ("github_url", "GitHub link"),
        ("linkedin_url", "LinkedIn link"),
    ];

    /// <summary>One editable field of a hero section. Kind: text, area or image. Default = the website's current text.</summary>
    public record HeroField(string Key, string Label, string Kind, string Default, string Hint = "");

    /// <summary>Fields on the dashboard's Hero section page, grouped by page. Stored as site settings.</summary>
    public static readonly (string Group, HeroField[] Fields)[] HeroGroups =
    [
        ("Photo", [
            new("hero_photo", "Profile photo", "image", "", "Used on the Home and About page. Empty = the photo in the website's public/images/maruf.jpg"),
        ]),
        ("Home page hero", [
            new("home_hero_badge", "Announcement badge", "text", "New"),
            new("home_hero_announce", "Announcement text", "text", "", "Empty = \"Latest: <your newest project> design\""),
            new("home_hero_title", "Headline", "text", "I design digital experiences"),
            new("home_hero_title_accent", "Headline — highlighted part", "text", "that make complex things feel simple."),
            new("home_hero_text", "Paragraph", "area", "I'm Maruf, a UI/UX and brand designer from Bangladesh. I design websites, mobile apps and dashboards that look sharp and help businesses grow."),
            new("home_hero_btn1_text", "Main button text", "text", "Start a project"),
            new("home_hero_btn1_link", "Main button link", "text", "#contact", "#contact, /work or a full URL"),
            new("home_hero_btn2_text", "Second button text", "text", "View work"),
            new("home_hero_btn2_link", "Second button link", "text", "#work"),
            new("home_hero_location", "Location on the photo", "text", "Sirajganj, Bangladesh"),
        ]),
        ("Home page numbers", [
            new("stat1_label", "Box 1 — label", "text", "Experience"),
            new("stat1_value", "Box 1 — number", "text", "4"),
            new("stat1_suffix", "Box 1 — after the number", "text", "+ yrs"),
            new("stat2_label", "Box 2 — label", "text", "Portfolio"),
            new("stat2_value", "Box 2 — number", "text", "16"),
            new("stat2_suffix", "Box 2 — after the number", "text", "+"),
            new("stat3_label", "Box 3 — label", "text", "Services"),
            new("stat3_value", "Box 3 — number", "text", "4"),
            new("stat3_suffix", "Box 3 — after the number", "text", ""),
        ]),
        ("About page hero", [
            new("about_hero_badge", "Announcement badge", "text", "Open"),
            new("about_hero_announce", "Announcement text", "text", "Available for new projects"),
            new("about_hero_title", "Headline", "text", "Part designer,"),
            new("about_hero_title_accent", "Headline — highlighted part", "text", "part problem solver."),
            new("about_hero_text", "Paragraph", "area", "I'm Sheikh Md Maruf, a UI/UX and brand designer based in Sirajganj, Bangladesh. For over four years I've helped businesses turn ideas into websites and apps people enjoy using."),
            new("about_hero_btn1_text", "Main button text", "text", "Start a project"),
            new("about_hero_btn1_link", "Main button link", "text", "#contact"),
            new("about_hero_btn2_text", "Second button text", "text", "View work"),
            new("about_hero_btn2_link", "Second button link", "text", "/work"),
        ]),
    ];

    public static readonly HashSet<string> HeroKeys = HeroGroups.SelectMany(g => g.Fields).Select(f => f.Key).ToHashSet();

    public static async Task RunAsync(CmsDbContext db, string contentRoot)
    {
        foreach (var (key, label) in SettingKeys)
            if (!await db.SiteSettings.AnyAsync(s => s.Key == key))
                db.SiteSettings.Add(new SiteSetting { Key = key, Label = label });

        // Hero fields start with the website's current text, so the form shows what is live now.
        foreach (var f in HeroGroups.SelectMany(g => g.Fields))
            if (!await db.SiteSettings.AnyAsync(s => s.Key == f.Key))
                db.SiteSettings.Add(new SiteSetting { Key = f.Key, Label = f.Label, Value = f.Default });

        if (!await db.SiteSettings.AnyAsync(s => s.Key == SiteKeySetting))
            db.SiteSettings.Add(new SiteSetting { Key = SiteKeySetting, Label = "Public site key", Value = NewSiteKey() });

        await db.SaveChangesAsync();

        if (await db.Projects.AnyAsync() || await db.Services.AnyAsync() || await db.Posts.AnyAsync()) return;

        var file = Path.Combine(contentRoot, "Data", "seed.json");
        if (!File.Exists(file)) return;

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(file));
        var root = doc.RootElement;

        var email = Str(root, "contactEmail");
        if (!string.IsNullOrEmpty(email) && !email.Contains("yourdomain"))
            (await db.SiteSettings.FindAsync("contact_email"))!.Value = email;

        var cases = root.GetProperty("cases");
        int i = 0;
        foreach (var p in root.GetProperty("projects").EnumerateArray())
        {
            var proj = new Project
            {
                Title = Str(p, "t")!, Type = Str(p, "type") ?? "web", Category = Str(p, "cat") ?? "", Year = Str(p, "y") ?? "",
                Highlight = Str(p, "hl") ?? "", Summary = Str(p, "sum") ?? "", Url = Str(p, "url") ?? "",
                Color1 = Str(p, "c1") ?? "#7f7bff", Color2 = Str(p, "c2") ?? "#3b2bd6", Badge = Str(p, "badge"),
                ImageUrl = Str(p, "img"), CoverUrl = Str(p, "cover"), SortOrder = i++,
            };
            proj.Slug = Str(p, "slug") ?? SlugHelper.Make(proj.Title);
            if (cases.TryGetProperty(proj.Slug, out var cs))
            {
                proj.Client = Str(cs, "client"); proj.Role = Str(cs, "role"); proj.Duration = Str(cs, "duration");
                proj.Tools = Str(cs, "tools"); proj.Team = Str(cs, "team");
                int j = 0;
                if (cs.TryGetProperty("metrics", out var ms))
                    foreach (var m in ms.EnumerateArray())
                        proj.Metrics.Add(new ProjectMetric { SortOrder = j++, Label = Str(m, "label") ?? "", Value = Str(m, "value") ?? "", Caption = Str(m, "cap") ?? "", Icon = Str(m, "icon") ?? "star" });
                j = 0;
                if (cs.TryGetProperty("steps", out var ss))
                    foreach (var s in ss.EnumerateArray())
                        proj.Steps.Add(new ProjectStep { SortOrder = j++, Title = Str(s, "title") ?? "", Body = string.Join("\n\n", s.GetProperty("body").EnumerateArray().Select(x => x.GetString())) });
                j = 0;
                if (cs.TryGetProperty("gallery", out var gs))
                    foreach (var g in gs.EnumerateArray())
                        proj.Gallery.Add(new ProjectImage { SortOrder = j++, Url = g.GetString() ?? "" });
            }
            db.Projects.Add(proj);
        }

        var details = root.GetProperty("serviceDetails");
        i = 0;
        foreach (var s in root.GetProperty("services").EnumerateArray())
        {
            var svc = new Service
            {
                Title = Str(s, "t")!, Summary = Str(s, "s") ?? "", Icon = Str(s, "icon") ?? "", Price = Str(s, "price"), SortOrder = i++,
                Tags = string.Join(", ", s.GetProperty("tags").EnumerateArray().Select(x => x.GetString())),
            };
            svc.Slug = SlugHelper.Make(svc.Title);
            if (details.TryGetProperty(svc.Slug, out var d))
            {
                svc.DetailType = Str(d, "type"); svc.DetailSummary = Str(d, "summary");
                int j = 0;
                if (d.TryGetProperty("metrics", out var ms))
                    foreach (var m in ms.EnumerateArray())
                        svc.Metrics.Add(new ServiceMetric { SortOrder = j++, Label = m[0].GetString()!, Value = m[1].GetString()!, Icon = m[2].GetString()! });
                j = 0;
                if (d.TryGetProperty("included", out var inc))
                    foreach (var m in inc.EnumerateArray())
                        svc.Included.Add(new ServiceIncluded { SortOrder = j++, Icon = m[0].GetString()!, Title = m[1].GetString()!, Text = m[2].GetString()! });
                j = 0;
                if (d.TryGetProperty("steps", out var st))
                    foreach (var m in st.EnumerateArray())
                        svc.Steps.Add(new ServiceStep { SortOrder = j++, Title = m[0].GetString()!, Text = m[1].GetString()! });
            }
            db.Services.Add(svc);
        }

        var articles = root.GetProperty("articles");
        i = 0;
        foreach (var b in root.GetProperty("posts").EnumerateArray())
        {
            var post = new Post
            {
                Title = Str(b, "t")!, Category = Str(b, "cat") ?? "", ReadTime = Str(b, "read") ?? "",
                Color1 = Str(b, "c1") ?? "#7f7bff", Color2 = Str(b, "c2") ?? "#3b2bd6", Type = Str(b, "type"),
                ImageUrl = Str(b, "img"), ExternalUrl = Str(b, "url"), SortOrder = i++,
                PublishedOn = DateTime.TryParseExact(Str(b, "date"), "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Today,
            };
            post.Slug = SlugHelper.Make(post.Title);
            if (articles.TryGetProperty(post.Slug, out var a))
            {
                post.ArticleSummary = Str(a, "sum");
                int j = 0;
                if (a.TryGetProperty("body", out var body))
                    foreach (var blk in body.EnumerateArray())
                    {
                        var block = new PostBlock { SortOrder = j++ };
                        if (blk.ValueKind == JsonValueKind.String) { block.Kind = "p"; block.Text = blk.GetString()!; }
                        else
                        {
                            var prop = blk.EnumerateObject().First();
                            block.Kind = prop.Name;
                            block.Text = prop.Value.ValueKind == JsonValueKind.Array
                                ? string.Join("\n", prop.Value.EnumerateArray().Select(x => x.GetString()))
                                : prop.Value.GetString()!;
                        }
                        post.Blocks.Add(block);
                    }
            }
            db.Posts.Add(post);
        }

        i = 0;
        foreach (var r in root.GetProperty("reviews").EnumerateArray())
            db.Reviews.Add(new Review { Quote = Str(r, "q")!, Name = Str(r, "n")!, Role = Str(r, "r") ?? "", Company = Str(r, "co") ?? "", ColorA = Str(r, "a") ?? "", ColorB = Str(r, "b") ?? "", SortOrder = i++ });

        i = 0;
        foreach (var f in root.GetProperty("faq").EnumerateArray())
            db.Faqs.Add(new Faq { Pill = f[0].GetString()!, Question = f[1].GetString()!, Answer = f[2].GetString()!, SortOrder = i++ });

        i = 0;
        foreach (var t in root.GetProperty("tools").EnumerateArray())
            db.Tools.Add(new Tool { Name = t.GetString()!, SortOrder = i++ });

        await db.SaveChangesAsync();
    }

    public static string NewSiteKey() => "pk_live_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
