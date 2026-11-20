using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Services;

/// <summary>Editable texts and links shown on the portfolio. Keys are read by the portfolio's live-content loader.</summary>
public static class SiteKeys
{
    public const string ContactEmail = "contact_email";
    public const string Tools = "tools";

    public static readonly IReadOnlyList<(string Key, string Label, string Group, bool Multiline)> Fields =
    [
        (ContactEmail, "Contact email", "Contact", false),
        ("availability", "Availability badge", "Contact", false),
        ("reply_time", "Reply time note", "Contact", false),
        ("location_note", "Location / timezone note", "Contact", false),
        ("fiverr_url", "Fiverr profile URL", "Links", false),
        ("dribbble_url", "Dribbble profile URL", "Links", false),
        ("github_url", "GitHub profile URL", "Links", false),
        ("linkedin_url", "LinkedIn profile URL", "Links", false),
        (Tools, "Tools I use (one per line)", "About", true),
    ];
}

public class ContentService(AppDbContext db, MediaStorage media)
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Dictionary<string, string>> GetSettingsAsync() =>
        await db.SiteSettings.ToDictionaryAsync(s => s.Key, s => s.Value);

    public async Task SetSettingAsync(string key, string value)
    {
        var s = await db.SiteSettings.FindAsync(key);
        if (s is null) db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        else { s.Value = value; s.UpdatedAt = DateTime.UtcNow; }
    }

    /// <summary>Published content in the exact shape of the portfolio's src/data types.</summary>
    public async Task<object> BuildPublicPayloadAsync(HttpRequest req)
    {
        string? U(string? p) => media.PublicUrl(p, req);
        var settings = await GetSettingsAsync();

        var projects = await db.CmsProjects.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        var services = await db.CmsServices.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        var posts = await db.CmsPosts.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenByDescending(p => p.PublishDate).ToListAsync();
        var reviews = await db.CmsReviews.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        var faq = await db.CmsFaqs.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();

        var cases = new Dictionary<string, object>();
        foreach (var p in projects.Where(HasCase))
            cases[p.Slug] = Compact(new
            {
                client = p.Client, role = p.Role, duration = p.Duration, tools = p.Tools, team = p.Team,
                metrics = Parse(p.MetricsJson), steps = Parse(p.StepsJson),
                gallery = Parse(p.GalleryJson) is JsonArray g ? new JsonArray(g.Select(x => (JsonNode?)JsonValue.Create(U(x?.GetValue<string>()))).ToArray()) : null,
            });

        var serviceDetails = new Dictionary<string, object>();
        foreach (var s in services.Where(s => s.Summary is not null || s.MetricsJson is not null || s.IncludedJson is not null || s.StepsJson is not null))
            serviceDetails[s.Slug] = Compact(new
            {
                type = s.DetailType, summary = s.Summary, metrics = Parse(s.MetricsJson), included = Parse(s.IncludedJson), steps = Parse(s.StepsJson)
            });

        var articles = new Dictionary<string, object>();
        foreach (var p in posts.Where(p => string.IsNullOrEmpty(p.ExternalUrl) && (p.Summary is not null || p.Body is not null)))
            articles[p.Slug] = Compact(new { sum = p.Summary, body = p.Body is null ? null : ArticleFormat.ToBlocks(p.Body) });

        return new
        {
            version = 1,
            updatedAt = DateTime.UtcNow,
            settings = settings.Where(s => s.Key != SiteKeys.Tools).ToDictionary(),
            contactEmail = settings.GetValueOrDefault(SiteKeys.ContactEmail),
            tools = (settings.GetValueOrDefault(SiteKeys.Tools) ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            projects = projects.Select(p => Compact(new
            {
                t = p.Title, type = p.Type, cat = p.Category, y = p.Year, hl = p.Highlight, sum = p.Summary, url = p.ExternalUrl ?? "",
                c1 = p.Color1, c2 = p.Color2, badge = p.Badge, img = U(p.ImagePath), cover = U(p.CoverPath), slug = p.Slug
            })),
            services = services.Select(s => Compact(new
            {
                t = s.Title, s = s.ShortText, tags = s.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                icon = s.IconSvg, price = s.Price
            })),
            reviews = reviews.Select(r => new { q = r.Quote, n = r.Name, r = r.Role, co = r.Company, a = r.Color1, b = r.Color2 }),
            faq = faq.Select(f => new[] { f.Pill, f.Question, f.Answer }),
            posts = posts.Select(p => Compact(new
            {
                t = p.Title, cat = p.Category, date = p.PublishDate.ToString("d MMMM yyyy"), read = p.ReadTime,
                c1 = p.Color1, c2 = p.Color2, type = p.Type, img = U(p.ImagePath), url = p.ExternalUrl
            })),
            cases,
            articles,
            serviceDetails,
        };
    }

    static bool HasCase(CmsProject p) =>
        p.Client is not null || p.Role is not null || p.Duration is not null || p.Tools is not null || p.Team is not null ||
        p.MetricsJson is not null || p.StepsJson is not null || p.GalleryJson is not null;

    static JsonNode? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonNode.Parse(json); } catch { return null; }
    }

    /// <summary>Drops null / empty values so optional fields stay optional on the portfolio.</summary>
    static JsonObject Compact(object o)
    {
        var node = JsonSerializer.SerializeToNode(o, Json)!.AsObject();
        foreach (var k in node.Where(kv => kv.Value is null || (kv.Value is JsonValue v && v.TryGetValue<string>(out var s) && s.Length == 0))
                     .Select(kv => kv.Key).ToList())
            node.Remove(k);
        return node;
    }

    // ---------------------------------------------------------------- import

    /// <summary>
    /// Imports the portfolio's existing content (exported by tools/export-portfolio-content.mjs).
    /// Only runs when the CMS is empty, so it never overwrites edits.
    /// </summary>
    public async Task<bool> ImportIfEmptyAsync(string jsonPath, ILogger logger)
    {
        if (!File.Exists(jsonPath) || await db.CmsProjects.AnyAsync() || await db.CmsServices.AnyAsync()) return false;
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
        var r = doc.RootElement;
        string? S(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        string? Raw(JsonElement e, string name) => e.TryGetProperty(name, out var v) ? v.GetRawText() : null;

        if (S(r, "contactEmail") is { } email) await SetSettingAsync(SiteKeys.ContactEmail, email);
        if (r.TryGetProperty("tools", out var tools))
            await SetSettingAsync(SiteKeys.Tools, string.Join("\n", tools.EnumerateArray().Select(t => t.GetString())));

        var cases = r.TryGetProperty("cases", out var c) ? c : default;
        int i = 0;
        foreach (var p in r.GetProperty("projects").EnumerateArray())
        {
            var title = S(p, "t")!;
            var slug = S(p, "slug") ?? Slug.From(title);
            var item = new CmsProject
            {
                Title = title, Slug = slug, Type = S(p, "type") ?? "web", Category = S(p, "cat") ?? "", Year = S(p, "y") ?? "",
                Highlight = S(p, "hl") ?? "", Summary = S(p, "sum") ?? "", ExternalUrl = S(p, "url"), Color1 = S(p, "c1") ?? "#7f7bff",
                Color2 = S(p, "c2") ?? "#3b2bd6", Badge = S(p, "badge"), ImagePath = S(p, "img"), CoverPath = S(p, "cover"), SortOrder = i++,
            };
            if (cases.ValueKind == JsonValueKind.Object && cases.TryGetProperty(slug, out var cs))
            {
                item.Client = S(cs, "client"); item.Role = S(cs, "role"); item.Duration = S(cs, "duration");
                item.Tools = S(cs, "tools"); item.Team = S(cs, "team");
                item.MetricsJson = Raw(cs, "metrics"); item.StepsJson = Raw(cs, "steps"); item.GalleryJson = Raw(cs, "gallery");
            }
            db.CmsProjects.Add(item);
        }

        var details = r.TryGetProperty("serviceDetails", out var d) ? d : default;
        i = 0;
        foreach (var s in r.GetProperty("services").EnumerateArray())
        {
            var title = S(s, "t")!;
            var slug = Slug.From(title);
            var item = new CmsService
            {
                Title = title, Slug = slug, ShortText = S(s, "s") ?? "", IconSvg = SvgSanitizer.Clean(S(s, "icon")), Price = S(s, "price"),
                Tags = string.Join(", ", s.TryGetProperty("tags", out var t) ? t.EnumerateArray().Select(x => x.GetString()) : []), SortOrder = i++,
            };
            if (details.ValueKind == JsonValueKind.Object && details.TryGetProperty(slug, out var sd))
            {
                item.DetailType = S(sd, "type"); item.Summary = S(sd, "summary");
                item.MetricsJson = Raw(sd, "metrics"); item.IncludedJson = Raw(sd, "included"); item.StepsJson = Raw(sd, "steps");
            }
            db.CmsServices.Add(item);
        }

        i = 0;
        foreach (var v in r.GetProperty("reviews").EnumerateArray())
            db.CmsReviews.Add(new CmsReview { Quote = S(v, "q")!, Name = S(v, "n")!, Role = S(v, "r") ?? "", Company = S(v, "co") ?? "", Color1 = S(v, "a") ?? "#7f7bff", Color2 = S(v, "b") ?? "#3b2bd6", SortOrder = i++ });

        i = 0;
        foreach (var f in r.GetProperty("faq").EnumerateArray())
            db.CmsFaqs.Add(new CmsFaq { Pill = f[0].GetString() ?? "", Question = f[1].GetString()!, Answer = f[2].GetString()!, SortOrder = i++ });

        var articles = r.TryGetProperty("articles", out var a) ? a : default;
        i = 0;
        foreach (var p in r.GetProperty("posts").EnumerateArray())
        {
            var title = S(p, "t")!;
            var slug = Slug.From(title);
            var post = new CmsPost
            {
                Title = title, Slug = slug, Category = S(p, "cat") ?? "", ReadTime = S(p, "read") ?? "", Color1 = S(p, "c1") ?? "#7f7bff",
                Color2 = S(p, "c2") ?? "#3b2bd6", Type = S(p, "type"), ImagePath = S(p, "img"), ExternalUrl = S(p, "url"), SortOrder = i++,
                PublishDate = DateOnly.TryParse(S(p, "date"), System.Globalization.CultureInfo.InvariantCulture, out var dt) ? dt : DateOnly.FromDateTime(DateTime.UtcNow),
            };
            if (articles.ValueKind == JsonValueKind.Object && articles.TryGetProperty(slug, out var art))
            {
                post.Summary = S(art, "sum");
                if (art.TryGetProperty("body", out var body)) post.Body = ArticleFormat.FromBlocks(body);
            }
            db.CmsPosts.Add(post);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Imported existing portfolio content from {Path}", jsonPath);
        return true;
    }
}
