using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PortfolioCms.Data;
using PortfolioCms.Models;

namespace PortfolioCms.Controllers;

/// <summary>
/// Public API used by the portfolio website (see src/data/live.ts there).
///   GET  /api/v1/content?key=pk_live_…   → all published content
///   POST /api/v1/submissions             → contact form (header X-Site-Key)
/// </summary>
[ApiController, Route("api/v1"), EnableCors("site"), IgnoreAntiforgeryToken, AllowAnonymous]
public class ApiController(CmsDbContext db) : ControllerBase
{
    private async Task<bool> KeyOk(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var real = (await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == Seeder.SiteKeySetting))?.Value;
        return real is not null && CryptographicEquals(real, key);
    }

    private static bool CryptographicEquals(string a, string b) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a), System.Text.Encoding.UTF8.GetBytes(b));

    /// <summary>Turns "/uploads/x.jpg" (files uploaded to this dashboard) into a full URL the website can load.</summary>
    private string? Media(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null
        : path.StartsWith("/uploads/") ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}"
        : path;

    private static string? Opt(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    [HttpGet("content")]
    public new async Task<IActionResult> Content([FromQuery] string? key)
    {
        if (!await KeyOk(key)) return Unauthorized(new { error = "Invalid site key" });

        var settings = await db.SiteSettings.AsNoTracking().Where(s => !s.Key.StartsWith("__")).ToListAsync();

        var projects = await db.Projects.AsNoTracking().Where(p => p.IsPublished).OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Include(p => p.Metrics).Include(p => p.Steps).Include(p => p.Gallery).AsSplitQuery().ToListAsync();
        var services = await db.Services.AsNoTracking().Where(s => s.IsPublished).OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Include(s => s.Metrics).Include(s => s.Included).Include(s => s.Steps).AsSplitQuery().ToListAsync();
        var posts = await db.Posts.AsNoTracking().Where(p => p.IsPublished).OrderByDescending(p => p.PublishedOn).ThenBy(p => p.SortOrder)
            .Include(p => p.Blocks).ToListAsync();

        var cases = new Dictionary<string, object>();
        foreach (var p in projects)
        {
            var hasCase = p.Metrics.Count + p.Steps.Count + p.Gallery.Count > 0 ||
                          new[] { p.Client, p.Role, p.Duration, p.Tools, p.Team }.Any(x => !string.IsNullOrWhiteSpace(x));
            if (!hasCase) continue;
            cases[p.Slug!] = new
            {
                client = Opt(p.Client), role = Opt(p.Role), duration = Opt(p.Duration), tools = Opt(p.Tools), team = Opt(p.Team),
                metrics = p.Metrics.Count == 0 ? null : p.Metrics.OrderBy(m => m.SortOrder).Select(m => new { label = m.Label, value = m.Value, cap = m.Caption, icon = m.Icon }),
                steps = p.Steps.Count == 0 ? null : p.Steps.OrderBy(s => s.SortOrder).Select(s => new { title = s.Title, body = Paragraphs(s.Body) }),
                gallery = p.Gallery.Count == 0 ? null : p.Gallery.OrderBy(g => g.SortOrder).Select(g => Media(g.Url)),
            };
        }

        var serviceDetails = services.ToDictionary(s => s.Slug!, s => (object)new
        {
            type = Opt(s.DetailType), summary = Opt(s.DetailSummary),
            metrics = s.Metrics.OrderBy(m => m.SortOrder).Select(m => new[] { m.Label, m.Value, m.Icon }),
            included = s.Included.OrderBy(m => m.SortOrder).Select(m => new[] { m.Icon, m.Title, m.Text }),
            steps = s.Steps.OrderBy(m => m.SortOrder).Select(m => new[] { m.Title, m.Text }),
        });

        var articles = posts.Where(p => p.Blocks.Count > 0 || !string.IsNullOrWhiteSpace(p.ArticleSummary))
            .ToDictionary(p => p.Slug!, p => (object)new { sum = Opt(p.ArticleSummary), body = p.Blocks.OrderBy(b => b.SortOrder).Select(Block) });

        return Ok(new
        {
            settings = settings.Where(s => s.Key != "contact_email").ToDictionary(s => s.Key, s => s.Value),
            contactEmail = Opt(settings.FirstOrDefault(s => s.Key == "contact_email")?.Value),
            tools = await db.Tools.AsNoTracking().Where(t => t.IsPublished).OrderBy(t => t.SortOrder).Select(t => t.Name).ToListAsync(),
            projects = projects.Select(p => new
            {
                t = p.Title, type = p.Type, cat = p.Category, y = p.Year, hl = p.Highlight, sum = p.Summary, url = p.Url,
                c1 = p.Color1, c2 = p.Color2, badge = Opt(p.Badge), img = Media(p.ImageUrl), cover = Media(p.CoverUrl), slug = p.Slug,
            }),
            services = services.Select(s => new
            {
                t = s.Title, s = s.Summary, icon = s.Icon, price = Opt(s.Price),
                tags = s.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            }),
            reviews = await db.Reviews.AsNoTracking().Where(r => r.IsPublished).OrderBy(r => r.SortOrder)
                .Select(r => new { q = r.Quote, n = r.Name, r = r.Role, co = r.Company, a = r.ColorA, b = r.ColorB }).ToListAsync(),
            faq = (await db.Faqs.AsNoTracking().Where(f => f.IsPublished).OrderBy(f => f.SortOrder).ToListAsync())
                .Select(f => new[] { f.Pill, f.Question, f.Answer }),
            posts = posts.Select(p => new
            {
                t = p.Title, cat = p.Category, date = p.PublishedOn.ToString("d MMMM yyyy", CultureInfo.InvariantCulture), read = p.ReadTime,
                c1 = p.Color1, c2 = p.Color2, type = Opt(p.Type), img = Media(p.ImageUrl), url = Opt(p.ExternalUrl),
            }),
            cases,
            articles,
            serviceDetails,
        });
    }

    private static string[] Paragraphs(string body) =>
        body.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static object Block(PostBlock b) => b.Kind switch
    {
        "h2" => new { h2 = b.Text },
        "h3" => new { h3 = b.Text },
        "quote" => new { quote = b.Text },
        "list" => new { list = b.Text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) },
        _ => b.Text,
    };

    public record SubmissionDto(string? Name, string? Email, string? InquiryType, string? Subject, string? Message, string? Gotcha, string? PageUrl);

    [HttpPost("submissions"), EnableRateLimiting("submissions")]
    public async Task<IActionResult> Submit([FromBody] SubmissionDto dto, [FromHeader(Name = "X-Site-Key")] string? key)
    {
        if (!await KeyOk(key)) return Unauthorized(new { error = "Invalid site key" });
        if (!string.IsNullOrEmpty(dto.Gotcha)) return Ok(new { ok = true }); // bot filled the hidden field

        static string Cut(string? s, int max) { var t = (s ?? "").Trim(); return t.Length > max ? t[..max] : t; }
        var name = Cut(dto.Name, 120); var email = Cut(dto.Email, 200); var message = Cut(dto.Message, 10000);
        if (name.Length == 0 || message.Length == 0 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(new { error = "Name, a valid email and a message are required." });

        db.Submissions.Add(new Submission
        {
            Name = name, Email = email, Message = message,
            InquiryType = Opt(Cut(dto.InquiryType, 120)), Subject = Opt(Cut(dto.Subject, 200)), PageUrl = Opt(Cut(dto.PageUrl, 500)),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        });
        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
